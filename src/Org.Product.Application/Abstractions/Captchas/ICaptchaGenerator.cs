namespace Org.Product.Application.Abstractions.Captchas;

public interface ICaptchaGenerator : IApplicationAbstraction
{
    Captcha Generate(CaptchaGenSettings? settings = null);
}
