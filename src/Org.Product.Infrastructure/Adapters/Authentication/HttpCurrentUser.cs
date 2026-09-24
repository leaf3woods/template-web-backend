using Microsoft.AspNetCore.Http;
using Org.Product.Application.Abstractions.Authentication;
using Org.Product.Domain.Shared;

namespace Org.Product.Infrastructure.Adapters.Authentication;

public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId
    {
        get
        {
            var uid = _accessor.HttpContext?.User.FindFirst(c => c.Type == CustomClaimsType.UserId)?.Value;
            if (Guid.TryParse(uid, out var result))
            {
                return result;
            }
            return null;
        }
    }

    public Guid? RoleId
    {
        get
        {
            var rid = _accessor.HttpContext?.User.FindFirst(c => c.Type == CustomClaimsType.RoleId)?.Value;
            if (Guid.TryParse(rid, out var result))
            {
                return result;
            }
            return null;
        }
    }

}
