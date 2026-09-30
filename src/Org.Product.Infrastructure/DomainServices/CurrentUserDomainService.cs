using Microsoft.AspNetCore.Http;
using Org.Product.Domain.Services;
using Org.Product.Infrastructure.Utilities;

namespace Org.Product.Infrastructure.DomainServices;

public class CurrentUserDomainService : ICurrentUserDomainService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserDomainService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId => _accessor.HttpContext?.User.GetUserId();

    public IReadOnlyList<Guid> RoleIds => _accessor.HttpContext?.User.GetRoleIds() ?? [];
}
