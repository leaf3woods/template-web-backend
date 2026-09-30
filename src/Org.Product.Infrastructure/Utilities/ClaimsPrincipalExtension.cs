using System.Security.Claims;
using Org.Product.Domain.Shared;

namespace Org.Product.Infrastructure.Utilities;

public static class ClaimsPrincipalExtension
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var uid = principal.FindFirst(c => c.Type == CustomClaimsType.UserId)?.Value;
        if (Guid.TryParse(uid, out var result) && result != Guid.Empty)
        {
            return result;
        }
        return null;
    }

    public static IReadOnlyList<Guid> GetRoleIds(this ClaimsPrincipal principal)
    {
        var roleIdsValue = principal.FindFirst(c => c.Type == CustomClaimsType.RoleId)?.Value;
        if (string.IsNullOrWhiteSpace(roleIdsValue))
        {
            return [];
        }
        var roleIds = new HashSet<Guid>();
        foreach (var roleIdStr in roleIdsValue.Split(','))
        {
            if (Guid.TryParse(roleIdStr, out var roleId) && roleId != Guid.Empty)
            {
                roleIds.Add(roleId);
            }
            else
            {
                return [];
            }
        }
        return [.. roleIds];
    }
}
