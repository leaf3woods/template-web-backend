using Moq;
using Org.Product.Domain.Shared;
using Org.Product.Infrastructure.Adapters.Security;
using Org.Product.Tests.Support;
using StackExchange.Redis;

namespace Org.Product.Tests;

public sealed class PermissionCacheTests
{
    [Fact]
    public async Task CachedPermissions_AreReadWithoutWriting()
    {
        var redis = new RedisStub();
        var roleId = Guid.NewGuid();
        redis.Values[string.Format(CacheKeyFormatter.Permissions, roleId)] = "[\"menu\",\"menu.get\"]";
        var service = new RedisRolePermissionStore(redis.Connection.Object);

        Assert.Equal(["menu", "menu.get"], await service.GetPermissionsAsync(roleId));
        Assert.True(await service.ContainsAsync(roleId));
        AssertNoWrites(redis);
    }

    [Fact]
    public async Task MissingCache_ReturnsNoPermissionsWithoutFillingCache()
    {
        var redis = new RedisStub();
        var service = new RedisRolePermissionStore(redis.Connection.Object);
        var roleId = Guid.NewGuid();

        Assert.Empty(await service.GetPermissionsAsync(roleId));
        Assert.False(await service.ContainsAsync(roleId));
        Assert.Empty(redis.Values);
        AssertNoWrites(redis);
    }

    [Fact]
    public async Task PartiallyMissingCache_DoesNotGrantPartialRolePermissions()
    {
        var redis = new RedisStub();
        var roleId = Guid.NewGuid();
        redis.Values[string.Format(CacheKeyFormatter.Permissions, roleId)] = "[\"menu\"]";
        var service = new RedisRolePermissionStore(redis.Connection.Object);

        Assert.Empty(await service.GetPermissionsAsync([roleId, Guid.NewGuid()]));
        Assert.Single(redis.Values);
        AssertNoWrites(redis);
    }

    [Fact]
    public async Task EmptyPermissionSet_IsAnExistingRoleCache()
    {
        var redis = new RedisStub();
        var roleId = Guid.NewGuid();
        redis.Values[string.Format(CacheKeyFormatter.Permissions, roleId)] = "[]";
        var service = new RedisRolePermissionStore(redis.Connection.Object);

        Assert.Empty(await service.GetPermissionsAsync(roleId));
        Assert.True(await service.ContainsAsync(roleId));
        AssertNoWrites(redis);
    }

    private static void AssertNoWrites(RedisStub redis)
    {
        redis.Database.Verify(database => database.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()), Times.Never);
    }
}
