using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Dtos.Cache;

public sealed class RolePermissionsCacheItem : CacheItem<Role, Guid>, IManualDtoMapping
{
    public string[] PermissionCodes { get; set; } = [];
}
