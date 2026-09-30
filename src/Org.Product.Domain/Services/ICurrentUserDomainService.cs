using Org.Product.Domain.Services.Base;

namespace Org.Product.Domain.Services;

public interface ICurrentUserDomainService : IDomainService
{
    Guid? UserId { get; }

    IReadOnlyList<Guid> RoleIds { get; }
}
