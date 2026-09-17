using Org.Product.Application.Abstractions.Captchas;
using Org.Product.Infrastructure.Utilities;
using StackExchange.Redis;

namespace Org.Product.Infrastructure.Adapters.Captchas;

public sealed class RedisCaptchaChallengeStore : ICaptchaChallengeStore
{
    private readonly IConnectionMultiplexer _connection;

    public RedisCaptchaChallengeStore(IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public async Task SaveAsync(Captcha captcha, TimeSpan? expiration = null)
    {
        var database = _connection.GetDatabase();
        var key = string.Format(CacheKeyFormatter.Captcha, captcha.Id);
        await (expiration is null
            ? database.StringSetAsync(key, captcha.Answer)
            : database.StringSetAsync(key, captcha.Answer, expiration.Value));
    }

    public async Task<bool> VerifyAsync(Captcha captcha)
    {
        var value = await _connection.GetDatabase()
            .StringGetAsync(string.Format(CacheKeyFormatter.Captcha, captcha.Id));
        if (!value.HasValue)
        {
            throw new Exception("captcha is invalid");
        }

        return value.ToString() == captcha.Answer;
    }
}
