using StackExchange.Redis;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Services;
using Org.Product.Domain.Shared;
using Org.Product.Domain.Utilities;

namespace Org.Product.Infrastructure.DomainServices
{
    public class UserDomainService : IUserDomainService
    {
        public UserDomainService(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public async Task CacheCaptchaAnswerAsync(Captcha captcha, TimeSpan expiration)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Captcha, captcha.Id);
            await database.StringSetAsync(key, captcha.Answer, expiration);
        }

        public async Task CacheTokenAsync(Guid userId, string token, TimeSpan expiration)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Token, userId);
            await database.StringSetAsync(key, token, expiration);
        }

        public async Task<bool> DeleteTokenAsync(Guid userId)
        {
            var databse = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Token, userId);
            return await databse.KeyDeleteAsync(key);
        }

        public async Task<bool> VerifyCaptchaAnswerAsync(Captcha captcha)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Captcha, captcha.Id);
            var raw = (await database.StringGetAsync(key));
            if (!raw.HasValue)
                throw new Exception("captcha is invalid");
            return raw.ToString() == captcha.Answer;
        }

        public async Task<bool> VerifyTokenAsync(Guid userId, string token)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Token, userId);
            var raw = (await database.StringGetAsync(key));
            return !raw.HasValue || raw.ToString() == token;
        }

        public void WithSalt(ref User user, string password)
        {
            var bytes = Convert.FromBase64String(password);
            user.Passphrase = Convert.ToBase64String(CryptoUtil.Salt(bytes, out var salt));
            user.Salt = Convert.ToBase64String(salt);
        }
    }
}
