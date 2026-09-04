using Microsoft.AspNetCore.Authorization;

namespace Org.Product.WebApi.Auth.Requirements;

public sealed class PermissionAuthorizationRequirement : IAuthorizationRequirement
{
    public PermissionAuthorizationRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
