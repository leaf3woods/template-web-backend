using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Services.Base;

namespace Org.Product.Domain.Services;

public class AuthorizationDomainService : IAuthorizationDomainService
{
    public bool HasGlobalAccess(IEnumerable<Guid> roleIds)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        return roleIds.Contains(Role.SuperRole.Id);
    }

    public bool HasPermission(IEnumerable<string> grantedPermissions, string requiredPermission)
    {
        ArgumentNullException.ThrowIfNull(grantedPermissions);

        if (string.IsNullOrWhiteSpace(requiredPermission))
        {
            return false;
        }

        return grantedPermissions.Any(grantedPermission =>
            !string.IsNullOrWhiteSpace(grantedPermission)
            && (
                string.Equals(requiredPermission, grantedPermission, StringComparison.Ordinal)
                || requiredPermission.StartsWith(grantedPermission + ".", StringComparison.Ordinal)
            )
        );
    }
}
