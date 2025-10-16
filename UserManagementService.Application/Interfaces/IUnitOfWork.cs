using UserManagementService.Domain.Entities;

namespace UserManagementService.Application.Interfaces
{
    /// <summary>
    /// Unit of Work pattern for coordinating repositories and transactions.
    /// </summary>
    public interface IUnitOfWork:IDisposable
    {
        IRepository<UserProfile> UserProfiles { get; }
        IRepository<UserRoleAssignment> RoleAssignments { get; }
        IRepository<UserPermission> Permissions { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}