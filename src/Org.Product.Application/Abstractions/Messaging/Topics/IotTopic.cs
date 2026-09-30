using System.Diagnostics.CodeAnalysis;

namespace Org.Product.Application.Abstractions.Messaging.Topics;

public sealed record IotTopic : MqttTopic
{
    public const string TypeName = "type";
    public const string UriName = "uri";

    public IotTopic(TopicType prefix, MqttDirection direction, MqttTag tag, string type, string uri, string? suffix = null)
        : base(prefix, direction, tag, suffix)
    {
        TopicSyntax.ValidateLevel(type, nameof(type));
        TopicSyntax.ValidateLevel(uri, nameof(uri));
        Type = type;
        Uri = uri;
        TopicSyntax.ValidateTopic(ToString());
    }

    public string Type { get; }
    public string Uri { get; }

    public static bool TryParse(string? topic, [NotNullWhen(true)] out IotTopic? result)
    {
        result = null;
        if (!TopicSyntax.IsValidTopic(topic))
        {
            return false;
        }

        var levels = topic!.Split('/');
        if (levels.Length < 7
            || !TopicSyntax.TryParse<TopicType>(levels[0], out var prefix)
            || !TopicSyntax.TryParse<MqttDirection>(levels[1], out var direction)
            || levels[2] != TypeName
            || !TopicSyntax.IsValidLevel(levels[3])
            || levels[4] != UriName
            || !TopicSyntax.IsValidLevel(levels[5])
            || !TopicSyntax.TryParse<MqttTag>(levels[6], out var tag)
            || !TopicSyntax.TryGetSuffix(levels, 7, out var suffix))
        {
            return false;
        }

        result = new IotTopic(prefix, direction, tag, levels[3], levels[5], suffix);
        return true;
    }

    public static IotTopic FromIotString(string topic) =>
        TryParse(topic, out var result) ? result : throw new FormatException("Invalid IoT MQTT topic.");

    public override string ToString() =>
        $"{TopicSyntax.Format(Prefix)}/{TopicSyntax.Format(Direction)}/{TypeName}/{Type}/{UriName}/{Uri}/{TopicSyntax.Format(Tag)}{TopicSyntax.FormatSuffix(Suffix)}";
}
