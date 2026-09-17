
using Org.Product.Application.Abstractions.Authentication;

namespace Org.Product.Application.Abstractions.Captchas;

public interface ICaptchaChallengeStore : ISecurityStore
{
    Task SaveAsync(Captcha captcha, TimeSpan? expiration = null);
    Task<bool> VerifyAsync(Captcha captcha);
}
