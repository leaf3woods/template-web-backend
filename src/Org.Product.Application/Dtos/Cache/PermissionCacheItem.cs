using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Authority;

namespace Org.Product.Application.Dtos.Cache;

public sealed class PermissionCacheItem : CacheItem<Permission, Guid>, IManualDtoMapping
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;
}
