using Microsoft.AspNetCore.Authorization;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Application.Dtos.Cache;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Entities.Authority;
using Org.Product.Domain.Services;
using Org.Product.Infrastructure.Utilities;
using Org.Product.WebApi.Auth.Requirements;

namespace Org.Product.WebApi.Auth.AuthHandlers;

public sealed class CustomRequireHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly ICacheQuery<Role, Guid, RolePermissionsCacheItem> _rolesQuery;
    private readonly IAuthorizationDomainService _authorization;
    private readonly ILogger<CustomRequireHandler> _logger;

    public CustomRequireHandler(
        ICacheQuery<Role, Guid, RolePermissionsCacheItem> rolesQuery,
        IAuthorizationDomainService authorization,
        ILogger<CustomRequireHandler> logger
    )
    {
        _rolesQuery = rolesQuery;
        _authorization = authorization;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement
    )
    {
        var userId = context.User.GetUserId();
        if (context.User.Identity?.IsAuthenticated != true || userId is null)
        {
            _logger.LogWarning("invalid token claims");
            context.Fail(new AuthorizationFailureReason(this, "invalid claims"));
            return;
        }

        var roleIds = context.User.GetRoleIds().ToArray();

        if (roleIds.Length == 0)
        {
            _logger.LogWarning("invalid role claims");
            context.Fail(new AuthorizationFailureReason(this, "invalid role claims"));
            return;
        }

        var roles = await _rolesQuery.FindManyWithDetailAsync<RolePermission, Permission>(roleIds);
        if (roleIds.Any(roleId => !roles.ContainsKey(roleId)))
        {
            context.Fail(new AuthorizationFailureReason(this, "role permissions not found"));
            return;
        }

        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles.Values)
        {
            permissions.UnionWith(role.PermissionCodes);
        }

        if (_authorization.HasGlobalAccess(roleIds))
        {
            context.Succeed(requirement);
            return;
        }

        if (_authorization.HasPermission(permissions, requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this, "no authorization"));
    }
}
