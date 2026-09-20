using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Dtos.Base;

public abstract class CacheItem : ICacheItem
{
    public string CacheKey { get; set; } = null!;
}

public abstract class CacheItem<TEntity, TKey> : CacheItem, ICacheItem<TEntity>
    where TEntity : IAggregateRoot
    where TKey : notnull
{
    public TKey Id { get; set; } = default!;

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; }
}
