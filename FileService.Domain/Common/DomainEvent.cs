namespace FileService.Domain.Common
{
    /// <summary>
    /// Base class for domain events.
    /// Events represent important business occurrences that other parts of the system may need to react to.
    /// For example, when a file is uploaded, we raise a FileUploadedEvent that could trigger virus scanning.
    /// </summary>
    public abstract class DomainEvent
    {
        public Guid EventId { get; }
        public DateTime OccuredId { get; }
        protected DomainEvent()
        {
            EventId=Guid.NewGuid();
            OccuredId=DateTime.UtcNow;
        }
    }
}