using System.Linq.Expressions;
using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Abstractions.CacheStore;

public interface ICacheQuery<TEntity, TKey, TCacheItem>
    where TEntity : AggregateRoot<TKey>
    where TKey : notnull, new()
    where TCacheItem : CacheItem<TEntity, TKey>
{
    Task<TCacheItem?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    Task<TCacheItem?> FindWithDetailAsync(TKey id, CancellationToken cancellationToken = default);

    Task<TCacheItem?> FindWithDetailAsync<TRelation>(
        TKey id,
        CancellationToken cancellationToken = default
    )
        where TRelation : IAggregateRoot;

    Task<TCacheItem?> FindWithDetailAsync<TRelation1, TRelation2>(
        TKey id,
        CancellationToken cancellationToken = default
    )
        where TRelation1 : IAggregateRoot
        where TRelation2 : IAggregateRoot;

    Task<IReadOnlyDictionary<TKey, TCacheItem>> FindManyWithDetailAsync(
        IReadOnlyCollection<TKey> ids,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyDictionary<TKey, TCacheItem>> FindManyWithDetailAsync<TRelation>(
        IReadOnlyCollection<TKey> ids,
        CancellationToken cancellationToken = default
    )
        where TRelation : IAggregateRoot;

    Task<IReadOnlyDictionary<TKey, TCacheItem>> FindManyWithDetailAsync<TRelation1, TRelation2>(
        IReadOnlyCollection<TKey> ids,
        CancellationToken cancellationToken = default
    )
        where TRelation1 : IAggregateRoot
        where TRelation2 : IAggregateRoot;

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
