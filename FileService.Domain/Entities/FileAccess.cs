using FileService.Domain.Common;
using FileService.Domain.Events;

namespace FileService.Domain.Entities
{
    /// <summary>
    /// Represents explicit access permission granted to a specific user for a restricted file.
    /// This entity is only used when a file's AccessLevel is set to Restricted,
    /// allowing fine-grained control over who can access specific files.
    /// </summary>
    public class FileAccess:BaseEntity
    {
        public Guid Id { get; private set;  }

        public Guid FileId { get; private set; }
        
        public Guid UserId { get; private set; }

        public Guid GrantedBy { get; private set; }
        
        public DateTime GrantedAt { get; private set; }

        public bool IsRevoked { get; private set; }

        public DateTime? RevokedAt { get; private set; }
        public Guid? RevokedBy { get; private set; }

        public File File { get; private set; } = null!;

        private FileAccess() { }



        /// <summary>
        /// Creates a new file access grant for a specific user.
        /// This factory method ensures all invariants are satisfied at creation time.
        /// </summary>
        public static FileAccess Create(Guid fileId,Guid userId,Guid grantedBy)
        {
            if(fileId==Guid.Empty)
                throw new ArgumentException("File ID cannot be empty",nameof(fileId));

            if(userId==Guid.Empty)
                throw new ArgumentException("User ID cannot be empty",nameof(userId));

            if(grantedBy==Guid.Empty)
                throw new ArgumentException("Granter ID cannot be empty",nameof(grantedBy));

            var fileAccess = new FileAccess
            {
                Id=Guid.NewGuid(),
                FileId = fileId,
                UserId = userId,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            fileAccess.AddDomainEvent(new FileAccessChangedEvent(fileId, userId, true, grantedBy));
            return fileAccess;
        }



        /// <summary>
        /// Revokes this access grant, preventing the user from accessing the file.
        /// The grant is kept for audit purposes but marked as inactive.
        /// </summary>
        public void Revoke(Guid revokedBy)
        {
            if (IsRevoked)
                throw new InvalidOperationException("Access is already revoked");

            if(revokedBy==Guid.Empty)
                throw new ArgumentException("Revoker ID cannot be empty",nameof(revokedBy));

            IsRevoked = true;
            RevokedAt= DateTime.UtcNow;
            RevokedBy= revokedBy;
            UpdateTimestamp();
        }


        /// <summary>
        /// Restores a previously revoked access grant.
        /// </summary>
        public void Restore()
        {
            if (!IsRevoked)
                throw new InvalidOperationException("Access is not revoked");

            IsRevoked = false;
            RevokedAt = null;
            RevokedBy = null;
            UpdateTimestamp();
        }
    }
}