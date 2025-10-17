namespace ChannelService.Domain.Common
{
    /// <summary>
    /// Base class for domain events.
    /// Events represent important domain occurences.
    /// </summary>
    public abstract class DomainEvent
    {
        public Guid EventId { get; }
        public DateTime OccuredOn { get; }
        protected DomainEvent()
        {
            EventId = Guid.NewGuid();
            OccuredOn = DateTime.UtcNow;
        }
    }
}