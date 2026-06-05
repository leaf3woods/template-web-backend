using System.Reflection;
using SkiaSharp;

namespace Template.Web.Application.Captchas
{
    public static class CaptchaUtil
    {
        private const int _angleRange = 45;

        private static (int, int) _lineCountRange = new(3, 6);

        private const int _lineWidth = 1;

        private const int _quality = 100;

        private const double _drawPositionLimit = 0.8;

        private const float _noiseSize = (float)1;

        private static (int, int) _circleRadiusRange = new(6, 12);

        private static (int, int) _circleCountRange = new(4, 8);

        public static byte[] GenerateImage(
            CaptchaGenOptions options,
            char[] text,
            bool noise = false,
            bool line = false,
            bool circle = false
        )
        {
            if (options is null)
            {
                throw new ArgumentNullException("no options was set");
            }
            var random = new Random();

            using var image2d = new SKBitmap(
                options.Width,
                options.Height,
                SKColorType.Bgra8888,
                SKAlphaType.Premul
            );
            using var canvas = new SKCanvas(image2d);

            var background = options.Background ?? SKColors.WhiteSmoke;
            canvas.Clear(background);

            var fontSize = (options.Width) / (text.Length + 1);
            using var drawStyle = new SKPaint { IsAntialias = true, TextSize = fontSize };

            var limitHeigtStart = (int)(options.Height * (1 - _drawPositionLimit)) / 2;
            var limitHeightEnd = options.Height - limitHeigtStart;

            if (circle)
            {
                var count = random.Next(_circleCountRange.Item1, _circleCountRange.Item2);
                drawStyle.Style = SKPaintStyle.Stroke;
                drawStyle.StrokeWidth = 2;
                for (int i = 0; i < count; i++)
                {
                    drawStyle.Color = Colors[random.Next(0, Colors.Length - 1)];
                    canvas.DrawCircle(
                        random.Next(options.Width),
                        random.Next(limitHeigtStart, limitHeightEnd),
                        random.Next(_circleRadiusRange.Item1, _circleRadiusRange.Item2),
                        drawStyle
                    );
                }
                drawStyle.Style = SKPaintStyle.Fill;
                drawStyle.StrokeWidth = 1;
            }

            using var font = SKTypeface.FromFamilyName(
                options.FontFamily,
                SKFontStyleWeight.SemiBold,
                SKFontStyleWidth.ExtraCondensed,
                SKFontStyleSlant.Upright
            );
            float py = (options.Height) / 2;
            drawStyle.Typeface = font;
            var offset = fontSize / 2;
            var offsetY = fontSize * (float)(3 / 8);
            for (int i = 0; i < text.Length; i++)
            {
                float px = i * fontSize;
                float angle = random.Next(-_angleRange, _angleRange);
                canvas.Translate(offset, offsetY);
                canvas.RotateDegrees(angle, px, py);
                drawStyle.Color = Colors[random.Next(0, Colors.Length - 1)];
                canvas.DrawText(text[i].ToString(), px, py, drawStyle);
                canvas.RotateDegrees(-angle, px, py);
                canvas.Translate(-offset, -offsetY);
            }

            if (noise)
            {
                for (int i = 0; i < options.Width * 2; i++)
                {
                    drawStyle.Color = Colors[random.Next(0, Colors.Length - 1)];
                    canvas.DrawRect(
                        random.Next(options.Width),
                        random.Next(options.Height),
                        _noiseSize,
                        _noiseSize,
                        drawStyle
                    );
                }
            }

            if (line)
            {
                var lineCount = random.Next(_lineCountRange.Item1, _lineCountRange.Item2);
                for (int i = 0; i < lineCount; i++)
                {
                    drawStyle.Color = Colors[random.Next(0, Colors.Length - 1)];
                    drawStyle.StrokeWidth = _lineWidth;
                    canvas.DrawLine(
                        random.Next(0, options.Width),
                        random.Next(limitHeigtStart, limitHeightEnd),
                        random.Next(0, options.Width),
                        random.Next(limitHeigtStart, limitHeightEnd),
                        drawStyle
                    );
                }
            }

            using var img = SKImage.FromBitmap(image2d);
            using var data = img.Encode(SKEncodedImageFormat.Png, _quality);
            return data.ToArray();
        }

        public static char[] GenCharacterText(this IEnumerable<int> chars, int length)
        {
            var random = new Random();
            var result = new char[length];
            var array = chars.ToArray();
            for (int i = 0; i < length; i++)
            {
                var index = random.Next(0, array.Length - 1);
                result[i] = (char)array[index];
            }
            return result;
        }

        public static char[] GenEquation(out int result)
        {
            var random = new Random();
            var first = random.Next(0, 9);
            var next = random.Next(0, 9);
            var @operator = (Operator)random.Next(0, 3);
            string? chars;
            switch (@operator)
            {
                case Operator.Add:
                    chars = $"{first}+{next}=?";
                    result = (first + next);
                    break;

                case Operator.Subtract:
                    chars = $"{first}-{next}=?";
                    result = (first - next);
                    break;

                case Operator.Multiply:
                    chars = $"{first}*{next}=?";
                    result = (first * next);
                    break;

                default:
                    throw new ArgumentException("unknow operator");
            }
            return chars.ToCharArray();
        }

        public static readonly SKColor[] Colors = typeof(SKColors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (SKColor)f.GetValue(null)!)
            .Where(f =>
                f.Alpha != 0 && ((0.299 * f.Red + 0.587 * f.Green + 0.114 * f.Blue) / 255.0) <= 0.6
            )
            .ToArray();

        public static readonly IEnumerable<int> NumChars = Enumerable.Range(48, 10);
        public static readonly IEnumerable<int> LowerChars = Enumerable.Range(65, 26);
        public static readonly IEnumerable<int> UpperChars = Enumerable.Range(97, 26);
    }

    public enum Operator
    {
        Add,
        Subtract,
        Multiply,
    }
}
