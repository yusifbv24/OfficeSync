using ChannelService.Application.Interfaces;
using ChannelService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ChannelService.Infrastructure.Repositories
{
    public class Repository<T>:IRepository<T> where T :class
    {
        protected readonly ChannelDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public Repository(ChannelDbContext context)
        {
            _context = context;
            _dbSet=context.Set<T>();
        }


        public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _dbSet.FindAsync(new object[] { id },cancellationToken);
        }


        public async Task<T?> GetByIdWithIncludesAsync(
            Guid id,
            CancellationToken cancellationToken,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            foreach(var include in includes)
            {
                query = query.Include(include);
            }
            return await query
                .AsNoTracking()
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



        public IQueryable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate);
        }



        public async Task<T> AddAsync(T entity,CancellationToken cancellationToken=default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return entity;
        }



        public Task UpdateAsync(T entity,CancellationToken cancellationToken = default)
        {
            var entry= _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _context.Attach(entity);
                entry.State = EntityState.Modified;
            }
            _context.ChangeTracker.DetectChanges();
            return Task.CompletedTask;
        }


        public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }



        public async Task<bool> ExistsAsync(
            Expression<Func<T,bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }


        public async Task<int> CountAsync(
            Expression<Func<T,bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return predicate==null
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