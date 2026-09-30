using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Application.Abstractions.Captchas;
using Org.Product.Infrastructure.Adapters.Cache;
using StackExchange.Redis;

namespace Org.Product.Infrastructure.Adapters.Captchas;

public sealed class RedisCaptchaChallengeStore : CacheStore<Guid>, ICacheStore<Captcha, Guid>
{
    public override string Key(Guid id) => CacheKeys.Captcha(id);

    public RedisCaptchaChallengeStore(IConnectionMultiplexer connection)
        : base(connection) { }
}
