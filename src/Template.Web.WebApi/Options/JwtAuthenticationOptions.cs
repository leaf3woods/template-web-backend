namespace Template.Web.WebApi.Options;

public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Jwt";

    public string KeyFolder { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
