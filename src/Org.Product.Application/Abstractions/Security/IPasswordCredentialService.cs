using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Abstractions.Security;

public interface IPasswordCredentialService
{
    void SetPassword(User user, string encodedPassword);
    bool Verify(User user, string encodedPassword);
}
