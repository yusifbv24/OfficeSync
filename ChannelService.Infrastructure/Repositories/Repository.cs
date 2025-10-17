using ChannelService.Application.Interfaces;
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



        public async Task<T> AddAsync(T entity,CancellationToken cancellationToken=default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return entity;
        }



        public Task UpdateAsync(T entity,CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
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
    }
}