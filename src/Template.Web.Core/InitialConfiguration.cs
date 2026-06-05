using Microsoft.Extensions.Configuration;

namespace Template.Web.Core.Utilities
{
    public static class InitialConfiguration
    {
        public static void Initialize(IConfiguration configuration)
        {
            var jwtSetting = configuration.GetSection(nameof(Jwt));
            Jwt.Issuer =
                jwtSetting.GetValue<string>(nameof(Jwt.Issuer))
                ?? throw new Exception("missing issuer in jwt setting section");
            Jwt.KeyFolder =
                jwtSetting.GetValue<string>(nameof(Jwt.KeyFolder))
                ?? throw new Exception("missing key folder in jwt setting section");
            Jwt.Audience =
                jwtSetting.GetValue<string>(nameof(Jwt.Audience))
                ?? throw new Exception("missing audience in jwt setting section");
            Jwt.ExpireMin = TimeSpan.FromMinutes(jwtSetting.GetValue<int>(nameof(Jwt.ExpireMin)));

            var apiInfo = configuration.GetSection(nameof(OpenApiInfo));
            OpenApi.Description =
                apiInfo.GetValue<string>(nameof(OpenApi.Description))
                ?? throw new Exception("config not found");
            OpenApi.Title =
                apiInfo.GetValue<string>(nameof(OpenApi.Title))
                ?? throw new Exception("config not found");
            OpenApi.Name =
                apiInfo.GetValue<string>(nameof(OpenApi.Name))
                ?? throw new Exception("config not found");
            OpenApi.Email =
                apiInfo.GetValue<string>(nameof(OpenApi.Email))
                ?? throw new Exception("config not found");
            OpenApi.Url =
                apiInfo.GetValue<string>(nameof(OpenApi.Url))
                ?? throw new Exception("config not found");
        }

        public static JwtSettings Jwt { get; private set; } = new JwtSettings();
        public static OpenApiInfo OpenApi { get; private set; } = new OpenApiInfo();

        public static bool IsDevelopment { get; private set; } =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        public class JwtSettings
        {
            public string KeyFolder { get; set; } = null!;
            public string Issuer { get; set; } = null!;
            public string Audience { get; set; } = null!;
            public TimeSpan ExpireMin { get; set; }
        }

        public class OpenApiInfo
        {
            public string Description { get; set; } = null!;
            public string Title { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Url { get; set; } = null!;
        }
    }
}
