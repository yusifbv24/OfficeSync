using ChannelService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChannelService.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Channel> Channels { get;  }
        IRepository<ChannelMember> ChannelMembers { get; }
        DbContext GetContext();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}