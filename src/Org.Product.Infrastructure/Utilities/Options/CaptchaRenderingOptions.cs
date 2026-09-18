using System.Drawing;

namespace Org.Product.Infrastructure.Utilities.Options;

public sealed class CaptchaRenderingOptions
{
    public const string SectionName = "Captcha";
    public string FontFamily { get; set; } = "consolas";
    public int Height { get; set; } = 80;
    public int Width { get; set; } = 200;
    public Color? Background { get; set; }
    public bool EnableNoise { get; set; } = true;
    public bool EnableLines { get; set; } = true;
    public bool EnableCircles { get; set; } = true;
}
