using Microsoft.EntityFrameworkCore.Storage;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Domain.Repositories;
using Org.Product.Domain.Services;
using Org.Product.Infrastructure.Utilities;

namespace Org.Product.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    public UnitOfWork(
        ApiDbContext dbContext,
        ICacheInvalidator cacheInvalidator,
        ICurrentUserDomainService currentUser
    )
    {
        _dbContext = dbContext;
        _cacheInvalidator = cacheInvalidator;
        _currentUser = currentUser;
    }

    private readonly ApiDbContext _dbContext;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ICurrentUserDomainService _currentUser;

    private readonly Dictionary<string, HashSet<object>> _pendingInvalidations = [];

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entityEntry in _dbContext.ChangeTracker.Entries())
        {
            var _ = entityEntry
                .ApplyPendingInvalidations(_pendingInvalidations)
                .ApplyAuditedData(_currentUser.UserId)
                .ApplySoftDelete();
        }
        var count = await _dbContext.SaveChangesAsync(cancellationToken);
        if (_dbContext.Database.CurrentTransaction is null)
        {
            await InvalidateCacheAsync();
        }
        return count;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active for this scope.");
        }

        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(
            transaction,
            _ => InvalidateCacheAsync(),
            () => _pendingInvalidations.Clear()
        );
    }

    private async Task InvalidateCacheAsync()
    {
        var tasks = _pendingInvalidations.Select(async entry =>
        {
            await _cacheInvalidator.InvalidateEntityCacheAsync(entry.Key, entry.Value);
            await _cacheInvalidator.InvalidateEntityListCacheAsync(entry.Key);
        });
        await Task.WhenAll(tasks);
        _pendingInvalidations.Clear();
    }

    private class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        public EfUnitOfWorkTransaction(
            IDbContextTransaction transaction,
            Func<CancellationToken, Task>? commitFun = null,
            Action? rollbackFun = null
        )
        {
            _transaction = transaction;
            _commitFun = commitFun;
            _rollbackFun = rollbackFun;
        }

        private readonly IDbContextTransaction _transaction;
        private readonly Func<CancellationToken, Task>? _commitFun;
        private readonly Action? _rollbackFun;

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
            if (_commitFun is not null)
            {
                await _commitFun(cancellationToken);
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.RollbackAsync(cancellationToken);
            _rollbackFun?.Invoke();
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}
