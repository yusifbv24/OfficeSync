using System.Linq.Expressions;

namespace MessagingService.Application.Interfaces
{
    public interface IRepository<T> where T: class
    {
        /// <summary>
        /// Get an entity by its ID.
        /// </summary>
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);


        /// <summary>
        /// Get an entity by ID with related entities eagerly loaded.
        /// Use this when you need navigation properties loaded.
        /// </summary>
        Task<T?> GetByIdWithIncludesAsync(
            Guid id,
            CancellationToken cancellationToken,
            params Expression<Func<T, object>>[] includes);



        /// <summary>
        /// Get a queryable for building complex queries with deferred execution.
        /// This is the foundation of the IQueryable pattern for performance.
        /// </summary>
        IQueryable<T> GetQueryable();



        /// <summary>
        /// Find entities matching a predicate.
        /// Returns IQueryable for further query building.
        /// </summary>
        IQueryable<T> Find(Expression<Func<T, bool>> predicate);



        /// <summary>
        /// Get the first entity matching a predicate or null.
        /// </summary>
        Task<T?> GetFirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken);



        /// <summary>
        /// Add a new entity to the database.
        /// </summary>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);




        /// <summary>
        /// Update an existing entity.
        /// </summary>
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);



        /// <summary>
        /// Delete an entity from the database.
        /// </summary>
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);



        /// <summary>
        /// Check if any entities match the predicate.
        /// </summary>
        Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken);




        /// <summary>
        /// Count entities matching the predicate.
        /// </summary>
        Task<int> CountAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken);



        /// <summary>
        /// Count entities from a queryable.
        /// Used with IQueryable pattern.
        /// </summary>
        Task<int> CountAsync(IQueryable<T> query, CancellationToken cancellationToken = default);




        /// <summary>
        /// Convert a queryable to a list.
        /// This is where the query actually executes.
        /// </summary>
        Task<List<T>> ToListAsync(IQueryable<T> query, CancellationToken cancellationToken = default);



        /// <summary>
        /// Get first or default from a queryable.
        /// This is where the query actually executes.
        /// </summary>
        Task<T?> FirstOrDefaultAsync(IQueryable<T> query, CancellationToken cancellationToken = default);
    }
}