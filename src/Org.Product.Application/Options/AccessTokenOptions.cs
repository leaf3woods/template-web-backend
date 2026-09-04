namespace Org.Product.Application.Options;

public sealed class AccessTokenOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpireMin { get; set; }

    public TimeSpan Expiration => TimeSpan.FromMinutes(ExpireMin);
}
