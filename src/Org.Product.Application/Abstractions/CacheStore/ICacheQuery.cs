using System.Linq.Expressions;
using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Abstractions.CacheStore;

public interface ICacheQuery<TEntity, TKey, TCacheItem>
    where TEntity : IAggregateRoot
    where TKey : notnull
    where TCacheItem : CacheItem<TEntity, TKey>
{
    Task<TCacheItem?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TCacheItem>> GetFilteredListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<TCacheItem>> GetFilteredListAsync<TRelation>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
        where TRelation : IAggregateRoot;

    Task<IReadOnlyList<TCacheItem>> GetFilteredListAsync<TRelation1, TRelation2>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
        where TRelation1 : IAggregateRoot
        where TRelation2 : IAggregateRoot;
}
