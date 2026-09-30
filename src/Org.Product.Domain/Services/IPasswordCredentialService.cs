using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Services.Base;

namespace Org.Product.Domain.Services;

public interface IPasswordCredentialService : IDomainService
{
    void SetPassword(User user, string encodedPassword);
    bool Verify(User user, string encodedPassword);
}
