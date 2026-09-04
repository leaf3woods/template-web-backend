namespace Org.Product.Application.Options;

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    public bool RequireVerification { get; set; } = true;

    public int ExpireSeconds { get; set; } = 180;

    public string FontFamily { get; set; } = "consolas";

    public int Height { get; set; } = 80;

    public int Width { get; set; } = 200;

    public bool EnableNoise { get; set; } = true;

    public bool EnableLines { get; set; } = true;

    public bool EnableCircles { get; set; } = true;

    public TimeSpan Expiration => TimeSpan.FromSeconds(ExpireSeconds);
}
