using MessagingService.Domain.Common;
using MessagingService.Domain.Enums;
using MessagingService.Domain.Events;
using MessagingService.Domain.ValueObjects;

namespace MessagingService.Domain.Entities
{
    /// <summary>
    /// Message aggregate root.
    /// Represents a message in a channel with all its associated data.
    /// Encapsulates all business logic for message operations.
    /// </summary>
    public class Message:BaseEntity,IAggregateRoot
    {
        // Using private fields with backing collections for true encapsulation
        private readonly List<DomainEvent> _domainEvents = [];
        private readonly List<MessageReaction> _reactions = [];
        private readonly List<MessageAttachment> _attachments = [];
        private readonly List<MessageReadReceipt> _readReceipts = [];

        // Private setters prevent external modification - all changes must go through methods
        public Guid ChannelId { get; private set; }
        public Guid SenderId { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public MessageType Type { get; private set; }
        public bool IsEdited { get; private set;  }
        public DateTime? EditedAt { get; private set;  }
        public bool IsDeleted {  get; private set; }
        public DateTime? DeletedAt {  get; private set; }


        /// <summary>
        /// If this is a reply, points to the parent message.
        /// Enables threading conversations.
        /// </summary>
        public Guid? ParrentMessageId { get; private set;  }
        public Message? ParrentMessage { get;private set;  }

        // Read-only collections prevent external modification of relationships
        public IReadOnlyCollection<MessageReaction> Reactions=>_reactions.AsReadOnly();
        public IReadOnlyCollection<MessageAttachment> Attachments=>_attachments.AsReadOnly();
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public IReadOnlyCollection<MessageReadReceipt> ReadReceipts=>_readReceipts.AsReadOnly();


        // Private constructor for EF Core - prevents direct instantiation
        private Message() { }


        /// <summary>
        /// Factory method to create a new message.
        /// This is the ONLY way to create a message, ensuring all business rules are enforced.
        /// </summary>
        public static Message Create(
            Guid channelId,
            Guid senderId,
            MessageContent content,
            MessageType type=MessageType.Text,
            Guid? parentMessageId = null)
        {
            var message = new Message
            {
                ChannelId = channelId,
                SenderId = senderId,
                Content = content,
                Type = type,
                IsEdited = false,
                IsDeleted = false,
                ParrentMessageId = parentMessageId
            };

            // Raise domain event for other services to react to
            message.AddDomainEvent(new MessageSentEvent(
                message.Id,
                message.ChannelId,
                message.SenderId,
                message.Type));

            return message;
        }




        /// <summary>
        /// Edit the message content.
        /// Business rule: Only the sender can edit their own message.
        /// Business rule: Cannot edit deleted messages.
        /// Business rule: System messages cannot be edited.
        /// </summary>
        public void Edit(MessageContent newContent, Guid editedBy)
        {
            // Enforce business rules
            if (IsDeleted)
                throw new InvalidOperationException("Cannot edit a deleted message");

            if (SenderId != editedBy)
                throw new InvalidOperationException("Only the message sender can edit the message");

            if (Type == MessageType.System)
                throw new InvalidOperationException("System messages cannot be edited");

            Content = newContent;
            IsEdited=true;
            EditedAt=DateTime.UtcNow;
            UpdateTimestamp();

            AddDomainEvent(new MessageEditedEvent(Id, ChannelId, editedBy));
        }




        /// <summary>
        /// Soft delete the message.
        /// We don't actually remove messages from the database to maintain conversation history.
        /// Business rule: Only sender can delete their own message.
        /// Business rule: Cannot delete already deleted messages.
        /// </summary>
        public void Delete(Guid deletedBy)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Message is already deleted");

            if (SenderId != deletedBy)
                throw new InvalidOperationException("Only the message sender can delete the message");

            IsDeleted = true;
            DeletedAt=DateTime.UtcNow;

            // When deleting, we clear the content but keep the metadata
            // This preserves conversation flow while removing sensitive content
            Content="[Message deleted]";
            UpdateTimestamp();

            AddDomainEvent(new MessageDeletedEvent(Id,ChannelId, deletedBy));
        }




