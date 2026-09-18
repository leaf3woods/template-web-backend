using Org.Product.Application.Abstractions.CacheStore;

namespace Org.Product.Application.Dtos.Base;

public abstract class CacheItem<TEntity> : CacheItem<TEntity, Guid> { }

public abstract class CacheItem<TEntity, TKey> : ReadDto<TEntity, TKey>, ICacheItem<TKey>
    where TKey : notnull
{
    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; }
}
