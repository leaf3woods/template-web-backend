using Microsoft.AspNetCore.Authorization;
using Org.Product.Application.Abstractions.Security;
using Org.Product.Domain.Services.Base;
using Org.Product.Domain.Shared;
using Org.Product.WebApi.Auth.Requirements;

namespace Org.Product.WebApi.Auth.AuthHandlers;

public sealed class CustomRequireHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly IRolePermissionStore _permissions;
    private readonly IAuthorizationDomainService _authorization;
    private readonly ILogger<CustomRequireHandler> _logger;

    public CustomRequireHandler(
        IRolePermissionStore permissions,
        IAuthorizationDomainService authorization,
        ILogger<CustomRequireHandler> logger
    )
    {
        _permissions = permissions;
        _authorization = authorization;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement
    )
    {
        var userId = context.User.FindFirst(CustomClaimsType.UserId)?.Value;
        var roleIdsValue = context.User.FindFirst(CustomClaimsType.RoleId)?.Value;
        if (
            context.User.Identity?.IsAuthenticated != true
            || !Guid.TryParse(userId, out var uid)
            || uid == Guid.Empty
            || string.IsNullOrWhiteSpace(roleIdsValue)
        )
        {
            _logger.LogWarning("invalid token claims");
            context.Fail(new AuthorizationFailureReason(this, "invalid claims"));
            return;
        }

        if (!TryParseRoleIds(roleIdsValue, out var roleIds))
        {
            _logger.LogWarning("invalid role claims");
            context.Fail(new AuthorizationFailureReason(this, "invalid role claims"));
            return;
        }

        if (!await _permissions.ContainsAsync(roleIds))
        {
            context.Fail(new AuthorizationFailureReason(this, "role permissions not found"));
            return;
        }

        if (_authorization.HasGlobalAccess(roleIds))
        {
            context.Succeed(requirement);
            return;
        }

        var permissions = await _permissions.GetPermissionsAsync(roleIds);
        if (_authorization.HasPermission(permissions, requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this, "no authorization"));
    }

    private static bool TryParseRoleIds(string value, out Guid[] roleIds)
    {
        roleIds = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(roleId =>
                Guid.TryParse(roleId, out var parsedRoleId) ? parsedRoleId : Guid.Empty
            )
            .ToArray();

        return roleIds.Length > 0 && roleIds.All(roleId => roleId != Guid.Empty);
    }
}
