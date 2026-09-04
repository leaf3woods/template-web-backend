namespace Template.Web.WebApi.Options;

public sealed class OpenApiOptions
{
    public const string SectionName = "OpenApiInfo";

    public string Description { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
