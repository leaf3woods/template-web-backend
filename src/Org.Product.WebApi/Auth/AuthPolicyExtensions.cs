using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Org.Product.WebApi.Auth.Requirements;

namespace Org.Product.WebApi.Auth;

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
            .Where(type => type.Namespace == "Org.Product.WebApi.Controllers")
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
