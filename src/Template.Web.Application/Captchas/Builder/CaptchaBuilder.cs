using Template.Web.Domain.Entities.Account;

namespace Template.Web.Application.Captchas.Builder
{
    public abstract class CaptchaBuilder
    {
        protected bool GenLines { get; set; } = false;
        protected bool GenNosie { get; set; } = false;
        protected bool GenCircles { get; set; } = false;
        protected CaptchaGenOptions? CaptchaGenOptions { get; set; }

        public abstract CaptchaBuilder WithNoise();

        public abstract CaptchaBuilder WithLines();

        public abstract CaptchaBuilder WithCircles();

        public abstract CaptchaBuilder WithGenOption(CaptchaGenOptions options);

        public static TBuilder Create<TBuilder>()
            where TBuilder : CaptchaBuilder, new()
        {
            return new TBuilder();
        }

        public abstract Captcha Build();
    }
}
