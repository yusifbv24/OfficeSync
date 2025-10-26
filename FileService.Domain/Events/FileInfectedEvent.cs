using FileService.Domain.Common;

namespace FileService.Domain.Events
{
    public record FileInfectedEvent(
        Guid FileId,
        string OriginalFileName,
        Guid UploadedBy):IDomainEvent;
}