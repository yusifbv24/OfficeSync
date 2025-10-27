using MessagingService.Application.Attachments;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MessagingService.Infrastructure.HttpClients
{
    /// <summary>
    /// HTTP client for communicating with File Service.
    /// 
    /// ARCHITECTURAL PURPOSE:
    /// This client allows Messaging Service to fetch file details without storing them.
    /// File Service remains the single source of truth for all file information.
    /// 
    /// PERFORMANCE CONSIDERATIONS:
    /// - Uses batch operations to minimize HTTP round trips
    /// - Caches nothing (File Service handles caching if needed)
    /// - Includes proper timeout and error handling
    /// - Forwards auth tokens for proper access control
    /// </summary>
    public class FileServiceClient:IFileServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<FileServiceClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public FileServiceClient(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccesor,
            ILogger<FileServiceClient> logger,
            IConfiguration configuration)
        {
            _httpClient= httpClient;
            _httpContextAccessor= httpContextAccesor;
            _logger= logger;
            var fileServiceUrl = configuration["Services:FileService:BaseUrl"]
                ?? throw new InvalidOperationException("File Service URL not configured");

            _httpClient.BaseAddress= new Uri(fileServiceUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }


        public async Task<Result<FileAttachmentDto?>> GetFileDetailsAsync(
            Guid fileId, 
            Guid requestedBy, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AddAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync(
                    $"/api/files/{fileId}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    if(response.StatusCode==HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        _logger?.LogDebug(
                            "File {FileId} not accessible by user {UserId}",
                            fileId,
                            requestedBy);

                        return Result<FileAttachmentDto?>.Success(null);
                    }

                    _logger?.LogWarning(
                        "Failed to fetch file {FileId} : {Status}",
                        fileId,
                        response.StatusCode);

                    return Result<FileAttachmentDto?>.Failure("Unable to fetch file details");
                }

                var fileResponse = await response.Content.ReadFromJsonAsync<FileServiceResponse>(
                    _jsonOptions,
                    cancellationToken);
                if(fileResponse?.IsSuccess==true && fileResponse.Data != null)
                {
                    var dto = MapToFileAttachmentDto(fileResponse.Data);
                    return Result<FileAttachmentDto?>.Success(dto);
                }
                return Result<FileAttachmentDto?>.Success(null);
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error fetching file {FileId}", fileId);
                return Result<FileAttachmentDto?>.Failure("Network error communicating with File Service");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error fetching file {FileId}", fileId);
                return Result<FileAttachmentDto?>.Failure("Error fetching file details");
            }
        }




        public async Task<Result<Dictionary<Guid, FileAttachmentDto>>> GetFileDetailsBatchAsync(
            List<Guid> fileIds,
            Guid requestedBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Early return for empty requests
                if (!fileIds.Any())
                {
                    return Result<Dictionary<Guid, FileAttachmentDto>>.Success(
                        []);
                }

                // Remove duplicates
                var distinctFileIds = fileIds.Distinct().ToList();

                _logger?.LogDebug(
                    "Batch fetching {Count} file details for user {UserId}",
                    distinctFileIds.Count,
                    requestedBy);

                await AddAuthorizationHeaderAsync();

                // Build query string: /api/files/batch?ids=guid1&ids=guid2&ids=guid3
                var queryString = string.Join("&", distinctFileIds.Select(id => $"ids={id}"));

                var response = await _httpClient.GetAsync(
                    $"/api/files/batch?{queryString}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning(
                        "Batch file fetch failed with status {Status}",
                        response.StatusCode);
                    return Result<Dictionary<Guid, FileAttachmentDto>>.Failure(
                        "Unable to fetch file details");
                }

                var batchResponse = await response.Content.ReadFromJsonAsync<FileServiceBatchResponse>(
                    _jsonOptions,
                    cancellationToken);

                if(batchResponse?.IsSuccess==true && batchResponse.Data != null)
                {
                    var fileDict = batchResponse.Data
                        .ToDictionary(
                            file => file.Id,
                            file => MapToFileAttachmentDto(file));

                    _logger?.LogDebug(
                        "Succesfully fetched {Count} of {Total} requested files",
                        fileDict.Count,
                        distinctFileIds.Count);

                    return Result<Dictionary<Guid, FileAttachmentDto>>.Success(fileDict);
                }

                return Result<Dictionary<Guid, FileAttachmentDto>>.Success([]);
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error during batch file fetch");
                return Result<Dictionary<Guid, FileAttachmentDto>>.Failure(
                    "Network error communicating with File Service");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during batch file fetch");
                return Result<Dictionary<Guid, FileAttachmentDto>>.Failure(
                    "Error fetching file details");
            }
        }



        public async Task<Result<bool>> ValidateFileAccessAsync(
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AddAuthorizationHeaderAsync();

                // HEAD request is more efficient than GET for existence checks
                var request = new HttpRequestMessage(HttpMethod.Head, $"/api/files/{fileId}");
                var response = await _httpClient.SendAsync(request, cancellationToken);

                var isValid = response.IsSuccessStatusCode;

                _logger?.LogDebug(
                    "File {FileId} validation for user {UserId}: {IsValid}",
                    fileId,
                    userId,
                    isValid);

                return Result<bool>.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error validating file {FileId} access", fileId);
                // In case of error, fail closed (deny access) for security
                return Result<bool>.Success(false);
            }
        }




        public async Task<Result<List<Guid>>> ValidateFileAccessBatchAsync(
           List<Guid> fileIds,
           Guid userId,
           CancellationToken cancellationToken = default)
        {
            try
            {
                // Early return for empty requests
                if (!fileIds.Any())
                {
                    return Result<List<Guid>>.Success(new List<Guid>());
                }

                var distinctFileIds = fileIds.Distinct().ToList();

                _logger.LogDebug(
                    "Batch validating {Count} files for user {UserId}",
                    distinctFileIds.Count,
                    userId);

                await AddAuthorizationHeaderAsync();

                // POST request with file IDs in body for validation
                var requestBody = new { FileIds = distinctFileIds };
                var response = await _httpClient.PostAsJsonAsync(
                    "/api/files/validate-access",
                    requestBody,
                    _jsonOptions,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning(
                        "Batch file validation failed with status {Status}",
                        response.StatusCode);
                    return Result<List<Guid>>.Failure("Unable to validate file access");
                }

                var validationResponse = await response.Content.ReadFromJsonAsync<FileAccessValidationResponse>(
                    _jsonOptions,
                    cancellationToken);

                if (validationResponse?.IsSuccess == true && validationResponse.Data != null)
                {
                    _logger?.LogDebug(
                        "Validated {Count} of {Total} files as accessible",
                        validationResponse.Data.Count,
                        distinctFileIds.Count);

                    return Result<List<Guid>>.Success(validationResponse.Data);
                }

                return Result<List<Guid>>.Success(new List<Guid>());
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error during batch file validation");
                return Result<List<Guid>>.Failure("Network error communicating with File Service");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during batch file validation");
                return Result<List<Guid>>.Failure("Error validating file access");
            }
        }



        private async Task AddAuthorizationHeaderAsync()
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", authHeader.Replace("Bearer ", ""));
            }
            await Task.CompletedTask;
        }

        private FileAttachmentDto MapToFileAttachmentDto(FileServiceFileData data)
        {
            return new FileAttachmentDto
            {
                FileId = data.Id,
                FileName = data.OriginalFileName,
                ContentType = data.ContentType,
                SizeInBytes = data.SizeInBytes,
                DownloadUrl = data.DownloadUrl,
                ThumbnailUrl = data.ThumbnailUrl,
                UploadedAt = data.UploadedAt
            };
        }

        // Response DTOs that match File Service API responses
        private record FileServiceResponse(bool IsSuccess, FileServiceFileData? Data, string? Message = null);

        private record FileServiceBatchResponse(bool IsSuccess, List<FileServiceFileData>? Data, string? Message = null);

        private record FileAccessValidationResponse(bool IsSuccess, List<Guid>? Data, string? Message = null);

        private record FileServiceFileData(
            Guid Id,
            string OriginalFileName,
            string ContentType,
            long SizeInBytes,
            string DownloadUrl,
            string? ThumbnailUrl,
            DateTime UploadedAt
        );
    }
}