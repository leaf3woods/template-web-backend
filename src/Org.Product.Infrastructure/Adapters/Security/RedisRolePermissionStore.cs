using System.Text.Json;
using Org.Product.Application.Abstractions.Security;
using StackExchange.Redis;

namespace Org.Product.Infrastructure.Adapters.Security;

public class RedisRolePermissionStore : IRolePermissionStore
{
    public RedisRolePermissionStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public async Task<IEnumerable<string>> GetPermissionsAsync(params IEnumerable<Guid> roleIds)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var keys = roleIds
            .Distinct()
            .Select(roleId => new RedisKey(
                string.Format(CacheKeyFormatter.Permissions, roleId)
            ))
            .ToArray();

        if (keys.Length == 0)
        {
            return [];
        }

        var rawValues = await database.StringGetAsync(keys);
        var rawHasValues = rawValues.Where(raw => raw.HasValue);

        if (keys.Length != rawHasValues.Count())
        {
            return [];
        }

        var permissions = rawHasValues
            .Select(raw =>
                JsonSerializer.Deserialize<IEnumerable<string>>(raw.ToString()) ?? []
            )
            .SelectMany(permission => permission)
            .Distinct();

        return permissions;
    }

    public async Task<bool> ContainsAsync(params IEnumerable<Guid> roleIds)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var scanTasks = roleIds
            .Distinct()
            .Select(roleId => new RedisKey(
                string.Format(CacheKeyFormatter.Permissions, roleId)
            ))
            .Select(key => database.KeyExistsAsync(key));
        if (!scanTasks.Any())
        {
            return false;
        }
        return (await Task.WhenAll(scanTasks)).All(exists => exists);
    }

    public async Task SaveAsync(
        Guid roleId,
        IEnumerable<string> permissions,
        TimeSpan? expiration = null
    )
    {
        var database = _connectionMultiplexer.GetDatabase();
        var key = string.Format(CacheKeyFormatter.Permissions, roleId);
        var json = JsonSerializer.Serialize(permissions);
        var task = expiration is null
            ? database.StringSetAsync(key, json)
            : database.StringSetAsync(key, json, expiration.Value);
        await task;
    }
}
