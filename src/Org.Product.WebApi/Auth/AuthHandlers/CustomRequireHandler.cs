using Microsoft.AspNetCore.Authorization;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Services;
using Org.Product.Domain.Shared;
using Org.Product.WebApi.Auth.Requirements;

namespace Org.Product.WebApi.Auth.AuthHandlers;

public sealed class CustomRequireHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly IRoleDomainService _roleDomainService;
    private readonly ILogger<CustomRequireHandler> _logger;

    public CustomRequireHandler(
        IRoleDomainService roleDomainService,
        ILogger<CustomRequireHandler> logger
    )
    {
        _roleDomainService = roleDomainService;
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

        if (!await _roleDomainService.ExistsInCacheAsync(roleIds))
        {
            context.Fail(new AuthorizationFailureReason(this, "role permissions not found"));
            return;
        }

        if (roleIds.Contains(Role.SuperRole.Id))
        {
            context.Succeed(requirement);
            return;
        }

        var permissions = await _roleDomainService.GetPermissionsAsync(roleIds);
        var hasPermission = permissions.Any(permission =>
            !string.IsNullOrWhiteSpace(permission)
            && (
                string.Equals(requirement.Permission, permission, StringComparison.Ordinal)
                || requirement.Permission.StartsWith(permission + ".", StringComparison.Ordinal)
            )
        );
        if (hasPermission)
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
