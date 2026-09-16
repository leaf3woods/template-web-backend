namespace Org.Product.Application.Utilities.Options;

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    public bool RequireVerification { get; set; } = true;

    public int ExpireSeconds { get; set; } = 180;

    public TimeSpan Expiration => TimeSpan.FromSeconds(ExpireSeconds);
}
