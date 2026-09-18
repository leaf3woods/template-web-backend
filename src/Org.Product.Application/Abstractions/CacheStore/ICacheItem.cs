using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Abstractions.CacheStore;

public interface ICacheItem<TKey> : IHasSortOrder, IEnableable
    where TKey : notnull
{
    TKey Id { get; }
}
