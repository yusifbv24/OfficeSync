using FileService.Domain.Common;
using FileService.Domain.Enums;
using FileService.Domain.Events;

namespace FileService.Domain.Entities
{
    /// <summary>
    /// Represents a file stored in the system with its metadata and relationships.
    /// This entity tracks both the physical file storage and all associated metadata
    /// including permissions, versions, and access history.
    /// </summary>
    public class File:BaseEntity
    {
        /// <summary>
        /// Unique identifier for the file in the system.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Original filename as provided by the uploader.
        /// Example: "Project_Report_2024.pdf"
        /// </summary>
        public string OriginalFileName {  get; private set; }=string.Empty;


        /// <summary>
        /// Unique filename used for storage on disk to prevent conflicts.
        /// Format: {Guid}_{OriginalFileName}
        /// Example: "a1b2c3d4-e5f6-7890-abcd-ef1234567890_Project_Report_2024.pdf"
        /// </summary>
        public string StoredFileName { get; private set; } = string.Empty;


        /// <summary>
        /// MIME type of the file for proper content type handling.
        /// Examples: "image/jpeg", "application/pdf", "video/mp4"
        /// </summary>
        public string ContentType { get; private set;  } = string.Empty;


        public long SizeInBytes { get; private set;  }


        /// <summary>
        /// Relative path where the file is stored on the file system.
        /// Format: uploads/{year}/{month}/{day}/{StoredFileName}
        /// Example: "uploads/2024/10/25/a1b2c3d4-e5f6-7890-abcd-ef1234567890_Project_Report_2024.pdf"
        /// </summary>
        public string FilePath { get; private set; }=string.Empty;


        /// <summary>
        /// Optional path to generated thumbnail for image files.
        /// Only populated for image content types.
        /// </summary>
        public string? ThumbnailPath { get; private set; }


        /// <summary>
        /// ID of the user who uploaded this file.
        /// </summary>
        public Guid UploadedBy { get; private set; }



        public DateTime UploadedAt {  get; private set; }



        public Guid? ChannelId { get; private set; }


        public Guid? MessageId { get; private set; }



        /// <summary>
        /// Indicates whether this file has been soft-deleted.
        /// Soft-deleted files are hidden but retained for recovery purposes.
        /// </summary>
        public bool IsDeleted { get; private set; }



        public DateTime? DeletedAt { get; private set;  }



        public Guid? DeletedBy { get; private set; }



        /// <summary>
        /// Access level for this file determining who can view/download it.
        /// </summary>
        public FileAccessLevel AccessLevel { get; private set; }



        /// <summary>
        /// Optional description or notes about the file provided by uploader.
        /// </summary>
        public string? Description { get; private set; }



        /// <summary>
        /// Number of times this file has been downloaded.
        /// Useful for analytics and identifying popular files.
        /// </summary>
        public int DownloadCount { get; private set; }



        /// <summary>
        /// SHA256 hash of the file content for integrity verification and deduplication.
        /// </summary>
        public string FileHash { get; private set; } = string.Empty;



        /// <summary>
        /// Indicates whether this file passed virus scanning.
        /// Null if scanning hasn't been performed yet.
        /// </summary>
        public bool? IsScanned { get; private set; }



        /// <summary>
        /// Timestamp when virus scanning was completed (UTC).
        /// </summary>
        public DateTime? ScannedAt { get; private set; }



        /// <summary>
        /// Collection of users who have explicit access to this file.
        /// Only relevant when AccessLevel is Restricted.
        /// </summary>
        public ICollection<FileAccess> FileAccesses { get; private set; } = [];




        private File() { }



