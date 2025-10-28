using FileService.Domain.Common;
using FileService.Domain.Enums;
using FileService.Domain.Events;
using FileService.Domain.ValueObjects;

namespace FileService.Domain.Entities
{
    /// <summary>
    /// Aggregate root representing metadata about an uploaded file.
    /// The actual file bytes are stored on disk, this entity only tracks information about the file.
    /// This separation keeps the database fast and allows us to use specialized storage for large files.
    /// </summary>
    public class FileMetadata:BaseEntity,IAggregateRoot
    {
        private readonly List<DomainEvent> _domainEvents = [];
        private readonly List<FileAccessLog> _accessLogs = [];

        public string OriginalFileName { get; private set; } = string.Empty;

        public string ContentType { get; private set; }=string.Empty;

        public long FileSizeBytes { get; private set; }

        public string StoragePath { get; private set; } = string.Empty;

        public string? ThumbnailPath { get; private set; }

        public FileType FileType { get; private set; }

        public FileStatus Status { get; private set; }

        public Guid UploadedBy {  get; private set; }

        public DateTime UploadedAt { get; private set; }

        public Guid? ChannelId {  get; private set; }

        public Guid? ConversationId {  get; private set; }

        public bool IsDeleted { get; private set; }

        public DateTime? DeletedAt { get; private set; }

        public Guid? DeletedBy { get; private set; }

        public string? Description { get; private set; }

        public IReadOnlyCollection<FileAccessLog> AccessLog => _accessLogs.AsReadOnly();

        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        private FileMetadata() { }

        public static FileMetadata Create(
            string originalFileName,
            MimeType contentType,
            FileSize fileSize,
            string storagePath,
            Guid uploadedBy,
            Guid? channelId,
            Guid? conversationId=null,
            string? description = null)
        {
            // Validate that the file belongs to either a channel or conversation, not both
            if (channelId.HasValue && conversationId.HasValue)
                throw new InvalidOperationException("File cannot belong to both a channel and a conversation");

            var fileMetadata = new FileMetadata()
            {
                OriginalFileName = originalFileName,
                ContentType = contentType,
                FileSizeBytes = fileSize.Bytes,
                StoragePath = storagePath,
                FileType = contentType.GetFileType(),
                Status = FileStatus.Uploading,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                ChannelId = channelId,
                ConversationId = conversationId,
                Description = description,
                IsDeleted = false
            };


            // Raise domain event
            fileMetadata.AddDomainEvent(new FileUploadedEvent(
                fileMetadata.Id,
                fileMetadata.OriginalFileName,
                fileMetadata.FileType,
                fileMetadata.FileSizeBytes,
                uploadedBy));

            return fileMetadata;
        }


        public void MarkAsAvailable()
        {
            if (Status != FileStatus.Uploading)
                throw new InvalidOperationException($"Cannot mark file as available from status {Status}");

            Status = FileStatus.Available;
            UpdateTimestamp();
        }


        public void SetThumbnail(string thumbnailPath)
        {
            if (FileType != FileType.Image && FileType != FileType.Video)
                throw new InvalidOperationException($"Cannot set thumbnail for file type {FileType}");

            ThumbnailPath=thumbnailPath;
            UpdateTimestamp();
        }


        public void Delete(Guid deletedBy)
        {
            if (IsDeleted)
                throw new InvalidOperationException("File is already deleted");

            IsDeleted = true;
            Status=FileStatus.Deleted;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
            UpdateTimestamp();

            // Raise domain event
            AddDomainEvent(new FileDeletedEvent(Id, OriginalFileName, deletedBy));
        }

        public void Quarantine(string reason)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot quarantine a deleted file");

            Status = FileStatus.Quarantined;
            UpdateTimestamp();
        }


        public void UpdateMetadata(string? description)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot update deleted file metadata");

            Description=description;
            UpdateTimestamp();
        }


        public void LogAccess(Guid userId, AccessType accessType,string ipAddress,string? userAgent = null) 
        {
            var accessLog=FileAccessLog.Create(Id,userId,accessType,ipAddress,userAgent);
            _accessLogs.Add(accessLog);

            // Raise domain event
            AddDomainEvent(new FileAccessedEvent(Id, userId, accessType));
        }



        /// <summary>
        /// Checks if a user has permission to access this file.
        /// This is a domain rule: files in channels can be accessed by channel members.
        /// Files in conversations can be accessed by conversation participants.
        /// </summary>
        public bool CanBeAccessedBy(Guid userId, bool isChannelMember,bool isConversationParticipant)
        {
            // File uploader always has access
            if (UploadedBy == userId)
                return true;

            // Deleted or quarantined files cannot be accessed
            if (IsDeleted || Status == FileStatus.Quarantined)
                return false;

            // File is still uploading
            if (Status == FileStatus.Uploading)
                return false;

            // Channel file : user must be a channel member
            if (ChannelId.HasValue)
                return isChannelMember;

            // Conversation file : user must be a participant
            if (ConversationId.HasValue)
                return isConversationParticipant;

            // Orphaned file: only uploader has access
            return false;
        }


        private void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        private void CleearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}