using FileService.Domain.Common;

namespace FileService.Domain.Events
{
    public record FileAccessChangedEvent(
        Guid FileId,
        Guid UserId,
        bool AccessGranted,
        Guid ChangedBy): IDomainEvent;
}