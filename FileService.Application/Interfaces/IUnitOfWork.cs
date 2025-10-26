namespace FileService.Application.Interfaces
{
    /// <summary>
    /// Unit of Work interface for transaction management.
    /// The Unit of Work pattern ensures that multiple repository operations
    /// can be committed or rolled back together as a single atomic transaction.
    /// 
    /// This is critical for maintaining data consistency. For example, when uploading
    /// a file, you might need to:
    /// 1. Insert the file record
    /// 2. Update storage quota information
    /// 3. Create audit log entries
    /// 
    /// All these operations should succeed together or fail together.
    /// The Unit of Work ensures this atomicity.
    /// 
    /// In Entity Framework Core, DbContext already implements the Unit of Work pattern,
    /// so this interface typically just wraps DbContext.SaveChangesAsync().
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Saves all pending changes to the database as a single transaction.
        /// Returns the number of entities affected.
        /// 
        /// If any operation fails, the entire transaction is rolled back,
        /// ensuring data remains consistent.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins an explicit database transaction.
        /// Most operations don't need explicit transactions because SaveChangesAsync
        /// already wraps changes in a transaction. However, complex multi-step operations
        /// might need explicit transaction control for additional flexibility.
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current explicit transaction.
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current explicit transaction.
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}