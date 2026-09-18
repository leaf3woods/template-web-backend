using Org.Product.Application.Dtos.Base;

namespace Org.Product.Application.Abstractions.CacheStore;

public interface ICacheQuery<TKey, in TQuery, TItem>
    where TKey : notnull
    where TQuery : QueryDto
    where TItem : ICacheItem<TKey>
{
    Task<TItem?> GetByIdAsync(
        TKey id,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<TItem>> ListAsync(
        TQuery query,
        CancellationToken cancellationToken = default
    );
}
