using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Org.Product.Infrastructure.Adapters.Security;
using Org.Product.Infrastructure.Adapters.Security.Options;

namespace Org.Product.Tests;

public sealed class SigningKeyTests
{
    [Fact]
    public void GeneratedToken_ValidatesOnFirstStartAndAfterKeysReload()
    {
        var folder = Path.Combine(Path.GetTempPath(), "org-product-keys-" + Guid.NewGuid().ToString("N"));
        try
        {
            var userId = Guid.NewGuid();
            string token;
            using (var keys = new FileSigningKeyProvider(folder))
            {
                var issuer = new JwtAccessTokenIssuer(keys, Options.Create(new AccessTokenOptions
                {
                    Issuer = "tests",
                    Audience = "tests",
                    ExpireMin = 5,
                }));
                var issued = issuer.Issue(userId, [Guid.NewGuid()]);
                Assert.Equal(TimeSpan.FromMinutes(5), issued.Lifetime);
                Validate(issued.Value, keys);
                token = issued.Value;
            }

            using var reloaded = new FileSigningKeyProvider(folder);
            Validate(token, reloaded);
        }
        finally
        {
            File.Delete(Path.Combine(folder, "private-key.pem"));
            File.Delete(Path.Combine(folder, "public-key.pem"));
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder);
            }
        }
    }

    private static void Validate(string token, FileSigningKeyProvider keys)
    {
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            IssuerSigningKey = keys.PublicKey,
            ValidIssuer = "tests",
            ValidAudience = "tests",
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
        }, out _);
        Assert.True(principal.Identity!.IsAuthenticated);
    }
}
