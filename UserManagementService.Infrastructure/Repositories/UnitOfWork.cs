using Microsoft.EntityFrameworkCore.Storage;
using UserManagementService.Application.Interfaces;
using UserManagementService.Domain.Entities;
using UserManagementService.Infrastructure.Data;

namespace UserManagementService.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work implementation coordinating multiple repositories.
    /// Ensures all operations succeed or fail together as a single transaction.
    /// </summary>
    public class UnitOfWork:IUnitOfWork
    {
        private readonly UserManagementDbContext _context;
        private IDbContextTransaction? _transaction;

        public IRepository<UserProfile> UserProfiles { get; }
        public IRepository<UserRoleAssignment> RoleAssignments { get; }
        public IRepository<UserPermission> Permissions { get; }

        public UnitOfWork(UserManagementDbContext context)
        {
            _context = context;
            UserProfiles= new Repository<UserProfile>(_context);
            RoleAssignments = new Repository<UserRoleAssignment>(_context);
            Permissions = new Repository<UserPermission>(_context);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction=await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if(_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if(_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            _transaction?.Dispose();
        }
    }
}