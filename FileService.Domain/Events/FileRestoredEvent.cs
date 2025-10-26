using FileService.Domain.Common;

namespace FileService.Domain.Events
{
    public record FileRestoredEvent(
        Guid FileId,
        string OriginalFileName):IDomainEvent;
}