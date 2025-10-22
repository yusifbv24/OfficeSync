using MessagingService.Domain.Common;

namespace MessagingService.Domain.Entities
{
    /// <summary>
    /// Entity representing a reaction to a message (emoji response).
    /// Part of the Message aggregate - not an aggregate root itself.
    /// Uses soft delete pattern to track reaction history.
    /// </summary>
    public class MessageReaction:BaseEntity
    {
        public Guid MessageId { get;private set;  }
        public Guid UserId { get; private set; }
        public string Emoji { get; private set; }=string.Empty;
        public bool IsRemoved { get; private set;  }
        public DateTime? RemovedAt { get; private set; }


        // Navigation property back to parent
        public Message Message { get; private set; } = null!;

        // Private constructor for EF Core
        private MessageReaction() { }



        /// <summary>
        /// Factory method to create a new reaction.
        /// </summary>
        public static MessageReaction Create(Guid messageId, Guid userId, string emoji)
        {
            if (string.IsNullOrWhiteSpace(emoji))
                throw new ArgumentException("Emoji cannot be empty", nameof(emoji));

            return new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji,
                IsRemoved = false
            };
        }



        /// <summary>
        /// Soft delete the reaction.
        /// We keep the record for history but mark it as removed.
        /// </summary>
        public void Remove()
        {
            if (IsRemoved)
                throw new InvalidOperationException("Reaction is already removed");

            IsRemoved = true;
            RemovedAt = DateTime.UtcNow;
            UpdateTimeStamp();
        }



        /// <summary>
        /// Restore a previously removed reaction.
        /// This happens when a user re-reacts with the same emoji.
        /// </summary>
        public void Restore()
        {
            if (!IsRemoved)
                throw new InvalidOperationException("Cannot restore a reaction that is not removed");

            IsRemoved = false;
            RemovedAt = null;
            UpdateTimeStamp();
        }
    }
}