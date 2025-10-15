namespace UserManagementService.Domain.Common;

/// <summary>
/// Base entity class that all domain entities inherit from.
/// Contains common properties like ID and timestamps.
/// Using DateTime ensures proper timezone handling for Azerbaijan (UTC+4).
/// All timestamps are stored in UTC in the database.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// Using Guid ensures uniqueness across distributed systems.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// When this entity was created (always stored in UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this entity was last updated (always stored in UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}