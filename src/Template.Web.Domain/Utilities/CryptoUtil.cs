using System.Security.Cryptography;
using System.Text;
using Template.Web.Domain.Entities.Account;

namespace Template.Web.Domain.Utilities
{
    public static class CryptoUtil
    {
        public static ECDsa PublicECDsa { get; private set; } = ECDsa.Create();
        public static ECDsa PrivateECDsa { get; private set; } = ECDsa.Create();

        public static void Initialize(string keyFolder)
        {
#if TEST
            var password = "dev@233";
            var secret = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var base64 = Convert.ToBase64String(secret);
            //------ to backend -------//
            var pass = Salt(secret, out var salt);
            var store = Convert.ToBase64String(pass);
            var salt64 = Convert.ToBase64String(salt);
            var isPass = User.DevUser.Verify(secret);
#endif

            var privateKeyPath = Path.Combine(keyFolder, "private-key.pem");
            var publicKeyPath = Path.Combine(keyFolder, "public-key.pem");

            if (!File.Exists(privateKeyPath) || !File.Exists(publicKeyPath))
            {
                var ecdsa = ECDsa.Create();
                ecdsa.GenerateKey(ECCurve.NamedCurves.nistP256);
                var privatePem = ecdsa.ExportECPrivateKeyPem();
                var publicPem = ecdsa.ExportSubjectPublicKeyInfoPem();
                File.WriteAllText(privateKeyPath, privatePem);
                File.WriteAllText(publicKeyPath, publicPem);
            }
            else
            {
                var privatePem = File.ReadAllText(privateKeyPath);
                var publicPem = File.ReadAllText(publicKeyPath);
                PrivateECDsa.ImportFromPem(privatePem);
                PublicECDsa.ImportFromPem(publicPem);
            }
        }

        /// <summary>
        ///     对指定内容进行加盐
        /// </summary>
        /// <param name="secret">待加盐内容</param>
        /// <param name="salt">此次加盐使用的盐</param>
        /// <returns>已加盐的密文</returns>
        public static byte[] Salt(byte[] secret, out byte[] salt)
        {
            salt = CreateSalt();
            var combine = secret.Concat(salt).ToArray();
            var salted = SHA256.HashData(combine);
            return salted;
        }

        /// <summary>
        ///     通过用户携带的盐验证提供的密码
        /// </summary>
        /// <param name="user"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static bool Verify(this User user, byte[] secret)
        {
            var salt = Convert.FromBase64String(user.Salt);
            var combine = secret.Concat(salt).ToArray();
            var salted = SHA256.HashData(combine);
            return Convert.FromBase64String(user.Passphrase).SequenceEqual(salted);
        }

        /// <summary>
        ///     产生一个随机长度的盐
        /// </summary>
        /// <returns></returns>
        public static byte[] CreateSalt()
        {
            var guid = Guid.NewGuid().ToByteArray()!;
            var length = Random.Shared.Next(guid.Length / 4, guid.Length / 2);
            var start = Random.Shared.Next(0, guid.Length / 2 - 1);
            return SHA256.HashData(guid[start..(length + start)]);
        }

        /// <summary>
        ///     对字符串内容进行hash
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string Sha256(string content)
        {
            var secret = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            var base64 = Convert.ToBase64String(secret);
            return base64;
        }
    }
}
