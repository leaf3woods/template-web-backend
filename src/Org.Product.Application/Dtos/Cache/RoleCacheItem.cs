using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Dtos.Cache;

public sealed class RoleCacheItem : CacheItem<Role, Guid>
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;
}
