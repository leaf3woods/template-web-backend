using System.Drawing;

namespace Org.Product.Application.Abstractions.Captchas;

public class CaptchaGenSettings
{
    public string? FontFamily { get; set; }
    public Color? Background { get; set; }
    public CaptchaType? Type { get; set; } = CaptchaType.Character;
    public int Width { get; set; }
    public int Height { get; set; }
}
