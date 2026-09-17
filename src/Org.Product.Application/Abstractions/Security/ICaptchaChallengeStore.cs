namespace Org.Product.Application.Abstractions.Security;

public interface ICaptchaChallengeStore : ISecurityStore
{
    Task SaveAsync(Captcha captcha, TimeSpan? expiration = null);
    Task<bool> VerifyAsync(Captcha captcha);
}
