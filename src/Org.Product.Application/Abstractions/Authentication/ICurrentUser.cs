namespace Org.Product.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    Guid? UserId { get; }

    Guid? RoleId { get; }
}
