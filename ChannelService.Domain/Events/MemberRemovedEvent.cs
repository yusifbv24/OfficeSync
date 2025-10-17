using ChannelService.Domain.Common;

namespace ChannelService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a member is removed from a channel
    /// </summary>
    public class MemberRemovedEvent:DomainEvent
    {
        public Guid ChannelId { get; }
        public Guid UserId { get; }
        public Guid RemovedBy { get; }
        public MemberRemovedEvent(Guid channelId,Guid userId,Guid removedBy)
        {
            ChannelId=channelId;
            UserId=userId;
            RemovedBy=removedBy;
        }
    }
}