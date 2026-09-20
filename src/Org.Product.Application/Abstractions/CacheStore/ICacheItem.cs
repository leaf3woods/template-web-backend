using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Abstractions.CacheStore;


public interface ICacheItem
{
    string CacheKey { get; set; }
}

public interface ICacheItem<TEntity> : ICacheItem, IEntityDto<TEntity>, IHasSortOrder, IEnableable
    where TEntity : IAggregateRoot
{
}
