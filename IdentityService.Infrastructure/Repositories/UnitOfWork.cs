﻿using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace IdentityService.Infrastructure.Repositories
{
    public class UnitOfWork:IUnitOfWork
    {
        private readonly IdentityDbContext _context;
        private IDbContextTransaction? _transaction;

        public IRepository<User> Users { get; }
        public IRepository<RefreshToken> RefreshTokens { get;  }

        public UnitOfWork(IdentityDbContext context)
        {
            _context = context;
            Users = new Repository<User>(context);
            RefreshTokens= new Repository<RefreshToken>(context);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken=default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }



        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
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
                _transaction=null;
            }
        }



        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}