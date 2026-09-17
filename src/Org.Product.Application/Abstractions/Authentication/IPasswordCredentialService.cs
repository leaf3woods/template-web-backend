using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Abstractions.Authentication;

public interface IPasswordCredentialService : ISecurityService
{
    void SetPassword(User user, string encodedPassword);
    bool Verify(User user, string encodedPassword);
}
