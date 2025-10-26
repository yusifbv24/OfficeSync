using FileService.Domain.Common;

namespace FileService.Domain.Events
{
    public record FileDeletedEvent(
        Guid FileId,
        string OriginalFileName,
        Guid DeletedBy): IDomainEvent;
}