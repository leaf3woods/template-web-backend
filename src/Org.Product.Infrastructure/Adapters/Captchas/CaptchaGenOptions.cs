using SkiaSharp;

namespace Org.Product.Infrastructure.Adapters.Captchas;

public class CaptchaGenOptions
{
    public string FontFamily { get; set; } = string.Empty;
    public SKColor? Background { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
