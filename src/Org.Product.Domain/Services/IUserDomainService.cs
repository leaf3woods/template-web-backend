using Org.Product.Domain.Entities.Account;

namespace Org.Product.Domain.Services
{
    public interface IUserDomainService : IDomainService
    {
        public Task CacheCaptchaAnswerAsync(Captcha captcha, TimeSpan? expiration = null);

        public Task CacheTokenAsync(Guid userId, string token, TimeSpan? expiration = null);

        public Task<bool> VerifyCaptchaAnswerAsync(Captcha captcha);

        public Task<bool> VerifyTokenAsync(Guid userId, string token);

        public Task<bool> DeleteTokenAsync(Guid userId);

        public void WithSalt(ref User user, string password);
    }
}
