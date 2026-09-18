using Org.Product.Application.Abstractions.Captchas;

namespace Org.Product.Infrastructure.Adapters.Captchas.Builder;

public class CharacterCaptchaBuilder : CaptchaBuilder
{
    private IEnumerable<int> chars = CaptchaUtil.NumChars;
    public int Length { get; private set; } = 4;

    public override Captcha Build()
    {
        if (CaptchaGenSettings is null)
        {
            throw new InvalidOperationException("Captcha generation settings were not set.");
        }

        var text = chars.GenCharacterText(Length);
        var captcha = new Captcha()
        {
            Type = CaptchaType.Character,
            Image = CaptchaUtil.GenerateImage(
                CaptchaGenSettings,
                text,
                GenNosie,
                GenLines,
                GenCircles
            ),
            Pixel = new(CaptchaGenSettings.Width, CaptchaGenSettings.Height),
            Answer = new string(text),
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

    public CharacterCaptchaBuilder WithLowerCase()
    {
        chars = chars.Concat(CaptchaUtil.LowerChars);
        return this;
    }

    public CharacterCaptchaBuilder WithUpperCase()
    {
        chars = chars.Concat(CaptchaUtil.UpperChars);
        return this;
    }
}
