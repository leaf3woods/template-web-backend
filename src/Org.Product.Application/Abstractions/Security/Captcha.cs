using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Abstractions.Security
{
    public class Captcha : AggregateRoot<Guid>
    {
        public CaptchaType Type { get; set; }

        public (int, int) Pixel { get; set; }

        public byte[]? Image { get; set; }

        public string Answer { get; set; } = null!;

        public override string ToString()
        {
            var base64 = Image is null ? string.Empty : Convert.ToBase64String(Image);
            return "data:image/png;base64," + base64;
        }
    }

    public enum CaptchaType
    {
        Question,
        Han,
        Character,
    }
}
