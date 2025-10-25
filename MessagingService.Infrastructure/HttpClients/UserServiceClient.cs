using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using MessagingService.Application.UserManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MessagingService.Infrastructure.HttpClients
{
    public class UserServiceClient:IUserServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserServiceClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public UserServiceClient(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserServiceClient> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            var userServiceUrl = configuration["Services:UserManagementService:BaseUrl"]
                ?? throw new InvalidOperationException("User Management Service URL not configured");

            _httpClient.BaseAddress=new Uri(userServiceUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }


        public async Task<Result<bool>> UserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AddAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync(
                    $"/api/users/by-userid/{userId}",
                    cancellationToken);

                return Result<bool>.Success(response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user exists");
                return Result<bool>.Success(false);
            }
        }



        public async Task<Result<string>> GetUserDisplayNameAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AddAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync(
                    $"/api/users/by-userid/{userId}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<string>.Failure("User not found");
                }

                var result = await response.Content.ReadFromJsonAsync<UserResponse>(
                    _jsonOptions,
                    cancellationToken);

                if (result?.Data?.DisplayName != null)
                {
                    return Result<string>.Success(result.Data.DisplayName);
                }

                return Result<string>.Failure("Unable to get user display name");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user display name");
                return Result<string>.Failure("Error communicating with User Management Service");
            }
        }



        public async Task<Result<Dictionary<Guid, string>>> GetUserDisplayNamesBatchAsync(
            IEnumerable<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userIdArray = userIds.Distinct().ToArray();

                // Early return for empty requests - prevents unnecessary HTTP calls
                if (userIdArray.Length == 0)
                {
                    _logger?.LogDebug("Batch user fetch called with empty user ID list");
                    return Result<Dictionary<Guid, string>>.Success(
                        new Dictionary<Guid, string>());
                }

                var requestBody = new { UserIds = userIdArray };

                await AddAuthorizationHeaderAsync();

                var response = await _httpClient.PostAsJsonAsync(
                    "/api/users/batch",
                    requestBody,
                    _jsonOptions,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<Dictionary<Guid, string>>.Failure(
                            "Failed to fetch users in batch");
                }

                var result = await response.Content.ReadFromJsonAsync<BatchUserResponse>(
                    _jsonOptions, cancellationToken);

                if (result?.Data == null)
                {
                    return Result<Dictionary<Guid, string>>.Success(
                        new Dictionary<Guid, string>());
                }

                var displayNames = result.Data.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value);

                return Result<Dictionary<Guid, string>>.Success(displayNames);
            }
            catch (HttpRequestException httpEx)
            {
                // Network-level errors (DNS failure, connection refused, etc.)
                _logger?.LogError(
                    httpEx,
                    "Network error during batch user fetch for {Count} users",
                    userIds.Count());

                return Result<Dictionary<Guid, string>>.Failure(
                    "Network error communicating with User Management Service");
            }
            catch (TaskCanceledException tcEx)
            {
                // Request timeout or cancellation
                _logger?.LogError(
                    tcEx,
                    "Timeout or cancellation during batch user fetch for {Count} users",
                    userIds.Count());

                return Result<Dictionary<Guid, string>>.Failure(
                    "Request timeout or cancelled");
            }
            catch (Exception ex)
            {
                // Catch-all for any other unexpected errors
                _logger?.LogError(
                    ex,
                    "Unexpected error during batch user fetch for {Count} users",
                    userIds.Count());

                return Result<Dictionary<Guid, string>>.Failure(
                    "Error communicating with User Management Service");
            }
        }




        private async Task AddAuthorizationHeaderAsync()
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", authHeader.Replace("Bearer", ""));
            }
            await Task.CompletedTask;
        }
    }
}