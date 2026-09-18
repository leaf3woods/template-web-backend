using Org.Product.Application.Abstractions.Captchas;

namespace Org.Product.Infrastructure.Adapters.Captchas.Builder;

public class HanCaptchaBuilder : CaptchaBuilder
{
    public int Length { get; set; }

    public override Captcha Build()
    {
        var captcha = new Captcha() { Type = CaptchaType.Han };
        return captcha;
    }

    public override CaptchaBuilder WithGenSettings(CaptchaGenSettings settings)
    {
        CaptchaGenSettings = settings;
        return this;
    }

    public override CaptchaBuilder WithLines()
    {
        GenLines = true;
        return this;
    }

    public override CaptchaBuilder WithNoise()
    {
        GenNosie = true;
        return this;
    }

    public override CaptchaBuilder WithCircles()
    {
        GenCircles = true;
        return this;
    }
}
