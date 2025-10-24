using MessagingService.Application.Interfaces;
using MessagingService.Domain.Entities;
using MessagingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MessagingService.Infrastructure.Repositories
{
    public class UnitOfWork:IUnitOfWork
    {
        private readonly MessagingDbContext _context;
        private IDbContextTransaction? _transaction;

        public IRepository<Message> Messages { get; }
        public IRepository<MessageReaction> Reactions { get; }
        public IRepository<MessageAttachment> Attachments { get; }

        public UnitOfWork(MessagingDbContext context)
        {
            _context = context;
            Messages= new Repository<Message>(context);
            Reactions= new Repository<MessageReaction>(context);
            Attachments= new Repository<MessageAttachment>(context);
        }


        public DbContext GetContext()=> _context;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction= await _context.Database.BeginTransactionAsync(cancellationToken);
        }


        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction=null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}