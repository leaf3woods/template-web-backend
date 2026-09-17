using System.Security.Cryptography;
using Org.Product.Application.Abstractions.Authentication;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Infrastructure.Adapters.Authentication;

public sealed class PasswordCredentialService : IPasswordCredentialService
{
    public void SetPassword(User user, string encodedPassword)
    {
        var secret = Convert.FromBase64String(encodedPassword);
        var guid = Guid.NewGuid().ToByteArray();
        var length = Random.Shared.Next(guid.Length / 4, guid.Length / 2);
        var start = Random.Shared.Next(0, guid.Length / 2 - 1);
        var salt = SHA256.HashData(guid[start..(length + start)]);
        user.Passphrase = Convert.ToBase64String(SHA256.HashData(secret.Concat(salt).ToArray()));
        user.Salt = Convert.ToBase64String(salt);
    }

    public bool Verify(User user, string encodedPassword)
    {
        var secret = Convert.FromBase64String(encodedPassword);
        var salt = Convert.FromBase64String(user.Salt);
        var salted = SHA256.HashData(secret.Concat(salt).ToArray());
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(user.Passphrase), salted);
    }
}
