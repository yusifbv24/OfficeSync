using MessagingService.Application.Channel;
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
    public class ChannelServiceClient:IChannelServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ChannelServiceClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ChannelServiceClient(
            HttpClient httpClient,
            HttpContextAccessor htttpContextAccessor,
            IConfiguration configuration,
            ILogger<ChannelServiceClient> logger)
        {
            _httpClient=httpClient;
            _httpContextAccessor=htttpContextAccessor;
            _logger=logger;

            var channelServiceUrl = configuration["Services:ChannelService:BaseUrl"]
                ?? throw new InvalidOperationException("Channel Service URL not configured");

            _httpClient.BaseAddress=new Uri(channelServiceUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }


        public async Task<Result<bool>> IsUserMemberOfChannelAsync(
            Guid channelId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AddAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync(
                    $"/api/channels/{channelId}/members?requestedBy={userId}",
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    // If we can get members,user is a member
                    return Result<bool>.Success(true);
                }

                if(response.StatusCode==HttpStatusCode.Forbidden ||
                   response.StatusCode == HttpStatusCode.NotFound)
                {
                    return Result<bool>.Success(false);
                }

                _logger?.LogWarning(
                    "Failed to check channel membership. Status: {Status}",
                    response.StatusCode);

                return Result<bool>.Success(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking channel membership");
                // In case of service communication failure, we fail open for availability
                // In production, you might want to fail closed for security
                return Result<bool>.Success(true);
            }
        }



        public async Task<Result<ChannelInfo>> GetChannelInfoAsync(
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
                    return Result<ChannelInfo>.Failure("Channel not found or access denied");
                }

                var result = await response.Content.ReadFromJsonAsync<ChannelResponse>(
                    _jsonOptions,
                    cancellationToken);

                if (result?.Data != null)
                {
                    var channelInfo = new ChannelInfo(
                        result.Data.Id,
                        result.Data.Name,
                        result.Data.IsArchived);

                    return Result<ChannelInfo>.Success(channelInfo);
                }
                return Result<ChannelInfo>.Failure("Unable to get channel information");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting channel information");
                return Result<ChannelInfo>.Failure("Error communicating with Channel Service");
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