using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Org.Product.Application.Dtos.Cache;
using Org.Product.Application.Utilities.MapperProfiles;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Tests;

public sealed class CacheProjectionTests
{
    [Fact]
    public void UserProjection_MapsQueryableFieldsWithoutCredentialProperties()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Username = "cache-user",
            Name = "Cache User",
            Passphrase = "credential-hash",
            Salt = "credential-salt",
            SortOrder = 3,
            IsEnabled = true,
        };

        var item = CreateMapper().Map<UserCacheItem>(user);

        Assert.Equal(id, item.Id);
        Assert.Equal(user.Username, item.Username);
        Assert.Equal(user.Name, item.Name);
        Assert.Equal(user.SortOrder, item.SortOrder);
        Assert.Equal(user.IsEnabled, item.IsEnabled);
        Assert.Null(typeof(UserCacheItem).GetProperty(nameof(User.Passphrase)));
        Assert.Null(typeof(UserCacheItem).GetProperty(nameof(User.Salt)));
        Assert.Null(typeof(UserCacheItem).GetProperty(nameof(User.Roles)));
    }

    [Fact]
    public void RoleProjection_MapsLookupFields()
    {
        var id = Guid.NewGuid();
        var role = new Role
        {
            Id = id,
            Name = "operator",
            Code = "operator",
            Description = "Operator role",
            SortOrder = 5,
            IsEnabled = true,
        };

        var item = CreateMapper().Map<RoleCacheItem>(role);

        Assert.Equal(id, item.Id);
        Assert.Equal(role.Name, item.Name);
        Assert.Equal(role.Code, item.Code);
        Assert.Equal(role.SortOrder, item.SortOrder);
        Assert.Equal(role.IsEnabled, item.IsEnabled);
    }

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            config => config.AddProfile<DtoConventionProfile>(),
            NullLoggerFactory.Instance
        ).CreateMapper();
}
