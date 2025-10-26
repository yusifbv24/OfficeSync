using FileService.Application.Common;
using File = FileService.Domain.Entities.File;

namespace FileService.Application.Interfaces
{
    /// <summary>
    /// Repository interface for File entity operations.
    /// This interface defines all data access methods for files, abstracting away
    /// the underlying database implementation. This follows the Repository pattern,
    /// which provides several benefits:
    /// 
    /// 1. Testability: Easy to mock for unit tests
    /// 2. Flexibility: Can swap database implementations without changing business logic
    /// 3. Centralization: All data access logic in one place
    /// 4. Clean separation: Domain layer doesn't depend on infrastructure concerns
    /// </summary>
    public interface IFileRepository
    {
        /// <summary>
        /// Adds a new file entity to the repository.
        /// The file is not persisted until SaveChanges is called on the Unit of Work.
        /// </summary>
        Task AddAsync(File file, CancellationToken cancellationToken = default);


        /// <summary>
        /// Retrieves a file by its unique identifier.
        /// Returns null if the file doesn't exist or is deleted.
        /// </summary>
        Task<File?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);


        /// <summary>
        /// Retrieves a file by ID including its access grants.
        /// Useful when you need to check file permissions.
        /// </summary>
        Task<File?> GetByIdWithAccessesAsync(Guid id, CancellationToken cancellationToken = default);


        /// <summary>
        /// Retrieves a file by ID including deleted files.
        /// Used for restore operations where you need to access soft-deleted records.
        /// </summary>
        Task<File?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);


        /// <summary>
        /// Finds a file by its content hash.
        /// Useful for deduplication - checking if identical file already exists.
        /// </summary>
        Task<File?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default);


        /// <summary>
        /// Retrieves files with filtering and pagination.
        /// This is the main query method that supports all the filter criteria
        /// users might want to apply when searching for files.
        /// 
        /// The method applies permission filtering automatically, ensuring users
        /// only see files they have access to based on their role and the file's access level.
        /// </summary>
        Task<PagedResult<File>> GetFilesAsync(
            Guid requesterId,
            bool isAdmin,
            Guid? channelId = null,
            Guid? uploadedBy = null,
            string? contentType = null,
            string? searchTerm = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);


        /// <summary>
        /// Gets all files associated with a specific message.
        /// Used when displaying message attachments.
        /// </summary>
        Task<List<File>> GetFilesByMessageAsync(Guid messageId, CancellationToken cancellationToken = default);


        /// <summary>
        /// Calculates total storage used by a specific user.
        /// Useful for enforcing storage quotas per user.
        /// </summary>
        Task<long> GetTotalSizeByUserAsync(Guid userId, CancellationToken cancellationToken = default);


        /// <summary>
        /// Calculates total storage used in a specific channel.
        /// Useful for channel-level storage reporting.
        /// </summary>
        Task<long> GetTotalSizeByChannelAsync(Guid channelId, CancellationToken cancellationToken = default);
    }
}