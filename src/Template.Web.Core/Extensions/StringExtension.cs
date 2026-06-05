namespace Template.Web.Application.Main.Utilities;

public static class StringExtension
{
    public static bool IsNullOrWhiteSpace(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static IEnumerable<string> GetNameSpaces(this string value)
    {
        return value.Split('.');
    }
}
