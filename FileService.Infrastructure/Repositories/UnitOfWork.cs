using FileService.Application.Interfaces;
using FileService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace FileService.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of the Unit of Work pattern using Entity Framework Core DbContext.
    /// 
    /// The Unit of Work pattern provides a way to group multiple database operations
    /// into a single transaction that can be committed or rolled back together.
    /// This ensures data consistency - either all operations succeed, or none do.
    /// 
    /// For example, when uploading a file, you might need to:
    /// 1. Insert the file record
    /// 2. Update user's storage quota
    /// 3. Create an audit log entry
    /// 
    /// Using the Unit of Work pattern ensures these operations are atomic - they all
    /// succeed together or fail together. If any step fails, the entire transaction
    /// is rolled back, leaving the database in a consistent state.
    /// 
    /// In Entity Framework Core, the DbContext already implements the Unit of Work
    /// pattern internally through its ChangeTracker and SaveChanges methods.
    /// This class simply wraps that functionality and makes it explicit.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FileServiceDbContext _context;
        private IDbContextTransaction? _transaction;
        public UnitOfWork(FileServiceDbContext context)
        {
            _context= context;
        }


        /// <summary>
        /// Begins an explicit database transaction.
        /// 
        /// Most operations don't need explicit transactions because SaveChangesAsync
        /// already wraps changes in a transaction. However, you might need explicit
        /// transactions for:
        /// 
        /// 1. Multi-step operations that need to save at different points
        /// 2. Operations spanning multiple DbContext instances
        /// 3. Complex workflows where you need fine-grained transaction control
        /// 
        /// Example usage:
        /// await BeginTransactionAsync();
        /// try
        /// {
        ///     // Do multiple SaveChanges calls
        ///     await SaveChangesAsync();
        ///     // Do more work
        ///     await SaveChangesAsync();
        ///     
        ///     await CommitTransactionAsync();
        /// }
        /// catch
        /// {
        ///     await RollbackTransactionAsync();
        /// }
        /// </summary>
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already in progress");
            }
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }


        /// <summary>
        /// Commits the current explicit transaction.
        /// All changes made since BeginTransaction are permanently saved to the database.
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if(_transaction == null)
            {
                throw new InvalidOperationException("No transaction in progress");
            }

            try
            {
                await _transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction= null;
            }
        }


        /// <summary>
        /// Rolls back the current explicit transaction.
        /// All changes made since BeginTransaction are discarded.
        /// The database remains in the state it was before BeginTransaction was called.
        /// </summary>
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction in progress");
            }
            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction= null;
            }
        }


        /// <summary>
        /// Saves all pending changes to the database in a single transaction.
        /// 
        /// This method:
        /// 1. Detects all changes tracked by the DbContext (inserts, updates, deletes)
        /// 2. Generates SQL commands for those changes
        /// 3. Executes them in a transaction
        /// 4. Updates entity state (sets generated IDs, timestamps, etc.)
        /// 5. Publishes domain events (handled by the DbContext override)
        /// 
        /// If any step fails, the entire transaction is rolled back automatically
        /// by EF Core, ensuring no partial changes are saved.
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}