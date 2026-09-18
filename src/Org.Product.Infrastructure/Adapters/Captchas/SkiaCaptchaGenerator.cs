using Microsoft.Extensions.Options;
using Org.Product.Application.Abstractions.Captchas;
using Org.Product.Infrastructure.Adapters.Captchas.Builder;
using Org.Product.Infrastructure.Utilities.Options;

namespace Org.Product.Infrastructure.Adapters.Captchas;

public sealed class SkiaCaptchaGenerator : ICaptchaGenerator
{
    private readonly CaptchaRenderingOptions _options;

    public SkiaCaptchaGenerator(IOptions<CaptchaRenderingOptions> options)
    {
        _options = options.Value;
    }

    public Captcha Generate(CaptchaGenOptions? options = null)
    {
        var genOptions = options ?? new CaptchaGenOptions
        {
            FontFamily = _options.FontFamily,
            Height = _options.Height,
            Width = _options.Width,
        };
        var builder = CaptchaBuilder.Create<QuestionCaptchaBuilder>()
            .WithGenOption(genOptions);
        if (_options.EnableNoise)
        {
            builder = builder.WithNoise();
        }

        if (_options.EnableLines)
        {
            builder = builder.WithLines();
        }

        if (_options.EnableCircles)
        {
            builder = builder.WithCircles();
        }

        return builder.Build();
    }
}
