using Microsoft.AspNetCore.Authorization;
using Template.Web.WebApi.Auth.Requirements;

namespace Template.Web.WebApi.Auth;

public static class AuthPolicyExtensions
{
    public static void AddAllPolicies(
        this AuthorizationOptions options,
        IEnumerable<string> permissions
    )
    {
        foreach (var permission in permissions)
        {
            options.AddPolicy(
                permission,
                policy => policy.AddRequirements(new PermissionAuthorizationRequirement(permission))
            );
        }
    }
}
