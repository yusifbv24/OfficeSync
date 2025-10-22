using MessagingService.Domain.Common;
using MessagingService.Domain.Enums;

namespace MessagingService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a message is sent.
    /// Other services can listen for this to push notifications, update read status, etc.
    /// </summary>
    public class MessageSentEvent:DomainEvent
    {
        public Guid MessageId { get; }
        public Guid ChannelId { get; }
        public Guid SenderId { get; }
        public MessageType Type { get; }
        public MessageSentEvent(Guid messageId,Guid channelId,Guid senderId,MessageType type)
        {
            MessageId= messageId;
            ChannelId= channelId;
            SenderId= senderId;
            Type= type;
        }
    }
}