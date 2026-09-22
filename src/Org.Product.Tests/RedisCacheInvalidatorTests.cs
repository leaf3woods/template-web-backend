using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Domain.Entities.Account;
using Org.Product.Infrastructure.Adapters.Cache;
using Org.Product.Tests.Support;

namespace Org.Product.Tests;

public sealed class RedisCacheInvalidatorTests
{
    [Fact]
    public async Task EntityKeys_AreInvalidatedByEntityNameAndIds()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var firstKey = CacheKeys.Entity<User, Guid>(firstId);
        var secondKey = CacheKeys.Entity<User, Guid>(secondId);
        var redis = new RedisStub();
        redis.Values[firstKey] = "first";
        redis.Values[secondKey] = "second";
        var invalidator = new RedisCacheInvalidator(redis.Connection.Object);

        var invalidated = await invalidator.InvalidateEntityCacheAsync(
            nameof(User),
            [firstId, secondId]
        );

        Assert.Equal(2, invalidated);
        Assert.DoesNotContain(firstKey, redis.Values.Keys);
        Assert.DoesNotContain(secondKey, redis.Values.Keys);
    }

    [Fact]
    public async Task StoreKeys_AreInvalidatedDirectly()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var tokenKey = CacheKeys.Token(userId);
        var permissionsKey = CacheKeys.Permissions(roleId);
        var redis = new RedisStub();
        redis.Values[tokenKey] = "token";
        redis.Values[permissionsKey] = "[]";
        var invalidator = new RedisCacheInvalidator(redis.Connection.Object);

        var invalidated = await invalidator.InvalidateStoreCacheAsync(
            [tokenKey, permissionsKey]
        );

        Assert.Equal(2, invalidated);
        Assert.DoesNotContain(tokenKey, redis.Values.Keys);
        Assert.DoesNotContain(permissionsKey, redis.Values.Keys);
    }
}
