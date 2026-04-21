using Template.Web.Domain.Entities.Account;

namespace Template.Web.Application.Captchas.Builder
{
    public class QuestionCaptchaBuilder : CaptchaBuilder
    {
        public override Captcha Build()
        {
            if (CaptchaGenOptions is null) throw new ArgumentNullException("captcha generate options was not set");
            var equation = CaptchaUtil.GenEquation(out var answer);
            var captcha = new Captcha()
            {
                Type = CaptchaType.Question,
                Image = CaptchaUtil.GenerateImage(CaptchaGenOptions, equation, GenNosie, GenLines, GenCircles),
                Pixel = new(CaptchaGenOptions.Width, CaptchaGenOptions.Height),
                Answer = answer.ToString()
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
    }
}