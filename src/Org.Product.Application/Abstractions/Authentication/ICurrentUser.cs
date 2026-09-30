namespace Org.Product.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    Guid? UserId { get; }

    IReadOnlyList<Guid> RoleIds { get; }
}
