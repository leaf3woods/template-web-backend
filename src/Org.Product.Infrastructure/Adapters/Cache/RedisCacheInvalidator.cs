using Org.Product.Application.Abstractions.CacheStore;
using StackExchange.Redis;

namespace Org.Product.Infrastructure.Adapters.Cache;

public sealed class RedisCacheInvalidator : ICacheInvalidator
{
    public RedisCacheInvalidator(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public Task<int> InvalidateEntityCacheAsync(
        string entityName,
        IReadOnlyCollection<object> ids
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(ids);

        var keys = new string[ids.Count];
        var index = 0;
        foreach (var id in ids)
        {
            ArgumentNullException.ThrowIfNull(id);
            keys[index++] = CacheKeys.Entity(entityName, id);
        }
        return InvalidateStoreCacheAsync(keys);
    }

    public async Task<int> InvalidateStoreCacheAsync(IReadOnlyCollection<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count == 0)
        {
            return 0;
        }

        foreach (var key in keys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
        }

        var database = _connectionMultiplexer.GetDatabase();
        var tasks = keys.Select(key => database.KeyDeleteAsync(key));
        return (await Task.WhenAll(tasks)).Count(deleted => deleted);
    }
}
