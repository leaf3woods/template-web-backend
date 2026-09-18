using System.Drawing;
using Microsoft.Extensions.Options;
using Moq;
using Org.Product.Application.Abstractions.Captchas;
using Org.Product.Application.Dtos;
using Org.Product.Application.Services.Base;
using Org.Product.Infrastructure.Adapters.Captchas;
using Org.Product.Infrastructure.Utilities.Options;
using Org.Product.WebApi.Controllers;
using SkiaSharp;

namespace Org.Product.Tests;

public sealed class CaptchaGeneratorTests
{
    [Fact]
    public void Generate_WithoutSettings_UsesConfiguredDefaultsAndCharacterType()
    {
        var generator = CreateGenerator();

        var captcha = generator.Generate();

        Assert.Equal(CaptchaType.Character, captcha.Type);
        Assert.Equal((180, 72), captcha.Pixel);
        Assert.Equal(4, captcha.Answer.Length);
        Assert.NotNull(captcha.Image);
        Assert.NotEmpty(captcha.Image);
    }

    [Fact]
    public void Generate_WithQuestionType_ReturnsQuestionCaptcha()
    {
        var generator = CreateGenerator();

        var captcha = generator.Generate(new CaptchaGenSettings
        {
            Type = CaptchaType.Question,
        });

        Assert.Equal(CaptchaType.Question, captcha.Type);
        Assert.Equal((180, 72), captcha.Pixel);
        Assert.True(int.TryParse(captcha.Answer, out _));
        Assert.NotNull(captcha.Image);
        Assert.NotEmpty(captcha.Image);
    }

    [Fact]
    public void Generate_WithZeroDimensions_UsesConfiguredDefaults()
    {
        var generator = CreateGenerator();

        var captcha = generator.Generate(new CaptchaGenSettings
        {
            Type = CaptchaType.Character,
            Width = 0,
            Height = 0,
        });

        Assert.Equal((180, 72), captcha.Pixel);
    }

    [Fact]
    public void Generate_WithRuntimeSettings_OverridesDimensionsAndBackground()
    {
        var generator = CreateGenerator();
        var background = Color.FromArgb(255, 12, 34, 56);

        var captcha = generator.Generate(new CaptchaGenSettings
        {
            Type = CaptchaType.Character,
            Width = 240,
            Height = 96,
            Background = background,
        });

        Assert.Equal((240, 96), captcha.Pixel);
        Assert.NotNull(captcha.Image);
        using var bitmap = SKBitmap.Decode(captcha.Image);
        Assert.NotNull(bitmap);
        Assert.Equal(
            new SKColor(background.R, background.G, background.B, background.A),
            bitmap.GetPixel(239, 95)
        );
    }

    [Theory]
    [InlineData(-1, 72)]
    [InlineData(180, -1)]
    public void Generate_WithNegativeDimension_Throws(int width, int height)
    {
        var generator = CreateGenerator();
        var settings = new CaptchaGenSettings
        {
            Width = width,
            Height = height,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(settings));
    }

    [Fact]
    public void Generate_WithHanType_ThrowsNotSupportedException()
    {
        var generator = CreateGenerator();

        Assert.Throws<NotSupportedException>(() => generator.Generate(new CaptchaGenSettings
        {
            Type = CaptchaType.Han,
        }));
    }

    [Fact]
    public void Generate_WithUndefinedType_ThrowsArgumentOutOfRangeException()
    {
        var generator = CreateGenerator();

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(
            new CaptchaGenSettings { Type = (CaptchaType)999 }
        ));
    }

    [Fact]
    public async Task GetCaptcha_WithType_ForwardsRequestedType()
    {
        var service = new Mock<IUserService>();
        service.Setup(candidate => candidate.GenerateCaptchaAsync(CaptchaType.Question))
            .ReturnsAsync(new CaptchaReadDto());
        var controller = new HomeController(service.Object);

        await controller.GetCaptcha(CaptchaType.Question);

        service.Verify(
            candidate => candidate.GenerateCaptchaAsync(CaptchaType.Question),
            Times.Once
        );
    }

    private static SkiaCaptchaGenerator CreateGenerator() => new(Options.Create(
        new CaptchaRenderingOptions
        {
            FontFamily = "consolas",
            Width = 180,
            Height = 72,
            EnableNoise = false,
            EnableLines = false,
            EnableCircles = false,
        }
    ));
}
