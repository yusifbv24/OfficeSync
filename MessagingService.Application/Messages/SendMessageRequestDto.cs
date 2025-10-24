using MessagingService.Application.Attachments;
using MessagingService.Domain.Enums;

namespace MessagingService.Application.Messages
{
    /// <summary>
    /// Request to send a message to a channel.
    /// Immutable record ensures thread safety and clear intent.
    /// </summary>
    public record SendMessageRequestDto(
        string Content,
        MessageType Type=MessageType.Text,
        Guid? ParentMessageId=null);
}