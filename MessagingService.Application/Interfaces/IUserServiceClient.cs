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
    }
}