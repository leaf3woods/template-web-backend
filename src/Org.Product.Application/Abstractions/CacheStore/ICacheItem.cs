using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Abstractions.CacheStore;


public interface ICacheItem {}

public interface ICacheItem<TEntity, TKey> : ICacheItem, IEntityDto<TEntity>, IHasSortOrder, IEnableable
    where TEntity : IAggregateRoot
    where TKey : notnull
{
}
