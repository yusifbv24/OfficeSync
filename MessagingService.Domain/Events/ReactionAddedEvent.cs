using MessagingService.Domain.Common;

namespace MessagingService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a reaction is added to a message.
    /// </summary>
    public class ReactionAddedEvent : DomainEvent
    {
        public Guid MessageId { get; }
        public Guid ChannelId { get; }
        public Guid UserId { get; }
        public string Emoji { get; }
        public ReactionAddedEvent(Guid messageId,Guid channelId,Guid userId,string emoji)
        {
            MessageId = messageId;
            ChannelId = channelId;
            UserId = userId;
            Emoji = emoji;
        }
    }
}