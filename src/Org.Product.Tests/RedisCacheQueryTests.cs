using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Org.Product.Application.Dtos.Cache;
using Org.Product.Application.Utilities.MapperProfiles;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Repositories;
using Org.Product.Infrastructure.Adapters.Cache;
using Org.Product.Infrastructure.Utilities.Options;
using Org.Product.Tests.Support;

namespace Org.Product.Tests;

public sealed class RedisCacheQueryTests
{
    [Fact]
    public async Task WhereAsync_CapturedValueDiffers_UsesDifferentCacheEntries()
    {
        var users = new[]
        {
            new User { Id = Guid.NewGuid(), Username = "operator", IsEnabled = true },
            new User { Id = Guid.NewGuid(), Username = "administrator", IsEnabled = true },
        };
        var repository = new Mock<IRepository<User>>();
        repository.Setup(item => item.Query(false)).Returns(new AsyncQuery<User>(users));
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis);
        var filter = new UserFilter { Username = "operator" };

        var first = await query.WhereAsync(user => user.Username == filter.Username);
        filter.Username = "administrator";
        var second = await query.WhereAsync(user => user.Username == filter.Username);

        Assert.Equal("operator", Assert.Single(first).Username);
        Assert.Equal("administrator", Assert.Single(second).Username);
        Assert.Equal(2, redis.Values.Count);
        repository.Verify(item => item.Query(false), Times.Exactly(2));
    }

    [Fact]
    public async Task WhereAsync_EquivalentExpressionUsesCachedResult()
    {
        var repository = new Mock<IRepository<User>>();
        repository.Setup(item => item.Query(false)).Returns(new AsyncQuery<User>(new[]
        {
            new User { Id = Guid.NewGuid(), Username = "operator", IsEnabled = true },
        }));
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis);

        var first = await query.WhereAsync(user => user.Username == "operator");
        var second = await query.WhereAsync(account => account.Username == "operator");

        Assert.Equal(Assert.Single(first).Id, Assert.Single(second).Id);
        Assert.Contains(redis.Values.Keys, key => key.ToString().Contains(":entities:v2:", StringComparison.Ordinal));
        repository.Verify(item => item.Query(false), Times.Once);
    }

    [Fact]
    public async Task WhereAsync_UnsupportedCapturedValue_BypassesCache()
    {
        var users = new[]
        {
            new User { Id = Guid.NewGuid(), Username = "operator", IsEnabled = true },
            new User { Id = Guid.NewGuid(), Username = "administrator", IsEnabled = true },
        };
        var repository = new Mock<IRepository<User>>();
        repository.Setup(item => item.Query(false)).Returns(new AsyncQuery<User>(users));
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis);
        var usernames = new[] { "operator" };

        var first = await query.WhereAsync(user => usernames.Contains(user.Username));
        var second = await query.WhereAsync(user => usernames.Contains(user.Username));

        Assert.Equal("operator", Assert.Single(first).Username);
        Assert.Equal("operator", Assert.Single(second).Username);
        Assert.Empty(redis.Values);
        redis.Connection.Verify(
            connection => connection.GetDatabase(It.IsAny<int>(), It.IsAny<object>()),
            Times.Never
        );
        repository.Verify(item => item.Query(false), Times.Exactly(2));
    }

    private static RedisCacheQuery<User, Guid, UserCacheItem> CreateQuery(
        IRepository<User> repository,
        RedisStub redis
    ) => new(
        repository,
        redis.Connection.Object,
        CreateMapper(),
        Options.Create(new CacheOptions())
    );

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            config => config.AddProfile<DtoConventionProfile>(),
            NullLoggerFactory.Instance
        ).CreateMapper();

    private sealed class UserFilter
    {
        public string Username { get; set; } = null!;
    }
}
