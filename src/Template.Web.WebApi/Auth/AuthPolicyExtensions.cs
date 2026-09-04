using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Template.Web.WebApi.Auth.Requirements;

namespace Template.Web.WebApi.Auth;

public static class AuthPolicyExtensions
{
    public static void AddAllPolicies(
        this AuthorizationOptions options,
        IEnumerable<string>? permissions = null
    )
    {
        permissions = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.Namespace == "Template.Web.WebApi.Controllers")
            .SelectMany(type =>
                type.GetMethods()
                    .Select(m => m.GetCustomAttribute<AuthorizeAttribute>())
                    .Append(type.GetCustomAttribute<AuthorizeAttribute>())
            )
            .Where(t => t?.Policy is not null)
            .Select(t => t!.Policy!)
            .ToArray();
        foreach (var permission in permissions)
        {
            options.AddPolicy(
                permission,
                policy => policy.AddRequirements(new PermissionAuthorizationRequirement(permission))
            );
        }
    }
}
