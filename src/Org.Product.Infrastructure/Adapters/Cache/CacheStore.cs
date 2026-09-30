using System.Text.Json;
using Org.Product.Application.Abstractions.CacheStore;
using StackExchange.Redis;

namespace Org.Product.Infrastructure.Adapters.Cache;

public abstract class CacheStore<TId> : ICacheStore<TId>
    where TId : notnull, IEquatable<TId>
{
    public readonly IConnectionMultiplexer Connection;

    protected CacheStore(IConnectionMultiplexer connection)
    {
        Connection = connection;
    }

    public abstract string Key(TId id);

    public async Task SaveAsync<TContent>(TId id, TContent content, TimeSpan? expiration = null)
    {
        var database = Connection.GetDatabase();
        var key = Key(id);
        var plain = content switch
        {
            IEquatable<TContent> => $"{content}",
            _ => JsonSerializer.Serialize(content),
        };
        await (
            expiration is null
                ? database.StringSetAsync(key, plain)
                : database.StringSetAsync(key, plain, expiration.Value)
        );
    }

    public async Task<bool> MatchesAsync(TId id, string expectedValue)
    {
        var value = await Connection.GetDatabase().StringGetAsync(Key(id));
        return value.HasValue && value.ToString() == expectedValue;
    }

    public async Task<bool> RemoveAsync(TId id, string? expectedValue = null)
    {
        var database = Connection.GetDatabase();
        var key = Key(id);
        if (expectedValue is null)
        {
            return await database.KeyDeleteAsync(key);
        }

        // Compare and delete atomically so a newer value cannot be removed by an older request.
        var result = await database.ScriptEvaluateAsync(
            "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end",
            [new RedisKey(key)],
            [new RedisValue(expectedValue)]
        );
        return (long)result == 1;
    }
}
