using MessagingService.Application.Attachments;
using MessagingService.Application.Reactions;
using MessagingService.Domain.Enums;

namespace MessagingService.Application.Messages
{
    /// <summary>
    /// Complete message information returned to clients.
    /// Includes all details, reactions, and attachments.
    /// </summary>
    public record MessageDto
    {
        public Guid Id { get; init; }
        public Guid ChannelId { get; init; }
        public Guid SenderId { get; init; }
        public string SenderName { get; init; } = string.Empty;
        public string Content { get; init;  }= string.Empty;
        public MessageType Type { get; init; }
        public bool IsEdited { get; init; }
        public DateTime? EditedAt { get; init;  }
        public bool IsDeleted { get; init; }
        public DateTime CreatedAt {  get; init; }
        public DateTime UpdatedAt { get; init; }
        public Guid? ParentMessageId { get; init;  }

        /// <summary>
        /// Reactions grouped by emoji with counts.
        /// </summary>
        public List<MessageReactionDto> Reactions { get; init; } = [];


        /// <summary>
        /// File attachments on this message.
        /// </summary>
        public List<MessageAttachmentDto> Attachments { get; init; } = [];
    }
}