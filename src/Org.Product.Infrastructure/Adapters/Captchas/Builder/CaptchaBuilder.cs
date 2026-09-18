using Org.Product.Application.Abstractions.Captchas;

namespace Org.Product.Infrastructure.Adapters.Captchas.Builder;

public abstract class CaptchaBuilder
{
    protected bool GenLines { get; set; } = false;
    protected bool GenNosie { get; set; } = false;
    protected bool GenCircles { get; set; } = false;
    protected CaptchaGenSettings? CaptchaGenSettings { get; set; }

    public abstract CaptchaBuilder WithNoise();

    public abstract CaptchaBuilder WithLines();

    public abstract CaptchaBuilder WithCircles();

    public abstract CaptchaBuilder WithGenSettings(CaptchaGenSettings settings);

    public static TBuilder Create<TBuilder>()
        where TBuilder : CaptchaBuilder, new()
    {
        return new TBuilder();
    }

    public abstract Captcha Build();
}
