using FileService.Application.DTOs;

namespace FileService.Application.Interfaces
{
    /// <summary>
    /// Client interface for communicating with the Channel Service.
    /// The File Service needs to verify channel membership when enforcing
    /// file access permissions for files with ChannelMembers access level.
    /// 
    /// Similar to the User Service client, this follows microservices principles
    /// by communicating through HTTP APIs rather than direct database access.
    /// </summary>
    public interface IChannelServiceClient
    {
        /// <summary>
        /// Checks if a user is a member of a specific channel.
        /// Used to verify access when a file's AccessLevel is ChannelMembers.
        /// Returns false if the user is not a member or the channel doesn't exist.
        /// </summary>
        Task<bool> IsUserChannelMemberAsync(
            Guid channelId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user has management permissions for a channel.
        /// Used to verify if a user can delete files or manage permissions in a channel.
        /// Operators and admins typically have these permissions.
        /// </summary>
        Task<bool> UserCanManageChannelAsync(
            Guid channelId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets basic channel information.
        /// Useful for displaying channel names in file listings.
        /// Returns null if the channel doesn't exist.
        /// </summary>
        Task<ChannelInfoDto?> GetChannelInfoAsync(
            Guid channelId,
            CancellationToken cancellationToken = default);
    }
}