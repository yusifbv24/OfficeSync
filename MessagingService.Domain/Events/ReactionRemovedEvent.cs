using MessagingService.Domain.Common;

namespace MessagingService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a reaction is removed from a message.
    /// </summary>
    public class ReactionRemovedEvent:DomainEvent
    {
        public Guid MessageId { get; }
        public Guid ChannelId { get; }
        public Guid UserId { get; }
        public ReactionRemovedEvent(Guid messageId,Guid channelId,Guid userId)
        {
            MessageId = messageId;
            ChannelId = channelId;
            UserId = userId;
        }
    }
}