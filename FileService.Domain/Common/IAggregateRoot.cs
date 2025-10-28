namespace FileService.Domain.Common
{
    /// <summary>
    /// Marker interface for aggregate roots.
    /// Only aggregate roots should be accessed directly by repositories.
    /// In this service, FileMetadata is the aggregate root that controls access to FileAccessLog entities.
    /// </summary>
    public interface IAggregateRoot
    {
    }
}