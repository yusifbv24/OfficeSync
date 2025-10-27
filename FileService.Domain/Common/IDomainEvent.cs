using MediatR;

namespace FileService.Domain.Common
{
    /// <summary>
    /// Marker interface for all domain events in the system.
    /// Domain events represent something significant that happened within the domain,
    /// such as a file being uploaded, deleted, or accessed. Other parts of the system
    /// can subscribe to these events to react to domain changes without tight coupling.
    /// 
    /// Domain events follow the event sourcing pattern and are immutable,
    /// which is why they are typically implemented as records rather than classes.
    /// </summary>
    public interface IDomainEvent:INotification
    {
    }
}