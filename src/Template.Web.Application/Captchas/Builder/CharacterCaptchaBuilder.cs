using Template.Web.Domain.Entities.Account;

namespace Template.Web.Application.Captchas.Builder
{
    public class CharacterCaptchaBuilder : CaptchaBuilder
    {
        private IEnumerable<int> chars = CaptchaUtil.NumChars;
        public int Length { get; private set; } = 4;

        public override Captcha Build()
        {
            if (CaptchaGenOptions is null) throw new ArgumentNullException("captcha generate options was not set");
            var text = chars.GenCharacterText(Length);
            var captcha = new Captcha()
            {
                Type = CaptchaType.Character,
                Image = CaptchaUtil.GenerateImage(CaptchaGenOptions, text, GenNosie, GenLines, GenCircles),
                Pixel = new(CaptchaGenOptions.Width, CaptchaGenOptions.Height),
                Answer = new string(text)
            };
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

        public CharacterCaptchaBuilder WithLowerCase()
        {
            chars = chars.Concat(CaptchaUtil.LowerChars);
            return this;
        }

        public CharacterCaptchaBuilder WithUpperCase()
        {
            chars = chars.Concat(CaptchaUtil.UpperChars);
            return this;
        }
    }
}