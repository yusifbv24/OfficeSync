using FileService.Application.DTOs;
using FileService.Application.Interfaces;
using FileService.Infrastructure.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FileService.Infrastructure.Clients
{
    /// <summary>
    /// HTTP client for communicating with the Channel Service.
    /// 
    /// The File Service needs to verify channel membership when enforcing
    /// file access permissions. For files with AccessLevel = ChannelMembers,
    /// only users who are members of the associated channel can access the file.
    /// 
    /// This client makes HTTP calls to the Channel Service to verify membership
    /// and retrieve channel information, following the same microservices principles
    /// as the User Service client.
    /// </summary>
    public class ChannelServiceClient : IChannelServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChannelServiceClient(
            HttpClient httpClient, 
            IOptions<ServiceEndpoints> options,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(options.Value.ChannelServiceUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpContextAccessor= httpContextAccessor;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
        public async Task<ChannelInfoDto?> GetChannelInfoAsync(
            Guid channelId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AddAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync(
                    $"/api/channels/{channelId}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content=await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<ChannelInfoDto>(content, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<bool> IsUserChannelMemberAsync(
            Guid channelId, 
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AddAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync(
                    $"/api/channels/{channelId}/members/{userId}/is-member",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var content =await response.Content.ReadAsStringAsync(cancellationToken);

                var result = JsonSerializer.Deserialize<MembershipCheckResult>(content, _jsonOptions);
                return result?.IsMember ?? false;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }


        public async Task<bool> UserCanManageChannelAsync(
            Guid channelId, 
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AddAuthorizationHeaderAsync();
                var response = await _httpClient.GetAsync(
                    $"/api/channels/{channelId}/members/{userId}/can-manage",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result=JsonSerializer.Deserialize<PermissionCheckResult>(content, _jsonOptions);
                return result?.CanManage ?? false;
            }
            catch (HttpRequestException)
            {
                return false;
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