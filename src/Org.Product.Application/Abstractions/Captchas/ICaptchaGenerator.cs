using Org.Product.Application.Abstractions.Authentication;

namespace Org.Product.Application.Abstractions.Captchas;

public interface ICaptchaGenerator : ISecurityService
{
    Captcha Generate();
}
