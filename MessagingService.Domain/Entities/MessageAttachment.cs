using MessagingService.Domain.Common;

namespace MessagingService.Domain.Entities
{
    /// <summary>
    /// Entity representing a file attachment on a message.
    /// Part of the Message aggregate - not an aggregate root itself.
    /// Stores metadata; actual file is stored in File Service.
    /// </summary>
    public class MessageAttachment:BaseEntity
    {
        public Guid MessageId { get; private set; }

        /// <summary>
        /// Reference to the file in the File Service.
        /// </summary>
        public Guid FileId { get; private set;  }

        public string FileName { get; private set; } = string.Empty;
        public string FileUrl { get; private set; }=string.Empty;
        public long FileSize { get; private set; }
        public string MimeType { get; private set; } = string.Empty;


        // Navigation property
        public Message Message { get; private set; } = null!;


        // Private constructor for EF Core
        private MessageAttachment() { }

        /// <summary>
        /// Factory method to create a new attachment.
        /// </summary>
        public static MessageAttachment Create(
            Guid messageId,
            Guid fileId,
            string fileName,
            string fileUrl,
            long fileSize,
            string mimeType)
        {
            if(string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty",nameof(fileName));

            if(string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL cannot be empty",nameof(fileUrl));

            if (fileSize <= 0)
                throw new ArgumentException("File size must be positive", nameof(fileSize));

            return new MessageAttachment
            {
                MessageId = messageId,
                FileId = fileId,
                FileName = fileName,
                FileUrl = fileUrl,
                FileSize = fileSize,
                MimeType = mimeType
            };
        }
    }
}