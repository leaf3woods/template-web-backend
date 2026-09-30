using System.Diagnostics.CodeAnalysis;

namespace Org.Product.Application.Abstractions.Messaging.Topics;

public sealed record UserTopic : MqttTopic
{
    public const string UserCodeName = "user";

    public UserTopic(TopicType prefix, MqttDirection direction, MqttTag tag, string? userCode = null, string? userTag = null, string? suffix = null)
        : base(prefix, direction, tag, suffix)
    {
        if (userCode is not null)
        {
            TopicSyntax.ValidateLevel(userCode, nameof(userCode));
        }

        if (!string.IsNullOrEmpty(userTag))
        {
            TopicSyntax.ValidateLevel(userTag, nameof(userTag));
        }

        UserCode = userCode;
        UserTag = string.IsNullOrEmpty(userTag) ? null : userTag;
        TopicSyntax.ValidateTopic(ToString());
    }

    public string? UserCode { get; }
    public string? UserTag { get; }

    public static bool TryParse(string? topic, [NotNullWhen(true)] out UserTopic? result)
    {
        result = null;
        if (!TopicSyntax.IsValidTopic(topic))
        {
            return false;
        }

        var levels = topic!.Split('/');
        if (levels.Length < 4
            || (levels[0].Length != 0 && !TopicSyntax.IsValidLevel(levels[0]))
            || !TopicSyntax.TryParse<TopicType>(levels[1], out var prefix)
            || !TopicSyntax.TryParse<MqttDirection>(levels[2], out var direction))
        {
            return false;
        }

        var tagIndex = 3;
        string? userCode = null;
        if (levels[3] == UserCodeName)
        {
            if (levels.Length < 6 || !TopicSyntax.IsValidLevel(levels[4]))
            {
                return false;
            }

            userCode = levels[4];
            tagIndex = 5;
        }

        if (!TopicSyntax.TryParse<MqttTag>(levels[tagIndex], out var tag)
            || !TopicSyntax.TryGetSuffix(levels, tagIndex + 1, out var suffix))
        {
            return false;
        }

        result = new UserTopic(prefix, direction, tag, userCode, levels[0], suffix);
        return true;
    }

    public static UserTopic FromUserString(string topic) =>
        TryParse(topic, out var result) ? result : throw new FormatException("Invalid user MQTT topic.");

    public override string ToString()
    {
        var target = UserCode is null ? string.Empty : $"{UserCodeName}/{UserCode}/";
        // Keep the original leading slash when UserTag is omitted.
        return $"{UserTag}/{TopicSyntax.Format(Prefix)}/{TopicSyntax.Format(Direction)}/{target}{TopicSyntax.Format(Tag)}{TopicSyntax.FormatSuffix(Suffix)}";
    }
}
