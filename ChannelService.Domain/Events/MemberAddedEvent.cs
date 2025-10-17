using ChannelService.Domain.Common;
using ChannelService.Domain.Enums;

namespace ChannelService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a member is added to a channel
    /// </summary>
    public class MemberAddedEvent: DomainEvent
    {
        public Guid ChannelId { get; }
        public Guid UserId {  get; }
        public MemberRole Role { get; }
        public Guid AddedBy { get; }
        public MemberAddedEvent(Guid channelId,Guid userId,MemberRole role,Guid addedBy)
        {
            ChannelId = channelId;
            UserId = userId;
            Role = role;
            AddedBy = addedBy;
        }
    }
}