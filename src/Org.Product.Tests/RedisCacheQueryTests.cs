using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Application.Dtos.Cache;
using Org.Product.Application.Utilities.MapperProfiles;
using Org.Product.Application.Utilities.MapperProfiles.CacheItemProfiles;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Entities.Authority;
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

        var first = await query.GetFilteredListAsync(user => user.Username == filter.Username);
        filter.Username = "administrator";
        var second = await query.GetFilteredListAsync(user => user.Username == filter.Username);

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

        var first = await query.GetFilteredListAsync(user => user.Username == "operator");
        var second = await query.GetFilteredListAsync(account => account.Username == "operator");

        Assert.Equal(Assert.Single(first).Id, Assert.Single(second).Id);
        Assert.Contains(redis.Values.Keys, key => key.ToString().Contains(":entities:v2:", StringComparison.Ordinal));
        repository.Verify(item => item.Query(false), Times.Once);
    }

    [Fact]
    public async Task WhereAsync_GenerationChanges_QueriesAgainWithNewListKey()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Username = "operator", IsEnabled = true },
        };
        var repository = new Mock<IRepository<User>>();
        repository.Setup(item => item.Query(false)).Returns(() => new AsyncQuery<User>(users));
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis);
        var invalidator = new RedisCacheInvalidator(redis.Connection.Object);

        var first = await query.GetFilteredListAsync(user => user.IsEnabled);
        users.Add(new User { Id = Guid.NewGuid(), Username = "administrator", IsEnabled = true });
        await invalidator.InvalidateEntityListCacheAsync(nameof(User));
        var second = await query.GetFilteredListAsync(user => user.IsEnabled);

        Assert.Single(first);
        Assert.Equal(2, second.Count);
        Assert.Equal(2, redis.Values.Keys.Count(key => key.ToString().Contains(":entities:v2:user:g", StringComparison.Ordinal)));
        Assert.Equal("1", redis.Values[CacheKeys.EntitiesGeneration<User>()].ToString());
        repository.Verify(item => item.Query(false), Times.Exactly(2));
    }

    [Fact]
    public async Task WhereAsync_PermissionCacheItem_ProjectsFromPermission()
    {
        var permissionId = Guid.NewGuid();
        var repository = new Mock<IRepository<Permission>>();
        repository.Setup(item => item.Query(false)).Returns(new AsyncQuery<Permission>(new[]
        {
            new Permission { Id = permissionId, Name = "View users", Code = "users.view", IsEnabled = true },
        }));
        var redis = new RedisStub();
        var query = new RedisCacheQuery<Permission, Guid, PermissionCacheItem>(
            repository.Object,
            redis.Connection.Object,
            CreateMapper(),
            Options.Create(new CacheOptions())
        );

        var result = await query.GetFilteredListAsync(permission => permission.IsEnabled);

        var item = Assert.Single(result);
        Assert.Equal(permissionId, item.Id);
        Assert.Equal("View users", item.Name);
        Assert.Equal("users.view", item.Code);
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

        var first = await query.GetFilteredListAsync(user => usernames.Contains(user.Username));
        var second = await query.GetFilteredListAsync(user => usernames.Contains(user.Username));

        Assert.Equal("operator", Assert.Single(first).Username);
        Assert.Equal("operator", Assert.Single(second).Username);
        Assert.Empty(redis.Values);
        redis.Connection.Verify(
            connection => connection.GetDatabase(It.IsAny<int>(), It.IsAny<object>()),
            Times.Never
        );
        repository.Verify(item => item.Query(false), Times.Exactly(2));
    }

    [Fact]
    public async Task WhereAsync_UnsupportedMethodCall_BypassesCache()
    {
        var repository = new Mock<IRepository<User>>();
        repository.Setup(item => item.Query(false)).Returns(new AsyncQuery<User>(new[]
        {
            new User { Id = Guid.NewGuid(), Username = "operator", IsEnabled = true },
        }));
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis);

        var first = await query.GetFilteredListAsync(user => user.Username.StartsWith("oper"));
        var second = await query.GetFilteredListAsync(user => user.Username.StartsWith("oper"));

        Assert.Equal("operator", Assert.Single(first).Username);
        Assert.Equal("operator", Assert.Single(second).Username);
        Assert.Empty(redis.Values);
        redis.Connection.Verify(
            connection => connection.GetDatabase(It.IsAny<int>(), It.IsAny<object>()),
            Times.Never
        );
        repository.Verify(item => item.Query(false), Times.Exactly(2));
    }

    [Fact]
    public async Task WhereAsync_ResultExceedsLimit_ThrowsWithoutCaching()
    {
        var repository = new Mock<IRepository<User>>();
        repository.Setup(item => item.Query(false)).Returns(new AsyncQuery<User>(new[]
        {
            new User { Id = Guid.NewGuid(), Username = "operator", IsEnabled = true },
            new User { Id = Guid.NewGuid(), Username = "administrator", IsEnabled = true },
        }));
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis, maxCollectionItems: 1);

        var exception = await Assert.ThrowsAsync<CacheQueryResultTooLargeException>(
            () => query.GetFilteredListAsync(user => user.IsEnabled)
        );

        Assert.Equal(typeof(User), exception.EntityType);
        Assert.Equal(1, exception.MaximumItems);
        Assert.Empty(redis.Values);
        repository.Verify(item => item.Query(false), Times.Once);
    }

    [Fact]
    public async Task WhereAsync_ResultAtLimit_IsCached()
    {
        var repository = new Mock<IRepository<User>>();
        repository.Setup(item => item.Query(false)).Returns(new AsyncQuery<User>(new[]
        {
            new User { Id = Guid.NewGuid(), Username = "operator", IsEnabled = true },
            new User { Id = Guid.NewGuid(), Username = "administrator", IsEnabled = true },
        }));
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis, maxCollectionItems: 2);

        var first = await query.GetFilteredListAsync(user => user.IsEnabled);
        var second = await query.GetFilteredListAsync(user => user.IsEnabled);

        Assert.Equal(2, first.Count);
        Assert.Equal(2, second.Count);
        Assert.Single(redis.Values);
        repository.Verify(item => item.Query(false), Times.Once);
    }

    private static RedisCacheQuery<User, Guid, UserCacheItem> CreateQuery(
        IRepository<User> repository,
        RedisStub redis,
        int maxCollectionItems = 500
    ) => new(
        repository,
        redis.Connection.Object,
        CreateMapper(),
        Options.Create(new CacheOptions
        {
            MaxCollectionItems = maxCollectionItems,
        })
    );

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            config =>
            {
                config.AddProfile<DtoConventionProfile>();
                config.AddProfile<PermissionCacheProfile>();
            },
            NullLoggerFactory.Instance
        ).CreateMapper();

    private sealed class UserFilter
    {
        public string Username { get; set; } = null!;
    }
}
