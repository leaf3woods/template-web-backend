using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
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
using Org.Product.Infrastructure.Repositories;
using Org.Product.Infrastructure.Utilities.Options;
using Org.Product.Tests.Support;

namespace Org.Product.Tests;

public sealed class RedisCacheQueryTests
{
    [Fact]
    public async Task FindAsync_DifferentProjectionTypesAndGenerationUseSeparateKeys()
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "reader",
            Code = "reader",
            Permissions = [new Permission { PermissionCode = "menu.read" }],
        };
        var repository = new Mock<IRepository<Role>>();
        repository.Setup(item => item.FindAsync(It.IsAny<object?[]>())).ReturnsAsync(() => role);
        var redis = new RedisStub();
        var roleQuery = new RedisCacheQuery<Role, Guid, RoleCacheItem>(
            repository.Object,
            redis.Connection.Object,
            CreateMapper(),
            Options.Create(new CacheOptions())
        );
        var permissionsQuery = new RedisCacheQuery<Role, Guid, RolePermissionsCacheItem>(
            repository.Object,
            redis.Connection.Object,
            CreateMapper(),
            Options.Create(new CacheOptions())
        );
        var invalidator = new RedisCacheInvalidator(redis.Connection.Object);

        var firstRole = await roleQuery.FindAsync(role.Id);
        var firstPermissions = await permissionsQuery.FindAsync(role.Id);
        role.Code = "updated";
        role.Permissions = [new Permission { PermissionCode = "menu.write" }];
        var cachedRole = await roleQuery.FindAsync(role.Id);
        var cachedPermissions = await permissionsQuery.FindAsync(role.Id);
        await invalidator.InvalidateEntityListCacheAsync(nameof(Role));
        var updatedRole = await roleQuery.FindAsync(role.Id);
        var updatedPermissions = await permissionsQuery.FindAsync(role.Id);

        Assert.Equal("reader", firstRole!.Code);
        Assert.Equal("menu.read", Assert.Single(firstPermissions!.PermissionCodes));
        Assert.Equal("reader", cachedRole!.Code);
        Assert.Equal("menu.read", Assert.Single(cachedPermissions!.PermissionCodes));
        Assert.Equal("updated", updatedRole!.Code);
        Assert.Equal("menu.write", Assert.Single(updatedPermissions!.PermissionCodes));
        Assert.Contains(
            redis.Values.Keys,
            key =>
                key.ToString().Contains(typeof(RoleCacheItem).FullName!, StringComparison.Ordinal)
        );
        Assert.Contains(
            redis.Values.Keys,
            key =>
                key.ToString()
                    .Contains(typeof(RolePermissionsCacheItem).FullName!, StringComparison.Ordinal)
        );
        repository.Verify(item => item.FindAsync(It.IsAny<object?[]>()), Times.Exactly(4));
    }

    [Fact]
    public async Task FindManyWithDetailAsync_QueriesOnlyMissingIdsAndOmitsUnknownIds()
    {
        var firstRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "first",
            Code = "first",
            Permissions = [new Permission { PermissionCode = "menu.read" }],
        };
        var secondRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "second",
            Code = "second",
            Permissions = [new Permission { PermissionCode = "menu.write" }],
        };
        var repository = new Mock<IRepository<Role>>();
        repository
            .Setup(item => item.Query(false))
            .Returns(() => new AsyncQuery<Role>([firstRole, secondRole]));
        var redis = new RedisStub();
        var query = new RedisCacheQuery<Role, Guid, RolePermissionsCacheItem>(
            repository.Object,
            redis.Connection.Object,
            CreateMapper(),
            Options.Create(new CacheOptions())
        );

        await query.FindManyWithDetailAsync<RolePermission, Permission>([firstRole.Id]);
        var results = await query.FindManyWithDetailAsync<RolePermission, Permission>([
            firstRole.Id,
            secondRole.Id,
            Guid.NewGuid(),
        ]);
        var cached = await query.FindManyWithDetailAsync<RolePermission, Permission>([
            firstRole.Id,
            secondRole.Id,
        ]);

        Assert.Equal(2, results.Count);
        Assert.Equal("menu.read", Assert.Single(results[firstRole.Id].PermissionCodes));
        Assert.Equal("menu.write", Assert.Single(results[secondRole.Id].PermissionCodes));
        Assert.Equal(2, cached.Count);
        repository.Verify(item => item.Query(false), Times.Exactly(2));
    }

    [Fact]
    public void RolePermissionsProjection_TranslatesToPostgresQuery()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=model_test;Username=unused")
            .Options;
        using var context = new ApiDbContext(options);

        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var sql = context
            .Roles.AsNoTracking()
            .Where(role => ids.Contains(role.Id))
            .ProjectTo<RolePermissionsCacheItem>(CreateMapper().ConfigurationProvider)
            .ToQueryString();

        Assert.Contains("PermissionCode", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FindWithDetailAsync_RelatedGenerationsInvalidateProjectedResult()
    {
        var roleId = Guid.NewGuid();
        var permission = new Permission { Id = Guid.NewGuid(), PermissionCode = "menu.read" };
        var role = new Role
        {
            Id = roleId,
            Name = "reader",
            Code = "reader",
            Permissions = [permission],
        };
        var repository = new Mock<IRepository<Role>>();
        repository.Setup(item => item.Query(false)).Returns(() => new AsyncQuery<Role>([role]));
        var redis = new RedisStub();
        var query = new RedisCacheQuery<Role, Guid, RolePermissionsCacheItem>(
            repository.Object,
            redis.Connection.Object,
            CreateMapper(),
            Options.Create(new CacheOptions())
        );
        var invalidator = new RedisCacheInvalidator(redis.Connection.Object);

        var first = await query.FindWithDetailAsync<RolePermission, Permission>(roleId);
        var cached = await query.FindWithDetailAsync<RolePermission, Permission>(roleId);
        permission.PermissionCode = "menu.write";
        await invalidator.InvalidateEntityListCacheAsync(nameof(RolePermission));
        var afterRelationChange = await query.FindWithDetailAsync<RolePermission, Permission>(
            roleId
        );
        permission.PermissionCode = "menu.delete";
        await invalidator.InvalidateEntityListCacheAsync(nameof(Permission));
        var afterPermissionChange = await query.FindWithDetailAsync<RolePermission, Permission>(
            roleId
        );
        role.Permissions = [new Permission { PermissionCode = "menu.admin" }];
        await invalidator.InvalidateEntityListCacheAsync(nameof(Role));
        var afterRoleChange = await query.FindWithDetailAsync<RolePermission, Permission>(roleId);

        Assert.Equal("menu.read", Assert.Single(first!.PermissionCodes));
        Assert.Equal("menu.read", Assert.Single(cached!.PermissionCodes));
        Assert.Equal("menu.write", Assert.Single(afterRelationChange!.PermissionCodes));
        Assert.Equal("menu.delete", Assert.Single(afterPermissionChange!.PermissionCodes));
        Assert.Equal("menu.admin", Assert.Single(afterRoleChange!.PermissionCodes));
        Assert.Contains(
            redis.Values.Keys,
            key =>
                key.ToString()
                    .Contains(
                        $":projected-entity:Role:{roleId}:{typeof(RolePermissionsCacheItem).FullName}:Role:0|RolePermission:0|Permission:0",
                        StringComparison.Ordinal
                    )
        );
        repository.Verify(item => item.Query(false), Times.Exactly(4));
    }

    [Fact]
    public async Task GetFilteredListAsync_DifferentProjectionTypesUseSeparateKeys()
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "reader",
            Code = "reader",
            IsEnabled = true,
            Permissions = [new Permission { PermissionCode = "menu.read" }],
        };
        var repository = new Mock<IRepository<Role>>();
        repository.Setup(item => item.Query(false)).Returns(new AsyncQuery<Role>([role]));
        var redis = new RedisStub();
        var roleQuery = new RedisCacheQuery<Role, Guid, RoleCacheItem>(
            repository.Object,
            redis.Connection.Object,
            CreateMapper(),
            Options.Create(new CacheOptions())
        );
        var permissionsQuery = new RedisCacheQuery<Role, Guid, RolePermissionsCacheItem>(
            repository.Object,
            redis.Connection.Object,
            CreateMapper(),
            Options.Create(new CacheOptions())
        );

        var roleResult = await roleQuery.GetFilteredListAsync<Permission>(item => item.IsEnabled);
        var permissionsResult = await permissionsQuery.GetFilteredListAsync<Permission>(item =>
            item.IsEnabled
        );

        Assert.Equal("reader", Assert.Single(roleResult).Code);
        Assert.Equal("menu.read", Assert.Single(Assert.Single(permissionsResult).PermissionCodes));
        Assert.Equal(
            2,
            redis.Values.Keys.Count(key =>
                key.ToString().Contains(":entities:v1:", StringComparison.Ordinal)
            )
        );
        repository.Verify(item => item.Query(false), Times.Exactly(2));
    }

    [Fact]
    public async Task WhereAsync_CapturedValueDiffers_UsesDifferentCacheEntries()
    {
        var users = new[]
        {
            new User
            {
                Id = Guid.NewGuid(),
                Username = "operator",
                IsEnabled = true,
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "administrator",
                IsEnabled = true,
            },
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
        repository
            .Setup(item => item.Query(false))
            .Returns(
                new AsyncQuery<User>(
                    new[]
                    {
                        new User
                        {
                            Id = Guid.NewGuid(),
                            Username = "operator",
                            IsEnabled = true,
                        },
                    }
                )
            );
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis);

        var first = await query.GetFilteredListAsync(user => user.Username == "operator");
        var second = await query.GetFilteredListAsync(account => account.Username == "operator");

        Assert.Equal(Assert.Single(first).Id, Assert.Single(second).Id);
        Assert.Contains(
            redis.Values.Keys,
            key => key.ToString().Contains(":entities:v1:", StringComparison.Ordinal)
        );
        repository.Verify(item => item.Query(false), Times.Once);
    }

    [Fact]
    public async Task WhereAsync_GenerationChanges_QueriesAgainWithNewListKey()
    {
        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Username = "operator",
                IsEnabled = true,
            },
        };
        var repository = new Mock<IRepository<User>>();
        repository.Setup(item => item.Query(false)).Returns(() => new AsyncQuery<User>(users));
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis);
        var invalidator = new RedisCacheInvalidator(redis.Connection.Object);

        var first = await query.GetFilteredListAsync(user => user.IsEnabled);
        users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                Username = "administrator",
                IsEnabled = true,
            }
        );
        await invalidator.InvalidateEntityListCacheAsync(nameof(User));
        var second = await query.GetFilteredListAsync(user => user.IsEnabled);

        Assert.Single(first);
        Assert.Equal(2, second.Count);
        Assert.Equal(
            2,
            redis.Values.Keys.Count(key =>
                key.ToString().Contains(":entities:v1:", StringComparison.Ordinal)
            )
        );
        Assert.Contains(
            redis.Values.Keys,
            key => key.ToString().Contains(":User:0:", StringComparison.Ordinal)
        );
        Assert.Contains(
            redis.Values.Keys,
            key => key.ToString().Contains(":User:1:", StringComparison.Ordinal)
        );
        Assert.Equal("1", redis.Values[CacheKeys.EntitiesGeneration<User>()].ToString());
        repository.Verify(item => item.Query(false), Times.Exactly(2));
    }

    [Fact]
    public async Task GetFilteredListAsync_RelatedEntityChanges_UseNewCacheKeys()
    {
        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Username = "operator",
                IsEnabled = true,
            },
        };
        var repository = new Mock<IRepository<User>>();
        repository.Setup(item => item.Query(false)).Returns(() => new AsyncQuery<User>(users));
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis);
        var invalidator = new RedisCacheInvalidator(redis.Connection.Object);
        var first = await query.GetFilteredListAsync<Role, Permission>(user => user.IsEnabled);
        var cached = await query.GetFilteredListAsync<Role, Permission>(user => user.IsEnabled);
        users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                Username = "administrator",
                IsEnabled = true,
            }
        );
        await invalidator.InvalidateEntityListCacheAsync(nameof(Role));
        var afterRoleChange = await query.GetFilteredListAsync<Role, Permission>(user =>
            user.IsEnabled
        );
        users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                Username = "auditor",
                IsEnabled = true,
            }
        );
        await invalidator.InvalidateEntityListCacheAsync(nameof(Permission));
        var afterPermissionChange = await query.GetFilteredListAsync<Role, Permission>(user =>
            user.IsEnabled
        );
        users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                Username = "reader",
                IsEnabled = true,
            }
        );
        await invalidator.InvalidateEntityListCacheAsync(nameof(User));
        var afterUserChange = await query.GetFilteredListAsync<Role, Permission>(user =>
            user.IsEnabled
        );

        Assert.Single(first);
        Assert.Single(cached);
        Assert.Equal(2, afterRoleChange.Count);
        Assert.Equal(3, afterPermissionChange.Count);
        Assert.Equal(4, afterUserChange.Count);
        Assert.Contains(
            redis.Values.Keys,
            key => key.ToString().Contains(":User:0|Role:0|Permission:0:", StringComparison.Ordinal)
        );
        Assert.Contains(
            redis.Values.Keys,
            key => key.ToString().Contains(":User:0|Role:1|Permission:0:", StringComparison.Ordinal)
        );
        Assert.Contains(
            redis.Values.Keys,
            key => key.ToString().Contains(":User:0|Role:1|Permission:1:", StringComparison.Ordinal)
        );
        Assert.Contains(
            redis.Values.Keys,
            key => key.ToString().Contains(":User:1|Role:1|Permission:1:", StringComparison.Ordinal)
        );
        repository.Verify(item => item.Query(false), Times.Exactly(4));
    }

    [Fact]
    public async Task GetFilteredListAsync_OneRelatedEntityChange_UsesNewCacheKey()
    {
        var repository = new Mock<IRepository<User>>();
        repository
            .Setup(item => item.Query(false))
            .Returns(
                new AsyncQuery<User>(
                    new[]
                    {
                        new User
                        {
                            Id = Guid.NewGuid(),
                            Username = "operator",
                            IsEnabled = true,
                        },
                    }
                )
            );
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis);
        var invalidator = new RedisCacheInvalidator(redis.Connection.Object);

        await query.GetFilteredListAsync<Role>(user => user.IsEnabled);
        await invalidator.InvalidateEntityListCacheAsync(nameof(Role));
        await query.GetFilteredListAsync<Role>(user => user.IsEnabled);

        Assert.Contains(
            redis.Values.Keys,
            key => key.ToString().Contains(":User:0|Role:0:", StringComparison.Ordinal)
        );
        Assert.Contains(
            redis.Values.Keys,
            key => key.ToString().Contains(":User:0|Role:1:", StringComparison.Ordinal)
        );
        repository.Verify(item => item.Query(false), Times.Exactly(2));
    }

    [Fact]
    public async Task WhereAsync_PermissionCacheItem_ProjectsFromPermission()
    {
        var permissionId = Guid.NewGuid();
        var repository = new Mock<IRepository<Permission>>();
        repository
            .Setup(item => item.Query(false))
            .Returns(
                new AsyncQuery<Permission>(
                    new[]
                    {
                        new Permission
                        {
                            Id = permissionId,
                            Name = "View users",
                            Code = "users.view",
                            IsEnabled = true,
                        },
                    }
                )
            );
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
            new User
            {
                Id = Guid.NewGuid(),
                Username = "operator",
                IsEnabled = true,
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "administrator",
                IsEnabled = true,
            },
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
        repository
            .Setup(item => item.Query(false))
            .Returns(
                new AsyncQuery<User>(
                    new[]
                    {
                        new User
                        {
                            Id = Guid.NewGuid(),
                            Username = "operator",
                            IsEnabled = true,
                        },
                    }
                )
            );
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
        repository
            .Setup(item => item.Query(false))
            .Returns(
                new AsyncQuery<User>(
                    new[]
                    {
                        new User
                        {
                            Id = Guid.NewGuid(),
                            Username = "operator",
                            IsEnabled = true,
                        },
                        new User
                        {
                            Id = Guid.NewGuid(),
                            Username = "administrator",
                            IsEnabled = true,
                        },
                    }
                )
            );
        var redis = new RedisStub();
        var query = CreateQuery(repository.Object, redis, maxCollectionItems: 1);

        var exception = await Assert.ThrowsAsync<CacheQueryResultTooLargeException>(() =>
            query.GetFilteredListAsync(user => user.IsEnabled)
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
        repository
            .Setup(item => item.Query(false))
            .Returns(
                new AsyncQuery<User>(
                    new[]
                    {
                        new User
                        {
                            Id = Guid.NewGuid(),
                            Username = "operator",
                            IsEnabled = true,
                        },
                        new User
                        {
                            Id = Guid.NewGuid(),
                            Username = "administrator",
                            IsEnabled = true,
                        },
                    }
                )
            );
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
    ) =>
        new(
            repository,
            redis.Connection.Object,
            CreateMapper(),
            Options.Create(new CacheOptions { MaxCollectionItems = maxCollectionItems })
        );

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            config =>
            {
                config.AddProfile<DtoConventionProfile>();
                config.AddProfile<PermissionCacheProfile>();
                config.AddProfile<RolePermissionsCacheProfile>();
            },
            NullLoggerFactory.Instance
        ).CreateMapper();

    private sealed class UserFilter
    {
        public string Username { get; set; } = null!;
    }
}
