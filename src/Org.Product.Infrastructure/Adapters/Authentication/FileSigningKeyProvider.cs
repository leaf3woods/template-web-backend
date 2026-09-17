using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Org.Product.Infrastructure.Adapters.Authentication;

public sealed class FileSigningKeyProvider : IDisposable
{
    private readonly ECDsa _privateKey = ECDsa.Create();
    private readonly ECDsa _publicKey = ECDsa.Create();

    public FileSigningKeyProvider(string keyFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyFolder);
        try
        {
            var privatePath = Path.Combine(keyFolder, "private-key.pem");
            var publicPath = Path.Combine(keyFolder, "public-key.pem");
            if (!File.Exists(privatePath) || !File.Exists(publicPath))
            {
                Directory.CreateDirectory(keyFolder);
                using var generated = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                File.WriteAllText(privatePath, generated.ExportECPrivateKeyPem());
                File.WriteAllText(publicPath, generated.ExportSubjectPublicKeyInfoPem());
            }

            _privateKey.ImportFromPem(File.ReadAllText(privatePath));
            _publicKey.ImportFromPem(File.ReadAllText(publicPath));
            // A reloaded key must not reuse cached providers holding a disposed ECDsa.
            var cryptoProvider = new CryptoProviderFactory();
            PrivateKey = new ECDsaSecurityKey(_privateKey) { CryptoProviderFactory = cryptoProvider };
            PublicKey = new ECDsaSecurityKey(_publicKey) { CryptoProviderFactory = cryptoProvider };
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public SecurityKey PrivateKey { get; }
    public SecurityKey PublicKey { get; }

    public void Dispose()
    {
        _privateKey.Dispose();
        _publicKey.Dispose();
    }
}
