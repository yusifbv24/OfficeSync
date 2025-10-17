using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using ChannelService.Application.UserManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChannelService.Infrastructure.HttpClients
{
    /// <summary>
    /// HTTP client for communicating with User Management Service.
    /// </summary>
    public class UserServiceClient:IUserServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserServiceClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public UserServiceClient(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<UserServiceClient> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger= logger;

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
                    $"/api/users/{userId}",
                    cancellationToken);

                return Result<bool>.Success(response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user exists");
                return Result<bool>.Success(true);
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
                    $"/api/users/{userId}",
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
                return Result<string>.Failure("Error communicating with User Managament Service");
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
        }
    }
}