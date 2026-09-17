using Microsoft.Extensions.Options;
using Org.Product.Application.Abstractions.Security;
using Org.Product.Infrastructure.Adapters.Security.Captchas;
using Org.Product.Infrastructure.Adapters.Security.Captchas.Builder;
using Org.Product.Infrastructure.Adapters.Security.Options;

namespace Org.Product.Infrastructure.Adapters.Security;

public sealed class SkiaCaptchaGenerator : ICaptchaGenerator
{
    private readonly CaptchaRenderingOptions _options;

    public SkiaCaptchaGenerator(IOptions<CaptchaRenderingOptions> options)
    {
        _options = options.Value;
    }

    public Captcha Generate()
    {
        var builder = CaptchaBuilder.Create<QuestionCaptchaBuilder>()
            .WithGenOption(new CaptchaGenOptions
            {
                FontFamily = _options.FontFamily,
                Height = _options.Height,
                Width = _options.Width,
            });
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
