using FileService.Domain.Common;
using FileService.Domain.Enums;

namespace FileService.Domain.Entities
{
    /// <summary>
    /// Entity representing an access log entry for a file.
    /// This is part of the FileMetadata aggregate and provides audit trail capabilities.
    /// Every time someone views or downloads a file, we create an access log entry.
    /// </summary>
    public class FileAccessLog:BaseEntity
    {
        public Guid FileId { get; private set;  }
        public Guid UserId { get; private set;  }
        public AccessType AccessType { get; private set; }
        public DateTime AccessedAt { get; private set; }
        public string IpAddress { get; private set; }=string.Empty;
        public string? UserAgent { get; private set; }
        public FileMetadata File { get; private set; } = null!;
        private FileAccessLog() { }

        public static FileAccessLog Create(
            Guid fileId,
            Guid userId,
            AccessType accessType,
            string ipAddress,
            string? userAgent = null)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address is required", nameof(ipAddress));

            return new FileAccessLog
            {
                FileId = fileId,
                UserId = userId,
                AccessType = accessType,
                AccessedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };
        }
    }
}