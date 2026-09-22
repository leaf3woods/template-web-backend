using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Domain.Entities.Base;
using Org.Product.Domain.Repositories;

namespace Org.Product.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    public UnitOfWork(ApiDbContext dbContext, ICacheInvalidator cacheInvalidator)
    {
        _dbContext = dbContext;
        _cacheInvalidator = cacheInvalidator;
    }

    private readonly ApiDbContext _dbContext;
    private readonly ICacheInvalidator _cacheInvalidator;

    private readonly Dictionary<string, HashSet<object>> _pendingInvalidations = [];

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entityEntry in _dbContext.ChangeTracker.Entries())
        {
            if (entityEntry.State is EntityState.Deleted or EntityState.Modified)
            {
                var primaryKey = entityEntry.Metadata.FindPrimaryKey();
                var entityName = entityEntry.Metadata.ClrType.Name;
                if (primaryKey is not null)
                {
                    var key = entityEntry.Property(primaryKey.Properties[0].Name).CurrentValue;
                    if (_pendingInvalidations.TryGetValue(entityName, out var list))
                    {
                        list.Add(key!);
                    }
                    else
                    {
                        _pendingInvalidations.Add(entityName, [key!]);
                    }
                }
            }
            if (
                entityEntry.State == EntityState.Deleted
                && entityEntry.Entity is ISoftDelete delete
            )
            {
                entityEntry.State = EntityState.Unchanged;
                delete.SoftDeleted = true;
                delete.DeleteTime = DateTime.UtcNow;
            }
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

    private async Task<int> InvalidateCacheAsync()
    {
        var tasks = _pendingInvalidations.Select(entry =>
            _cacheInvalidator.InvalidateEntityCacheAsync(entry.Key, entry.Value)
        );
        var result = await Task.WhenAll(tasks);
        _pendingInvalidations.Clear();
        return result.Sum(c => c);
    }

    private class EfUnitOfWorkTransaction
        : IUnitOfWorkTransaction
    {
        public EfUnitOfWorkTransaction(
            IDbContextTransaction transaction,
            Func<CancellationToken, Task>? commitFun = null,
            Action? rollbackFun = null)
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
