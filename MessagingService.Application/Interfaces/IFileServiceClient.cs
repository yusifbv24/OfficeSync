using MessagingService.Application.Attachments;
using MessagingService.Application.Common;

namespace MessagingService.Application.Interfaces
{
    /// <summary>
    /// Interface for communicating with File Service from Messaging Service.
    /// 
    /// PURPOSE: Fetch file details to enrich message responses.
    /// The Messaging Service stores only FileIds. When returning messages to clients,
    /// we use this client to fetch the actual file details (names, sizes, URLs) from
    /// File Service to create complete MessageDto objects.
    /// 
    /// IMPORTANT: We fetch this data on-the-fly when building DTOs, but we NEVER
    /// store it in the Messaging database. File Service is the single source of truth.
    /// </summary>
    public interface IFileServiceClient
    {
        /// <summary>
        /// Get details for a single file by its ID.
        /// Returns null if file doesn't exist or user doesn't have access.
        /// 
        /// This is useful when displaying a single message with attachments.
        /// </summary>
        Task<Result<FileAttachmentDto?>> GetFileDetailsAsync(
            Guid fileId,
            Guid requestedBy,
            CancellationToken cancellationToken = default);



        /// <summary>
        /// Batch fetch file details for multiple FileIds in a single HTTP call.
        /// This is critical for performance when displaying a list of messages.
        /// 
        /// PERFORMANCE OPTIMIZATION:
        /// If you have 50 messages each with 2 attachments, that's 100 files.
        /// Making 100 separate HTTP calls would be extremely slow.
        /// This method fetches all 100 files in ONE call.
        /// 
        /// Returns a dictionary mapping FileId to file details.
        /// Files the user can't access or that don't exist are simply omitted.
        /// </summary>
        Task<Result<Dictionary<Guid, FileAttachmentDto>>> GetFileDetailsBatchAsync(
            List<Guid> fileIds,
            Guid requestedBy,
            CancellationToken cancellationToken = default);



        /// <summary>
        /// Verify that a file exists and the user has access to it.
        /// Used before creating a message with file attachments.
        /// 
        /// VALIDATION FLOW:
        /// 1. Frontend uploads file to File Service → gets FileId
        /// 2. Frontend sends message with FileId to Messaging Service
        /// 3. Messaging Service calls this method to verify FileId is valid
        /// 4. If valid, message is created; if not, error is returned
        /// </summary>
        Task<Result<bool>> ValidateFileAccessAsync(
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default);



        /// <summary>
        /// Batch validate multiple files at once.
        /// Returns list of FileIds that are valid and accessible.
        /// </summary>
        Task<Result<List<Guid>>> ValidateFileAccessBatchAsync(
            List<Guid> fileIds,
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}