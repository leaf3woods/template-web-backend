namespace Org.Product.Application.Abstractions.Security;

public interface ICaptchaGenerator : ISecurityService
{
    Captcha Generate();
}
