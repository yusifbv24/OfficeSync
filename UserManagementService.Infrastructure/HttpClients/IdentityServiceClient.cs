using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using UserManagementService.Application.Common;
using UserManagementService.Application.DTOs.Identity;
using UserManagementService.Application.Interfaces;

namespace UserManagementService.Infrastructure.HttpClients
{
    /// <summary>
    /// HTTP client for communicating with the Identity Service.
    /// Handles serialization, error handling, and proper cancellation token propagation.
    /// Uses typed HttpClient pattern for testability and proper lifecycle management.
    /// </summary>
    public class IdentityServiceClient: IIdentityServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdentityServiceClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;


        public IdentityServiceClient(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<IdentityServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Configure the HttpClient base address 
            var identityServiceUrl = configuration["Services:IdentityService:BaseUrl"]
                ?? throw new InvalidOperationException("Identity Service base URL is not configured.");

            _httpClient.BaseAddress = new Uri(identityServiceUrl);
            _httpClient.Timeout= TimeSpan.FromSeconds(30); // Set a reasonable timeout

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Create a user in the Identity Service.
        /// This creates authentication credentials (username/password).
        /// </summary>
        public async Task<Result<IdentityUserDto>> CreateUserAsync(
            string username,
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    username,
                    email,
                    password
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "api/auth/register",
                    request,
                    _jsonOptions,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent=await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Failed to create user in Identity Service. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode,
                        errorContent);

                    return Result<IdentityUserDto>.Failure(
                        $"Failed to create user in Identity Service:{response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<IdentityServiceResponse>(
                    _jsonOptions,
                    cancellationToken);

                if(result?.IsSuccess==true && result.Data != null)
                {
                    var dto = new IdentityUserDto(
                        UserId: result.Data.Id,
                        Username: result.Data.Username,
                        Email: result.Data.Email);

                    return Result<IdentityUserDto>.Success(dto);
                }

                return Result<IdentityUserDto>.Failure("Invalid response from Identity Service");
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP request failed while creating user in Identity Service");
                return Result<IdentityUserDto>.Failure("Failed to connect to Identity Service");
            }
            catch(TaskCanceledException ex)
            {
                _logger?.LogWarning(ex, "Request to Identity Service was cancelled or timed out");
                return Result<IdentityUserDto>.Failure("Request timeout");
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while creating user in Identity Service");
                return Result<IdentityUserDto>.Failure("Unexpected error occured");
            }
        }




        /// <summary>
        /// Deactivate a user in the Identity Service.
        /// This prevents the user from logging in.
        /// </summary>
        public async Task<Result<bool>> DeactivateUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PutAsync(
                    $"/api/internal/users/{userId}/deactivate",
                    null,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning(
                        "Failed to deactivate user in Identity Service. UserId: {UserId},Status: {StatusCode}",
                        userId,
                        response.StatusCode);

                    return Result<bool>.Failure($"Failed to deactivate user: {response.StatusCode}");
                }

                return Result<bool>.Success(true);
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP request failed while deactivating user in Identity Service");
                return Result<bool>.Failure("Failed to connect to Identity Service");
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while deactivating user in Identity Service");
                return Result<bool>.Failure("Unexpected error occurred");
            }
        }



        ///<summary>
        /// Check if a user exists in the Identity Service
        /// </summary>
        public async Task<Result<bool>> UserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/internal/users/{userId}",
                    cancellationToken);

                return Result<bool>.Success(response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user exists in IdentityService");
                return Result<bool>.Failure("Error checking user existence");
            }
        }
    }
}