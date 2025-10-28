using FileService.Domain.Common;

namespace FileService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a file is deleted.
    /// This could trigger cleanup processes or notify other services about the deletion.
    /// </summary>
    public class FileDeletedEvent:DomainEvent
    {
        public Guid FileId { get; }
        public string FileName { get; }
        public Guid DeletedBy {  get; }
        public FileDeletedEvent(Guid fileId, string fileName,Guid deletedBy)
        {
            FileId=fileId;
            FileName=fileName;
            DeletedBy=deletedBy;
        }
    }
}