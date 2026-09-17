namespace Org.Product.Application.Abstractions.Security;

public interface IRolePermissionStore : ISecurityStore
{
    Task<IEnumerable<string>> GetPermissionsAsync(params IEnumerable<Guid> roleIds);
    // Tests the stored permission snapshots, not the existence of database roles.
    Task<bool> ContainsAsync(params IEnumerable<Guid> roleIds);
    Task SaveAsync(Guid roleId, IEnumerable<string> permissions, TimeSpan? expiration = null);
}
