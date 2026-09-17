using Microsoft.Extensions.DependencyInjection;
using Org.Product.Application.Utilities.Options;
using Org.Product.Infrastructure.Adapters.Security.Options;
using Org.Product.WebApi.Utilities.Options;

namespace Org.Product.WebApi.Utilities
{
    public static class OptionsExtensions
    {
        public static void AddAllOptions(this IServiceCollection services)
        {
            services
                .AddOptions<CaptchaOptions>()
                .BindConfiguration(CaptchaOptions.SectionName)
                .Validate(
                    options =>
                        options.ExpireSeconds > 0,
                    "Captcha requires a positive ExpireSeconds."
                )
                .ValidateOnStart();

            services
                .AddOptions<CaptchaRenderingOptions>()
                .BindConfiguration(CaptchaRenderingOptions.SectionName)
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(options.FontFamily)
                        && options.Height > 0
                        && options.Width > 0,
                    "Captcha requires FontFamily and positive dimensions."
                )
                .ValidateOnStart();

            services
                .AddOptions<JwtAuthenticationOptions>()
                .BindConfiguration(JwtAuthenticationOptions.SectionName)
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(options.KeyFolder)
                        && !string.IsNullOrWhiteSpace(options.Issuer)
                        && !string.IsNullOrWhiteSpace(options.Audience),
                    "Jwt requires a non-empty KeyFolder, Issuer, and Audience."
                )
                .ValidateOnStart();

            services
                .AddOptions<AccessTokenOptions>()
                .BindConfiguration(AccessTokenOptions.SectionName)
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(options.Issuer)
                        && !string.IsNullOrWhiteSpace(options.Audience)
                        && options.ExpireMin > 0,
                    "Jwt requires Issuer, Audience and a positive ExpireMin."
                )
                .ValidateOnStart();

            services
                .AddOptions<OpenApiOptions>()
                .BindConfiguration(OpenApiOptions.SectionName)
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(options.Description)
                        && !string.IsNullOrWhiteSpace(options.Title)
                        && !string.IsNullOrWhiteSpace(options.Name)
                        && !string.IsNullOrWhiteSpace(options.Email)
                        && Uri.IsWellFormedUriString(options.Url, UriKind.Absolute),
                    "OpenApiInfo requires Description, Title, Name, Email, and an absolute Url."
                )
                .ValidateOnStart();
        }
    }
}
