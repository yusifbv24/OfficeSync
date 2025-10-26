using FileService.Domain.Common;

namespace FileService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a new file is successfully uploaded to the system.
    /// Other services can subscribe to this event to perform post-upload actions
    /// such as notifying users, updating message attachments, or triggering workflows.
    /// </summary>
    public record FileUploadedEvent(
        Guid FileId,
        string OriginalFileName,
        string ContentType,
        long SizeInBytes,
        Guid UploadedBy,
        Guid? ChannelId,
        Guid? MessageId
    ):IDomainEvent;
}