using MessagingService.Domain.Common;

namespace MessagingService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a message is edited.
    /// </summary>
    public class MessageEditedEvent:DomainEvent
    {
        public Guid MessageId { get; }
        public Guid ChannelId { get; }
        public Guid EditedBy { get; }
        public MessageEditedEvent(Guid messageId,Guid channelId,Guid editedBy)
        {
            MessageId = messageId;
            ChannelId = channelId;
            EditedBy = editedBy;
        }
    }
}