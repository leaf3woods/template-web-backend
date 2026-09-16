namespace Org.Product.Application.Abstractions.Security;

public interface IUserSessionStore
{
    Task SaveAsync(Guid userId, string token, TimeSpan? expiration = null);
    Task<bool> IsValidAsync(Guid userId, string token);
    Task<bool> RevokeAsync(Guid userId, string? expectedToken = null);
}
