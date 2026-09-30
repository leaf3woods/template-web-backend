using Autofac;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Application.Abstractions.Captchas;
using Org.Product.Domain.Entities.Account;
using Org.Product.Infrastructure.Adapters.Authentication;
using Org.Product.Infrastructure.Adapters.Captchas;
using Org.Product.Tests.Support;
using Org.Product.WebApi.Utilities.InjectionModules;
using StackExchange.Redis;

namespace Org.Product.Tests;

public sealed class CacheStoreTests
{
    [Theory]
    [InlineData(false, null, "value", false)]
    [InlineData(false, "value", "value", true)]
    [InlineData(false, "value", "wrong", false)]
    [InlineData(false, "value", "Value", false)]
    [InlineData(false, "", "", false)]
    [InlineData(true, null, "value", false)]
    [InlineData(true, "value", "value", true)]
    [InlineData(true, "value", "wrong", false)]
    [InlineData(true, "value", "Value", false)]
    [InlineData(true, "", "", false)]
    public async Task Matches_RequiresExistingExactValueWithoutConsumingIt(
        bool captcha,
        string? storedValue,
        string expectedValue,
        bool expectedResult
    )
    {
        var redis = new RedisStub();
        var store = CreateStore(redis, captcha);
        var id = Guid.NewGuid();
        if (storedValue is not null)
        {
            await store.SaveAsync(id, storedValue);
        }

        Assert.Equal(expectedResult, await store.MatchesAsync(id, expectedValue));
        Assert.Equal(storedValue is not null, redis.Values.ContainsKey(store.Key(id)));
        if (storedValue is not null)
        {
            Assert.Equal(storedValue, redis.Values[store.Key(id)].ToString());
        }
    }

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(false, "value", true)]
    [InlineData(false, "older-value", false)]
    [InlineData(true, null, true)]
    [InlineData(true, "value", true)]
    [InlineData(true, "older-value", false)]
    public async Task Remove_DeletesOnlyTheRequestedIdAndMatchingValue(
        bool captcha,
        string? expectedValue,
        bool expectedResult
    )
    {
        var redis = new RedisStub();
        var store = CreateStore(redis, captcha);
        var id = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        await store.SaveAsync(id, "value");
        await store.SaveAsync(otherId, "other-value");

        Assert.Equal(expectedResult, await store.RemoveAsync(id, expectedValue));
        Assert.Equal(!expectedResult, redis.Values.ContainsKey(store.Key(id)));
        if (!expectedResult)
        {
            Assert.Equal("value", redis.Values[store.Key(id)].ToString());
        }
        Assert.Equal("other-value", redis.Values[store.Key(otherId)].ToString());
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "value")]
    [InlineData(true, null)]
    [InlineData(true, "value")]
    public async Task Remove_MissingValueReturnsFalse(bool captcha, string? expectedValue)
    {
        var redis = new RedisStub();
        var store = CreateStore(redis, captcha);

        Assert.False(await store.RemoveAsync(Guid.NewGuid(), expectedValue));
        Assert.Empty(redis.Values);
    }

    [Fact]
    public async Task ScopedStores_ResolveAndKeepSameIdValuesSeparate()
    {
        var redis = new RedisStub();
        var builder = new ContainerBuilder();
        builder.RegisterModule<ApplicationAbstractionModule>();
        builder.RegisterInstance(redis.Connection.Object).As<IConnectionMultiplexer>();
        using var container = builder.Build();
        using var scope = container.BeginLifetimeScope();
        var sessions = scope.Resolve<ICacheStore<User, Guid>>();
        var challenges = scope.Resolve<ICacheStore<Captcha, Guid>>();
        var id = Guid.NewGuid();

        await sessions.SaveAsync(id, "token");
        await challenges.SaveAsync(id, "answer");

        Assert.NotEqual(sessions.Key(id), challenges.Key(id));
        Assert.True(await sessions.MatchesAsync(id, "token"));
        Assert.True(await challenges.MatchesAsync(id, "answer"));
        Assert.False(await sessions.MatchesAsync(id, "answer"));
        Assert.False(await challenges.MatchesAsync(id, "token"));

        Assert.True(await sessions.RemoveAsync(id, "token"));
        Assert.True(await challenges.MatchesAsync(id, "answer"));
    }

    private static ICacheStore<Guid> CreateStore(RedisStub redis, bool captcha) =>
        captcha
            ? new RedisCaptchaChallengeStore(redis.Connection.Object)
            : new RedisUserSessionStore(redis.Connection.Object);
}
