using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Services;
using Org.Product.Domain.Shared;
using Org.Product.Domain.Utilities;
using StackExchange.Redis;

namespace Org.Product.Infrastructure.DomainServices
{
    public class UserDomainService : IUserDomainService
    {
        public UserDomainService(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public async Task CacheCaptchaAnswerAsync(Captcha captcha, TimeSpan? expiration = null)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Captcha, captcha.Id);
            var task = expiration is null
                ? database.StringSetAsync(key, captcha.Answer)
                : database.StringSetAsync(key, captcha.Answer, expiration.Value);
            await task;
        }

        public async Task CacheTokenAsync(Guid userId, string token, TimeSpan? expiration = null)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Token, userId);
            var task = expiration is null
                ? database.StringSetAsync(key, token)
                : database.StringSetAsync(key, token, expiration.Value);
            await task;
        }

        public async Task<bool> ExistsInCacheAsync(params IEnumerable<Guid> userIds)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var scanTasks = userIds
                .Distinct()
                .Select(userId => new RedisKey(
                    string.Format(CacheKeyFormatter.Token, userId)
                ))
                .Select(key => database.KeyExistsAsync(key))
                .ToArray();
            if (scanTasks.Length == 0)
            {
                return false;
            }
            return (await Task.WhenAll(scanTasks)).All(exists => exists);
        }

        public async Task<bool> DeleteTokenAsync(Guid userId, string? expectedToken = null)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Token, userId);
            if (expectedToken is null)
            {
                return await database.KeyDeleteAsync(key);
            }

            // An older in-flight logout must not delete a newer login session.
            var result = await database.ScriptEvaluateAsync(
                "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end",
                [new RedisKey(key)],
                [new RedisValue(expectedToken)]
            );
            return (long)result == 1;
        }

        public async Task<bool> VerifyCaptchaAnswerAsync(Captcha captcha)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Captcha, captcha.Id);
            var raw = await database.StringGetAsync(key);
            if (!raw.HasValue)
                throw new Exception("captcha is invalid");
            return raw.ToString() == captcha.Answer;
        }

        public async Task<bool> VerifyTokenAsync(Guid userId, string token)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Token, userId);
            var raw = await database.StringGetAsync(key);
            return raw.HasValue && raw.ToString() == token;
        }

        public void WithSalt(ref User user, string password)
        {
            var bytes = Convert.FromBase64String(password);
            user.Passphrase = Convert.ToBase64String(CryptoUtil.Salt(bytes, out var salt));
            user.Salt = Convert.ToBase64String(salt);
        }
    }
}
