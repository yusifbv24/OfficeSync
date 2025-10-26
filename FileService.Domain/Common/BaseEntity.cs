namespace FileService.Domain.Common
{
    public abstract class BaseEntity
    {
        /// <summary>
        /// List of domain events that have occurred on this entity.
        /// These events are dispatched after the entity is successfully persisted,
        /// allowing other parts of the system to react to state changes.
        /// </summary>
        private readonly List<IDomainEvent> _domainEvents = [];


        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected BaseEntity()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        protected void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }


        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear(); 
        }
    }
}