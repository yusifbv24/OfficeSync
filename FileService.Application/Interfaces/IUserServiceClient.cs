using FileService.Application.Common;

namespace FileService.Application.Interfaces
{
    /// <summary>
    /// Client interface for communicating with the User Management Service.
    /// This service client allows the File Service to fetch user information
    /// without directly accessing the User Management Service's database.
    /// 
    /// This follows the microservices principle of service autonomy:
    /// each service owns its data and exposes it through well-defined APIs.
    /// Services communicate via HTTP calls rather than direct database access.
    /// 
    /// The client handles HTTP communication, error handling, and deserialization,
    /// presenting a clean interface to the application layer.
    /// </summary>
    public interface IUserServiceClient
    {
        /// <summary>
        /// Fetches a single user's profile information.
        /// Returns null if the user doesn't exist.
        /// Used for checking roles, permissions, and displaying user information.
        /// </summary>
        Task<UserProfileDto?> GetUserProfileAsync(
            Guid userId,
            CancellationToken cancellationToken = default);


        /// <summary>
        /// Fetches multiple user profiles in a single batch request.
        /// This is much more efficient than making separate requests for each user.
        /// 
        /// When displaying a list of files, we need uploader names for potentially
        /// dozens or hundreds of files. Batching these requests into one HTTP call
        /// dramatically improves performance by reducing network overhead.
        /// 
        /// This addresses the N+1 query problem you fixed in the Messaging Service.
        /// </summary>
        Task<List<UserProfileDto>> GetUserProfilesBatchAsync(
            List<Guid> userIds,
            CancellationToken cancellationToken = default);
    }
}