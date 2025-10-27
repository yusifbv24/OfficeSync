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
        private readonly List<MessageReadReceipt> _readReceipts = [];

        // To get file details (name, size, URL), you must call File Service.
        private readonly List<Guid> _attachmentFileIds = [];

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
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public IReadOnlyCollection<MessageReadReceipt> ReadReceipts=>_readReceipts.AsReadOnly();


        // This is the ONLY file-related property: just the IDs
        public IReadOnlyCollection<Guid> AttachmentFields=>_attachmentFileIds.AsReadOnly();

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
            MessageType type = MessageType.Text,
            Guid? parentMessageId = null,
            List<Guid>? attachmentFileIds = null)
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

            // Add file references if provided
            if(attachmentFileIds!=null && attachmentFileIds.Any())
            {
                foreach(var fileId in attachmentFileIds)
                {
                    message._attachmentFileIds.Add(fileId);
                }

                if(message.Type==MessageType.Text)
                {
                    message.Type = MessageType.File;
                }
            }


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
        /// Add a file reference to this message.
        /// 
        /// IMPORTANT: This only stores the FileId reference.
        /// The actual file must already exist in File Service.
        /// The caller is responsible for validating the file exists and is accessible.
        /// 
        /// Business rules:
        /// - Cannot add files to deleted messages
        /// - File Service should have already validated file upload and permissions
        /// - We're just storing the reference here
        /// </summary>
        public void AddFileReference(Guid fileId)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot add files to deleted messages");

            if (fileId == Guid.Empty)
                throw new ArgumentException("FileId cannot be empty");

            // Prevent duplicate file references
            if (_attachmentFileIds.Contains(fileId))
                throw new InvalidOperationException("File is already attached to this message");

            _attachmentFileIds.Add(fileId);

            // Update message type if needed
            if (Type == MessageType.Text && _attachmentFileIds.Any())
            {
                Type = MessageType.File;
            }

            UpdateTimestamp();
        }





        /// <summary>
        /// Remove a file reference from this message.
        /// 
        /// IMPORTANT: This only removes the reference.
        /// The actual file in File Service is not deleted - that's File Service's responsibility.
        /// If you want to delete the file itself, call File Service separately.
        /// </summary>
        public void RemoveFileReference(Guid fileId)
        {
            if (!_attachmentFileIds.Contains(fileId))
                throw new InvalidOperationException("File is not attached to this message");

            _attachmentFileIds.Remove(fileId);
            UpdateTimestamp();
        }



        public bool HasFileAttached(Guid fileId) => _attachmentFileIds.Contains(fileId);
        public int GetAttachmentCount() => _attachmentFileIds.Count;
        public bool HasUserRead(Guid userId) => _readReceipts.Any(r => r.UserId == userId);
        public IEnumerable<Guid> GetReadByUsers() => _readReceipts.Select(r => r.UserId);
        public int GetReadCount() => _readReceipts.Count;
        public bool HasUserReacted(Guid userId, string emoji) =>
            _reactions.Any(r => r.UserId == userId && r.Emoji == emoji && !r.IsRemoved);

        private void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}