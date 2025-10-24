using MessagingService.Application.Interfaces;
using MessagingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MessagingService.Infrastructure.Repositories
{
    public class Repository<T>:IRepository<T> where T : class
    {
        protected readonly MessagingDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(MessagingDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }


        public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }


        public async Task<T?> GetByIdWithIncludesAsync(
            Guid id,
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            // Apply each include to eagerly load related entities
            foreach(var include in includes)
            {
                query= query.Include(include);
            }

            // Use EF.Property to access the Id property generically
            return await query
                .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
        }


        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }


        public async Task<T?> GetFirstOrDefaultAsync(
            Expression<Func<T,bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        }


        public IQueryable<T> Find(Expression<Func<T,bool>> predicate)
        {
            return _dbSet.Where(predicate);
        }


        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return entity;
        }


        public Task UpdateAsync(T entity,CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }


        public Task DeleteAsync(T entity,CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }


        public async Task<bool> ExistsAsync(
            Expression<Func<T,bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate,cancellationToken);
        }


        public async Task<int> CountAsync(
            Expression<Func<T,bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return predicate == null
                ? await _dbSet.CountAsync(cancellationToken)
                : await _dbSet.CountAsync(predicate, cancellationToken);
        }


        public async Task<int> CountAsync(IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            return await query.CountAsync(cancellationToken);
        }


        public async Task<List<T>> ToListAsync(IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            return await query.ToListAsync(cancellationToken);
        }


        public async Task<T?> FirstOrDefaultAsync(IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            return await query.FirstOrDefaultAsync(cancellationToken);
        }
    }
}