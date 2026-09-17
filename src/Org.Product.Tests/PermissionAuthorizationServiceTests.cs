using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Services;

namespace Org.Product.Tests;

public sealed class PermissionAuthorizationServiceTests
{
    private readonly AuthorizationDomainService _service = new();

    [Fact]
    public void HasGlobalAccess_SuperRoleIsAssigned_ReturnsTrue()
    {
        Assert.True(_service.HasGlobalAccess([Role.MemberRole.Id, Role.SuperRole.Id]));
    }

    [Theory]
    [InlineData("menu.delete.Id", true)]
    [InlineData("menu.delete", true)]
    [InlineData("menu", true)]
    [InlineData("menu.del", false)]
    [InlineData("menu.delete.Id.extra", false)]
    [InlineData("Menu.delete.Id", false)]
    [InlineData("", false)]
    public void HasPermission_GrantedPermissionAndRequestedPermission_ReturnsExpected(
        string grantedPermission,
        bool expected
    )
    {
        Assert.Equal(expected, _service.HasPermission([grantedPermission], "menu.delete.Id"));
    }
}
