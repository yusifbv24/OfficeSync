using FileService.Domain.Common;
using FileService.Domain.Enums;

namespace FileService.Domain.Events
{
    public class FileAccessedEvent:DomainEvent
    {
        public Guid FileId { get;}
        public Guid UserId { get;}
        public AccessType AccessType { get;}
        public FileAccessedEvent(Guid fileId,Guid userId,AccessType accessType)
        {
            FileId= fileId;
            UserId= userId;
            AccessType= accessType;
        }
    }
}