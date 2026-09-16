using Org.Product.Application.Abstractions.Security;
using StackExchange.Redis;

namespace Org.Product.Infrastructure.Adapters.Security;

public sealed class RedisUserSessionStore : IUserSessionStore
{
    private readonly IConnectionMultiplexer _connection;

    public RedisUserSessionStore(IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public async Task SaveAsync(Guid userId, string token, TimeSpan? expiration = null)
    {
        var database = _connection.GetDatabase();
        var key = string.Format(CacheKeyFormatter.Token, userId);
        await (expiration is null
            ? database.StringSetAsync(key, token)
            : database.StringSetAsync(key, token, expiration.Value));
    }

    public async Task<bool> IsValidAsync(Guid userId, string token)
    {
        var value = await _connection.GetDatabase()
            .StringGetAsync(string.Format(CacheKeyFormatter.Token, userId));
        return value.HasValue && value.ToString() == token;
    }

    public async Task<bool> RevokeAsync(Guid userId, string? expectedToken = null)
    {
        var database = _connection.GetDatabase();
        var key = string.Format(CacheKeyFormatter.Token, userId);
        if (expectedToken is null)
        {
            return await database.KeyDeleteAsync(key);
        }

        // An older in-flight logout must not delete a newer login session.
        var result = await database.ScriptEvaluateAsync(
            "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end",
            [new RedisKey(key)],
            [new RedisValue(expectedToken)]
        );
        return (long)result == 1;
    }
}