        /// <summary>
        /// Creates a new file entity with all required metadata.
        /// This factory method ensures all invariants are satisfied at creation time.
        /// </summary>
        public static File Create(
            string originalFileName,
            string storedFileName,
            string contentType,
            long sizeInBytes,
            string filePath,
            string fileHash,
            Guid uploadedBy,
            FileAccessLevel accessLevel=FileAccessLevel.Private,
            Guid? channelId=null,
            Guid? messageId=null,
            string? description = null)
        {
            // Validate that required fields are not empty
            if (string.IsNullOrWhiteSpace(originalFileName))
                throw new ArgumentException("Original filename cannot be empty", nameof(originalFileName));

            if (string.IsNullOrWhiteSpace(storedFileName))
                throw new ArgumentException("Stored filename cannot be empty", nameof(storedFileName));
            
            if(string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type cannot be empty",nameof(contentType));

            if (sizeInBytes <= 0)
                throw new ArgumentException("File size must be greater than zero", nameof(sizeInBytes));

            if(string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty",nameof(filePath));

            if(uploadedBy==Guid.Empty)
                throw new ArgumentException("Uploader ID cannot be empty",nameof(uploadedBy));

            var file = new File
            {
                Id = Guid.NewGuid(),
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                ContentType = contentType,
                SizeInBytes = sizeInBytes,
                FilePath = filePath,
                FileHash = fileHash,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                ChannelId = channelId,
                MessageId = messageId,
                AccessLevel = accessLevel,
                Description = description,
                IsDeleted = false,
                DownloadCount = 0,
                IsScanned = false,
            };

            file.AddDomainEvent(new FileUploadedEvent(
                file.Id,
                file.OriginalFileName,
                file.ContentType,
                file.SizeInBytes,
                file.UploadedBy,
                file.ChannelId,
                file.MessageId));

            return file;
        }




        /// <summary>
        /// Sets the thumbnail path after thumbnail generation completes.
        /// Only valid for image files.
        /// </summary>
        public void SetThumbnail(string thumbnailPath)
        {
            if(string.IsNullOrWhiteSpace(thumbnailPath))
                throw new ArgumentException("Thumbnail path cannot be empty",nameof(thumbnailPath));

            // Verify this is an image file before allowing thumbnail
            if (!ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Thumbnails can only be set for image files");

            ThumbnailPath= thumbnailPath;
            UpdateTimestamp();
        }




        public void MarkAsScanned(bool isClean)
        {
            IsScanned= isClean;
            ScannedAt = DateTime.UtcNow;
            UpdateTimestamp();

            // If the file is infected, raise domain event for handling
            if (!isClean)
            {
                AddDomainEvent(new FileInfectedEvent(Id, OriginalFileName, UploadedBy));
            }
        }




        public void IncremenetDownloadCount()
        {
            DownloadCount++;
            UpdateTimestamp();
        }




        /// <summary>
        /// Soft deletes the file, hiding it from normal queries but retaining for recovery.
        /// </summary>
        public void Delete(Guid deletedBy)
        {
            if (IsDeleted)
                throw new InvalidOperationException("File is already deleted");

            if(deletedBy== Guid.Empty)
                throw new ArgumentException("Deleter ID cannot be empty",nameof(deletedBy));


            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
            UpdateTimestamp();

            AddDomainEvent(new FileDeletedEvent(Id, OriginalFileName, deletedBy));
        }




        /// <summary>
        /// Restores a previously soft-deleted file.
        /// </summary>
        public void Restore()
        {
            if (!IsDeleted)
                throw new InvalidOperationException("File is not deleted");

            IsDeleted= false;
            DeletedAt = null;
            DeletedBy = null;
            UpdateTimestamp();
            AddDomainEvent(new FileRestoredEvent(Id, OriginalFileName));
        }



        public void UpdateDescription(string? description)
        {

            Description= description;
            UpdateTimestamp();
        }



        public void UpdateAccessLevel(FileAccessLevel newAccessLevel)
        {
            var oldAccessLevel = AccessLevel;
            AccessLevel = newAccessLevel;
            UpdateTimestamp();

            // If changing from Restricted to another level, clear specific access grants
            if(oldAccessLevel==FileAccessLevel.Restricted && newAccessLevel != FileAccessLevel.Restricted)
            {
                FileAccesses.Clear();
            }
        }



        /// <summary>
        /// Grants explicit access to a specific user for restricted files.
        /// Only applicable when AccessLevel is Restricted.
        /// </summary>
        public void GrantAccess(Guid userId,Guid grantedBy)
        {
            if (AccessLevel != FileAccessLevel.Restricted)
                throw new InvalidOperationException("Can only grant explicit access to restricted files");

            if(userId==Guid.Empty)
                throw new ArgumentException("User ID cannot be empty",nameof(userId));

            if(grantedBy==Guid.Empty)
                throw new ArgumentException("Granter ID cannot be empty",nameof(grantedBy));

            // Check if access already exists
            if (FileAccesses.Any(fa => fa.UserId == userId && !fa.IsRevoked))
                return;

            var fileAccess = FileAccess.Create(Id, userId, grantedBy);
            FileAccesses.Add(fileAccess);

            UpdateTimestamp();
        }



        public void RevokeAccess(Guid userId, Guid revokedBy)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            if (revokedBy == Guid.Empty)
                throw new ArgumentException("Revoker ID cannot be empty", nameof(revokedBy));

            var fileAccess = FileAccesses.FirstOrDefault(fa => fa.UserId == userId && !fa.IsRevoked);
            if (fileAccess == null)
                throw new InvalidOperationException("User does not have access to this file");

            fileAccess.Revoke(revokedBy);
            UpdateTimestamp();
        }



        /// <summary>
        /// Checks if a specific user has permission to access this file.
        /// Takes into account access levels, ownership, and explicit grants.
        /// </summary>
        public bool CanUserAccess(Guid userId,bool isAdmin = false)
        {
            // Admins can access any file
            if(isAdmin)
                return true;

            // File owner can always access their own files
            if(UploadedBy==userId)
                return true;

            return AccessLevel switch
            {
                FileAccessLevel.Public => true,
                FileAccessLevel.Private => false,
                FileAccessLevel.ChannelMembers => true,
                FileAccessLevel.Restricted => FileAccesses.Any(fa => fa.UserId == userId && fa.IsRevoked),
                _ => false
            };
        }
    }
}