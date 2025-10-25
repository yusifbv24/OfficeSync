using MessagingService.Application.Common;

namespace MessagingService.Application.Interfaces
{
    /// <summary>
    /// Interface for communicating with User Management Service.
    /// Abstracts the HTTP communication details.
    /// </summary>
    public interface IUserServiceClient
    {
        /// <summary>
        /// Check if a user exists in the system.
        /// Useful for validation.
        /// </summary>
        Task<Result<bool>> UserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);



        /// <summary>
        /// Get a user's display name.
        /// Used frequently when building message DTOs to show sender names.
        /// </summary>
        Task<Result<string>> GetUserDisplayNameAsync(
            Guid userId,
            CancellationToken cancellationToken= default);



        /// <summary>
        /// Batch fetch multiple user display names in a single HTTP call.
        /// This is a critical performance optimization that should be used whenever
        /// you need to fetch display names for multiple users.
        Task<Result<Dictionary<Guid, string>>> GetUserDisplayNamesBatchAsync(
            IEnumerable<Guid> userIds,
            CancellationToken cancellationToken = default);
    }
}