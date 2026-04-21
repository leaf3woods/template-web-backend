using SkiaSharp;

namespace Template.Web.Application.Captchas
{
    public class CaptchaGenOptions
    {
        public string FontFamily { get; set; } = string.Empty;
        public SKColor? Background { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}