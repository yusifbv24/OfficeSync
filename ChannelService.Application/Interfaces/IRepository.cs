using System.Linq.Expressions;

namespace ChannelService.Application.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id,CancellationToken cancellationToken);
        IQueryable<T> GetQueryable();
        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T,bool>> predicate, CancellationToken cancellationToken);
        IQueryable<T> Find(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
        Task<T> AddAsync(T entity,CancellationToken cancellationToken=default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken=default);
        Task DeleteAsync(T entity,CancellationToken cancellationToken=default);
        Task<bool> ExistsAsync(Expression<Func<T,bool>> predicate,CancellationToken cancellationToken=default);
        Task<bool> CountAsync(Expression<Func<T,bool>> predicate, CancellationToken cancellationToken=default);
    }
}