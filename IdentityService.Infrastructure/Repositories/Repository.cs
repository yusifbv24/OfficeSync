using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IdentityService.Infrastructure.Repositories
{
    public class Repository<T>:IRepository<T> where T : class
    {
        protected readonly IdentityDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(IdentityDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id,CancellationToken cancellationToken=default)
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }



        public async Task<T?> GetFirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken=default)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate,cancellationToken);
        }



        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken=default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }



        public async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T,bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        }



        public async Task<T> AddAsync(T entity,CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity,cancellationToken);
            return entity;
        }



        public Task UpdateAsync(T entity,CancellationToken cancellationToken=default)
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
            CancellationToken cancellationToken=default)
        {
            return await _dbSet.AnyAsync(predicate,cancellationToken);
        }
    }
}