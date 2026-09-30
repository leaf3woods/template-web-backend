using Microsoft.AspNetCore.Http;
using Org.Product.Application.Abstractions.Authentication;
using Org.Product.Domain.Shared.Exceptions;
using Org.Product.Infrastructure.Utilities;

namespace Org.Product.Infrastructure.Adapters.Authentication;

public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId => _accessor.HttpContext?.User.GetUserId();

    public IReadOnlyList<Guid> RoleIds => _accessor.HttpContext?.User.GetRoleIds() ?? [];
}
