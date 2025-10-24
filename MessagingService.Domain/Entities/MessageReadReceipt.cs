using MessagingService.Domain.Common;

namespace MessagingService.Domain.Entities
{
    /// <summary>
    /// Tracks when a user has read a specific message.
    /// For group channels, we store a receipt for each member.
    /// For direct messages, we store receipt for the other person.
    /// </summary>
    public class MessageReadReceipt:BaseEntity
    {
        public Guid MessageId { get;private set;  }
        public Guid UserId { get;private set;  }
        public DateTime ReadAt { get; private set; }

        // Navigation property
        public Message Message { get; private set; } = null!;


        // Private constructor for EF Core
        private MessageReadReceipt() { }


        /// <summary>
        /// Factory method to create a read receipt.
        /// </summary>
        public static MessageReadReceipt Create(Guid messageId,Guid userId)
        {
            return new MessageReadReceipt
            {
                MessageId = messageId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Update the read timestamp if user reads again.
        /// </summary>
        public void UpdateReadTime()
        {
            ReadAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}