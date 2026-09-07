using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.Product.WebApi.Auth.Requirements;

namespace Org.Product.WebApi.Auth;

public static class AuthPolicyExtensions
{
    public static void AddAllPolicies(
        this AuthorizationOptions options,
        IEnumerable<string>? permissions = null
    )
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        permissions ??= Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type =>
                type.GetMethods()
                    .SelectMany(m => m.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
                    .Concat(type.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
            )
            .Where(t => !string.IsNullOrWhiteSpace(t.Policy))
            .Select(t => t.Policy!)
            .ToArray();
        foreach (var permission in permissions.Distinct(StringComparer.Ordinal))
        {
            options.AddPolicy(
                permission,
                policy => policy.RequireAuthenticatedUser()
                    .AddRequirements(new PermissionAuthorizationRequirement(permission))
            );
        }
    }
}
