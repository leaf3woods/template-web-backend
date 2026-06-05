using Microsoft.AspNetCore.Authorization;
using Template.Web.Application.Auth.Requirements;
using Template.Web.Domain.ValueObjects;

namespace Template.Web.Application.Auth
{
    public static class AuthPolicyExtension
    {
        public static void AddAllPolicies(
            this AuthorizationOptions options,
            IEnumerable<PermissionDefinition> permissions
        )
        {
            foreach (var permission in permissions)
            {
                options.AddPolicy(
                    permission.Name,
                    policy =>
                        policy.AddRequirements(
                            new PermissionAuthorizationRequirement(permission.Name)
                        )
                );
            }
        }

        public static void AddAllPolicies(
            this AuthorizationOptions options,
            IEnumerable<string> permissions
        )
        {
            foreach (var permission in permissions)
            {
                options.AddPolicy(
                    permission,
                    policy =>
                        policy.AddRequirements(new PermissionAuthorizationRequirement(permission))
                );
            }
        }
    }
}
