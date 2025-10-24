using MessagingService.Application.Channel;
using MessagingService.Application.Common;

namespace MessagingService.Application.Interfaces
{
    /// <summary>
    /// Interface for communicating with Channel Service.
    /// Abstracts the HTTP communication details.
    /// </summary>
    public interface IChannelServiceClient
    {
        /// <summary>
        /// Check if a user is a member of a specific channel.
        /// This is a critical authorization check before allowing message operations.
        /// </summary>
        Task<Result<bool>> IsUserMemberOfChannelAsync(
            Guid channelId,
            Guid userId,
            CancellationToken cancellationToken = default);



        /// <summary>
        /// Get information about a channel.
        /// Useful for validation and displaying channel context.
        /// </summary>
        Task<Result<ChannelInfo>> GetChannelInfoAsync(
            Guid channelId,
            CancellationToken cancellationToken = default);
    }
}