using MessagingService.Domain.Common;

namespace MessagingService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a message is deleted.
    /// </summary>
    public class MessageDeletedEvent:DomainEvent
    {
        public Guid MessageId { get; }
        public Guid ChannelId { get; }
        public Guid DeletedBy { get; }
        public MessageDeletedEvent(Guid messageId,Guid channelId,Guid deletedBy)
        {
            MessageId = messageId;
            ChannelId = channelId;
            DeletedBy = deletedBy;
        }
    }
}