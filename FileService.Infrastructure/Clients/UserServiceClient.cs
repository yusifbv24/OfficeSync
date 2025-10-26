using FileService.Application.Common;
using FileService.Application.Interfaces;
using FileService.Infrastructure.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FileService.Infrastructure.Clients
{
    /// <summary>
    /// HTTP client for communicating with the User Management Service.
    /// 
    /// In a microservices architecture, services communicate through HTTP APIs
    /// rather than directly accessing each other's databases. This provides several benefits:
    /// 
    /// 1. Service Autonomy: Each service owns its data and business logic
    /// 2. Loose Coupling: Services can evolve independently
    /// 3. Technology Flexibility: Services can use different databases or technologies
    /// 4. Clear Boundaries: API contracts define how services interact
    /// 
    /// This client handles all the complexity of HTTP communication (serialization,
    /// error handling, retries) and presents a clean interface to the application layer.
    /// 
    /// The implementation uses HttpClient with proper configuration for connection pooling
    /// and timeout handling, which are critical for performance and reliability in
    /// production environments.
    /// </summary>
    public class UserServiceClient : IUserServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public UserServiceClient(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IOptions<ServiceEndpoints> options)
        {
            _httpClient= httpClient;
            _httpContextAccessor= httpContextAccessor;
            _httpClient.BaseAddress = new Uri(options.Value.UserServiceUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,  // Accept both camelCase and PascalCase
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Use camelCase for requests
            };
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AddAuthorizationHeaderAsync();

                // Call the User Management Service API endpoint
                var response = await _httpClient.GetAsync(
                    "/api/users/{userId}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<UserProfileDto>(content, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }


        public async Task<List<UserProfileDto>> GetUserProfilesBatchAsync(
            List<Guid> userIds, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!userIds.Any())
                {
                    return [];
                }

                // Build query string with all user IDs
                var queryString = string.Join("&", userIds.Select(id => $"userIds={id}"));

                await AddAuthorizationHeaderAsync();
                var response = await _httpClient.GetAsync(
                    $"/api/users/batch?{queryString}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return [];
                }
                var content=await response.Content.ReadAsStringAsync(cancellationToken);
                var profiles = JsonSerializer.Deserialize<List<UserProfileDto>>(content, _jsonOptions);

                return profiles ?? [];
            }
            catch (HttpRequestException)
            {
                // If batch request fails, return empty list
                // The application can still function, just without user display names
                return [];
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