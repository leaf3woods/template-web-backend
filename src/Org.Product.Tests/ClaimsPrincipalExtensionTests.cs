using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Org.Product.Domain.Shared;
using Org.Product.Infrastructure.DomainServices;
using Org.Product.Infrastructure.Utilities;

namespace Org.Product.Tests;

public sealed class ClaimsPrincipalExtensionTests
{
    [Fact]
    public void ValidClaims_AreSharedByCurrentUser()
    {
        var userId = Guid.NewGuid();
        var firstRoleId = Guid.NewGuid();
        var secondRoleId = Guid.NewGuid();
        var principal = CreatePrincipal(userId.ToString(), $" {firstRoleId}, {secondRoleId} ");
        var currentUser = CreateCurrentUser(principal);

        Assert.Equal(userId, principal.GetUserId());
        Assert.Equal(new[] { firstRoleId, secondRoleId }, principal.GetRoleIds());
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal(new[] { firstRoleId, secondRoleId }, currentUser.RoleIds);
    }

    [Fact]
    public void MalformedRoleList_IsRejectedAsAWhole()
    {
        var validRoleId = Guid.NewGuid();
        foreach (var roleIds in new[] { $"{validRoleId},invalid", $"{validRoleId}," })
        {
            var principal = CreatePrincipal(Guid.NewGuid().ToString(), roleIds);
            var currentUser = CreateCurrentUser(principal);

            Assert.Empty(principal.GetRoleIds());
            Assert.Empty(currentUser.RoleIds);
        }
    }

    [Fact]
    public void EmptyIdentifiers_AreRejected()
    {
        var principal = CreatePrincipal(Guid.Empty.ToString(), Guid.Empty.ToString());
        var currentUser = CreateCurrentUser(principal);

        Assert.Null(principal.GetUserId());
        Assert.Empty(principal.GetRoleIds());
        Assert.Null(currentUser.UserId);
        Assert.Empty(currentUser.RoleIds);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, string roleIds) =>
        new(
            new ClaimsIdentity([
                new Claim(CustomClaimsType.UserId, userId),
                new Claim(CustomClaimsType.RoleId, roleIds),
            ])
        );

    private static CurrentUserDomainService CreateCurrentUser(ClaimsPrincipal principal) =>
        new(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });
}
