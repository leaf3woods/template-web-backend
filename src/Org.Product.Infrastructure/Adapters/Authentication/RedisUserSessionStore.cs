using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Domain.Entities.Account;
using Org.Product.Infrastructure.Adapters.Cache;
using StackExchange.Redis;

namespace Org.Product.Infrastructure.Adapters.Authentication;

public sealed class RedisUserSessionStore : CacheStore<Guid>, ICacheStore<User, Guid>
{
    public override string Key(Guid id) => CacheKeys.Token(id);

    public RedisUserSessionStore(IConnectionMultiplexer connection)
        : base(connection) { }
}
