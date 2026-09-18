using Org.Product.Application.Abstractions.Captchas;

namespace Org.Product.Infrastructure.Adapters.Captchas.Builder;

public class QuestionCaptchaBuilder : CaptchaBuilder
{
    public override Captcha Build()
    {
        if (CaptchaGenSettings is null)
        {
            throw new InvalidOperationException("Captcha generation settings were not set.");
        }

        var equation = CaptchaUtil.GenEquation(out var answer);
        var captcha = new Captcha()
        {
            Type = CaptchaType.Question,
            Image = CaptchaUtil.GenerateImage(
                CaptchaGenSettings,
                equation,
                GenNosie,
                GenLines,
                GenCircles
            ),
            Pixel = new(CaptchaGenSettings.Width, CaptchaGenSettings.Height),
            Answer = answer.ToString(),
        };
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
