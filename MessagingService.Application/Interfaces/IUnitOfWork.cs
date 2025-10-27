using MessagingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MessagingService.Application.Interfaces
{
    public interface IUnitOfWork:IDisposable
    {
        IRepository<Message> Messages { get; }
        IRepository<MessageReaction> Reactions { get; }
        DbContext GetContext();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken=default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}