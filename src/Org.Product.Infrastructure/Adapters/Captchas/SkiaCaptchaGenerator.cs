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

    public Captcha Generate(CaptchaGenSettings? settings = null)
    {
        var height = settings?.Height;
        var width = settings?.Width;
        var resolvedSettings = new CaptchaGenSettings
        {
            FontFamily = settings?.FontFamily ?? _options.FontFamily,
            Height = height is null or 0 ? _options.Height : height.Value,
            Width = width is null or 0 ? _options.Width : width.Value,
            Background = settings?.Background ?? _options.Background
        };
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resolvedSettings.Height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resolvedSettings.Width);

        CaptchaBuilder builder = settings?.Type switch
        {
            CaptchaType.Question => CaptchaBuilder.Create<QuestionCaptchaBuilder>(),
            null or CaptchaType.Character => CaptchaBuilder.Create<CharacterCaptchaBuilder>(),
            CaptchaType.Han => throw new NotSupportedException("Han captcha type is not supported"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(CaptchaGenSettings.Type),
                settings.Type,
                "Unsupported captcha type."
            )
        };

        builder = builder.WithGenSettings(resolvedSettings);
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
