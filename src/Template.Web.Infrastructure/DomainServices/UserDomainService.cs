using Template.Web.Core;
using Template.Web.Domain.Entities.Account;
using Template.Web.Domain.Services;
using Template.Web.Domain.Utilities;
using StackExchange.Redis;

namespace Template.Web.Infrastructure.DomainServices
{
    public class UserDomainService : IUserDomainService
    {
        public UserDomainService(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public async Task CacheCaptchaAnswerAsync(Captcha captcha, int expire)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Captcha, captcha.Id);
            await database.StringSetAsync(key, captcha.Answer, TimeSpan.FromSeconds(expire));
        }

        public async Task CacheTokenAsync(Guid userId, string token)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Token, userId);
            await database.StringSetAsync(key, token);
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