using FileService.Domain.Common;
using FileService.Domain.Enums;

namespace FileService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a file is successfully uploaded.
    /// Other parts of the system can react to this event.
    /// For example, a virus scanner could listen for this event and scan newly uploaded files.
    /// </summary>
    public class FileUploadedEvent:DomainEvent
    {
        public Guid FileId { get; }
        public string FileName { get; }
        public FileType FileType { get; }
        public long FileSizeBytes { get; }
        public Guid UploadedBy { get; }
        public FileUploadedEvent(
            Guid fileId,
            string fileName,
            FileType fileType,
            long fileSizeBytes,
            Guid uploadedBy)
        {
            FileId=fileId;
            FileName=fileName;
            FileType=fileType;
            FileSizeBytes=fileSizeBytes;
            UploadedBy=uploadedBy;
        }
    }
}