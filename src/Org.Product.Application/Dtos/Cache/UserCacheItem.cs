using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Dtos.Cache;

public sealed class UserCacheItem : CacheItem<User>
{
    public string Username { get; set; } = null!;

    public string? Name { get; set; }
}
