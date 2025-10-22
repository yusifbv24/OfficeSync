namespace MessagingService.Domain.Common
{
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