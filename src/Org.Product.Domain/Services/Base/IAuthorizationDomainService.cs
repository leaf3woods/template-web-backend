namespace Org.Product.Domain.Services.Base;

public interface IAuthorizationDomainService : IDomainService
{
    bool HasGlobalAccess(IEnumerable<Guid> roleIds);

    bool HasPermission(IEnumerable<string> grantedPermissions, string requiredPermission);
}
