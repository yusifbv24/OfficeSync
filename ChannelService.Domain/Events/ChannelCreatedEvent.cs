using ChannelService.Domain.Common;
using ChannelService.Domain.Enums;

namespace ChannelService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a channel is created
    /// </summary>
    public class ChannelCreatedEvent:DomainEvent
    {
        public Guid ChannelId { get; }
        public string Name { get; }
        public ChannelType Type { get; }
        public Guid CreatedBy { get; }
        public ChannelCreatedEvent(Guid channelId,string name, ChannelType type, Guid createdBy)
        {
            ChannelId = channelId;
            Name = name;
            Type = type;
            CreatedBy = createdBy;
        }
    }
}