using ChannelService.Application.Interfaces;
using ChannelService.Domain.Entities;
using ChannelService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChannelService.Infrastructure.Repositories
{
    public class UnitOfWork:IUnitOfWork
    {
        private readonly ChannelDbContext _context;
        private IDbContextTransaction? _transaction;

        public IRepository<Channel> Channels { get; }
        public IRepository<ChannelMember> ChannelMembers { get; }
        public UnitOfWork(ChannelDbContext context)
        {
            _context = context;
            Channels = new Repository<Channel>(context);
            ChannelMembers= new Repository<ChannelMember>(context);
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
            if (_transaction != null)
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
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}