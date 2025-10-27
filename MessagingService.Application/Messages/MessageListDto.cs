using MessagingService.Domain.Enums;

namespace MessagingService.Application.Messages
{
    /// <summary>
    /// Simplified message information for list views.
    /// Optimized for performance when loading many messages.
    /// </summary>
    public record MessageListDto
    {
        public Guid Id { get; init; }
        public Guid ChannelId { get; init; }
        public Guid SenderId { get; init; }
        public string SenderName { get; set; }=string.Empty;
        public string Content { get; init; }=string.Empty;
        public MessageType Type { get; init; }
        public bool IsEdited { get; init; }
        public bool IsDeleted { get; init; }
        public DateTime CreatedAt { get; init; }


        /// <summary>
        /// Total count of reactions (not full reaction details).
        /// </summary>
        public int ReactionCount { get; init; }


        /// <summary>
        /// Number of attachments (not full attachment details).
        /// </summary>
        public int AttachmentCount { get; init; }
        public bool HasAttachments => AttachmentCount > 0;
    }
}