using System.Drawing;

namespace Org.Product.Application.Abstractions.Captchas;

public class CaptchaGenOptions
{
    public string FontFamily { get; set; } = string.Empty;
    public Color? Background { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