        /// <summary>
        /// Add a reaction to the message (emoji response).
        /// Business rule: Users cannot react multiple times with the same emoji.
        /// Business rule: Cannot react to deleted messages.
        /// </summary>
        public void AddReaction(Guid userId, string emoji)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot react to a deleted message");

            if (string.IsNullOrWhiteSpace(emoji))
                throw new ArgumentException("Emoji cannot be empty");

            // Check if user already reacted with this emoji
            var existingReaction=_reactions.FirstOrDefault(r=>
                r.UserId==userId &&
                r.Emoji==emoji &&
                !r.IsRemoved);

            if(existingReaction != null)
                throw new InvalidOperationException("User has already reacted with this emoji");

            // Check if this reaction was previously removed and restored it
            var removedReaction = _reactions.FirstOrDefault(r =>
                r.UserId == userId &&
                r.Emoji == emoji &&
                r.IsRemoved);

            if (removedReaction != null)
            {
                removedReaction.Restore();
            }
            else
            {
                var reaction=MessageReaction.Create(Id,userId,emoji);
                _reactions.Add(reaction);
            }
            UpdateTimestamp();
            AddDomainEvent(new ReactionAddedEvent(Id, ChannelId, userId, emoji));
        }




        /// <summary>
        /// Remove a reaction from the message.
        /// Business rule: Can only remove your own reactions.
        /// </summary>
        public void RemoveReaction(Guid userId, string emoji)
        {
            var reaction=_reactions.First(r=>
                r.UserId==userId &&
                r.Emoji==emoji &&
                !r.IsRemoved);

            if (reaction == null)
                throw new InvalidOperationException("Reaction not found");

            reaction.Remove();
            UpdateTimestamp();

            AddDomainEvent(new ReactionRemovedEvent(Id,ChannelId, userId));
        }




        /// <summary>
        /// Add an attachment to the message.
        /// Business rule: Cannot add attachments to deleted messages.
        /// </summary>
        public void AddAttachment(Guid fileId,string fileName,string fileUrl,long fileSize,string mimeType)
        {
            if(IsDeleted)
                throw new InvalidOperationException("Cannot add attachments to deleted messages");

            var attachment=MessageAttachment.Create(Id,fileId,fileName,fileUrl,fileSize,mimeType);
            _attachments.Add(attachment);

            // Update message type if this is the first attachment
            if (Type == MessageType.Text)
            {
                Type = mimeType.StartsWith("image/") ? MessageType.Image : MessageType.File;
            }

            UpdateTimestamp();
        }



        /// <summary>
        /// Mark message as read by a user.
        /// If already read, updates the read time.
        /// </summary>
        public void MarkAsRead(Guid userId)
        {
            // Dont allow sender to mark their own message as read 
            if (userId == SenderId)
                return;

            // Check if user already read this message
            var existingReceipt = _readReceipts.FirstOrDefault(r => r.UserId == userId);

            if (existingReceipt != null)
            {
                // Update read time
                existingReceipt.UpdateReadTime();
            }
            else
            {
                // Create new read receipt
                var receipt = MessageReadReceipt.Create(Id, userId);
                _readReceipts.Add(receipt);
            }
            UpdateTimestamp();
        }



        /// <summary>
        /// Check if a specific user has read this message.
        /// </summary>
        public bool HasUserRead(Guid userId)
        {
            return _readReceipts.Any(r => r.UserId == userId);
        }



        /// <summary>
        /// Get all users who have read this message.
        /// </summary>
        public IEnumerable<Guid> GetReadByUsers()
        {
            return _readReceipts.Select(r => r.UserId);
        }



        /// <summary>
        /// Get the count of users who have read this message.
        /// </summary>
        public int GetReadCount()
        {
            return _readReceipts.Count;
        }




        /// <summary>
        /// Check if a specific user has reacted with a specific emoji.
        /// </summary>
        public bool HasUserReacted(Guid userId,string emoji)
        {
            return _reactions.Any(r=>r.UserId==userId && r.Emoji==emoji && !r.IsRemoved);
        }

        private void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}