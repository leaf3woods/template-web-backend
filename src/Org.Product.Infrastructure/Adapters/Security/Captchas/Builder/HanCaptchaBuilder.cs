using Org.Product.Application.Abstractions.Security;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Infrastructure.Adapters.Security.Captchas.Builder
{
    public class HanCaptchaBuilder : CaptchaBuilder
    {
        public int Length { get; set; }

        public override Captcha Build()
        {
            var captcha = new Captcha() { Type = CaptchaType.Han };
            return captcha;
        }

        public override CaptchaBuilder WithGenOption(CaptchaGenOptions options)
        {
            CaptchaGenOptions = options;
            return this;
        }

        public override CaptchaBuilder WithLines()
        {
            GenLines = true;
            return this;
        }

        public override CaptchaBuilder WithNoise()
        {
            GenNosie = true;
            return this;
        }

        public override CaptchaBuilder WithCircles()
        {
            GenCircles = true;
            return this;
        }
    }
}
