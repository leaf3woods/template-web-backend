using System.Text;

namespace Org.Product.Application.Abstractions.Messaging.Topics;

internal static class TopicSyntax
{
    private static readonly UTF8Encoding _utf8 = new(false, true);

    internal static string Format<TEnum>(TEnum value)
        where TEnum : struct, Enum => value.ToString().ToLowerInvariant();

    internal static bool TryParse<TEnum>(string level, out TEnum value)
        where TEnum : struct, Enum =>
        Enum.TryParse(level, ignoreCase: true, out value)
        && Enum.IsDefined(value)
        && string.Equals(level, Format(value), StringComparison.Ordinal);

    internal static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Unknown topic value.");
        }
    }

    internal static bool IsValidTopic(string? topic)
    {
        if (string.IsNullOrEmpty(topic) || topic.AsSpan().ContainsAny('\0', '+', '#'))
        {
            return false;
        }

        try
        {
            return _utf8.GetByteCount(topic) <= ushort.MaxValue;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    internal static bool IsValidLevel(string? level) =>
        IsValidTopic(level) && !level!.Contains('/');

    internal static void ValidateLevel(string level, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(level, parameterName);
        if (!IsValidLevel(level))
        {
            throw new ArgumentException("A topic level must be non-empty and contain no '/', '+', '#', or null character, and must be valid MQTT UTF-8.", parameterName);
        }
    }

    internal static void ValidateTopic(string topic)
    {
        if (!IsValidTopic(topic))
        {
            throw new ArgumentException("A topic must be valid MQTT UTF-8, contain no wildcards or null character, and encode to at most 65535 bytes.", nameof(topic));
        }
    }

    internal static void ValidateSuffix(string? suffix)
    {
        if (suffix is null)
        {
            return;
        }

        foreach (var level in suffix.Split('/'))
        {
            ValidateLevel(level, nameof(suffix));
        }
    }

    internal static bool TryGetSuffix(string[] levels, int start, out string? suffix)
    {
        suffix = null;
        if (start == levels.Length)
        {
            return true;
        }

        for (var i = start; i < levels.Length; i++)
        {
            if (!IsValidLevel(levels[i]))
            {
                return false;
            }
        }

        suffix = string.Join('/', levels, start, levels.Length - start);
        return true;
    }

    internal static string FormatSuffix(string? suffix) => suffix is null ? string.Empty : $"/{suffix}";
}
