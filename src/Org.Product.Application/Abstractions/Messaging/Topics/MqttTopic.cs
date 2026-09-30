using System.Diagnostics.CodeAnalysis;

namespace Org.Product.Application.Abstractions.Messaging.Topics;

public record MqttTopic
{
    public MqttTopic(TopicType prefix, MqttDirection direction, MqttTag tag, string? suffix = null)
    {
        TopicSyntax.ValidateEnum(prefix, nameof(prefix));
        TopicSyntax.ValidateEnum(direction, nameof(direction));
        TopicSyntax.ValidateEnum(tag, nameof(tag));
        TopicSyntax.ValidateSuffix(suffix);
        Prefix = prefix;
        Direction = direction;
        Tag = tag;
        Suffix = suffix;
        TopicSyntax.ValidateTopic($"{TopicSyntax.Format(prefix)}/{TopicSyntax.Format(direction)}/{TopicSyntax.Format(tag)}{TopicSyntax.FormatSuffix(suffix)}");
    }

    public TopicType Prefix { get; }
    public MqttDirection Direction { get; }
    public MqttTag Tag { get; }
    public string? Suffix { get; }

    public static bool TryParse(string? topic, [NotNullWhen(true)] out MqttTopic? result)
    {
        result = null;
        if (!TopicSyntax.IsValidTopic(topic))
        {
            return false;
        }

        var levels = topic!.Split('/');
        if (levels.Length < 3
            || !TopicSyntax.TryParse<TopicType>(levels[0], out var prefix)
            || !TopicSyntax.TryParse<MqttDirection>(levels[1], out var direction)
            || !TopicSyntax.TryParse<MqttTag>(levels[2], out var tag)
            || !TopicSyntax.TryGetSuffix(levels, 3, out var suffix))
        {
            return false;
        }

        result = new MqttTopic(prefix, direction, tag, suffix);
        return true;
    }

    public static MqttTopic FromString(string topic) =>
        TryParse(topic, out var result) ? result : throw new FormatException("Invalid basic MQTT topic.");

    public override string ToString() =>
        $"{TopicSyntax.Format(Prefix)}/{TopicSyntax.Format(Direction)}/{TopicSyntax.Format(Tag)}{TopicSyntax.FormatSuffix(Suffix)}";
}
