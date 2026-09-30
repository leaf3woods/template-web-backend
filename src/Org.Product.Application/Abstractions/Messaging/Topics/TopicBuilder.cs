namespace Org.Product.Application.Abstractions.Messaging.Topics;

public class TopicBuilder
{
    protected TopicType _prefix = TopicType.Iot;
    protected MqttDirection _direction = MqttDirection.Up;
    protected MqttTag _tag = MqttTag.State;
    protected string? _suffix;

    protected TopicBuilder() { }

    public static TopicBuilder CreateBuilder() => new();

    public virtual TopicBuilder WithPrefix(TopicType prefix)
    {
        _prefix = prefix;
        return this;
    }

    public virtual TopicBuilder WithDirection(MqttDirection direction)
    {
        _direction = direction;
        return this;
    }

    public virtual TopicBuilder WithTag(MqttTag tag)
    {
        _tag = tag;
        return this;
    }

    public virtual TopicBuilder WithSuffix(string suffix)
    {
        ArgumentNullException.ThrowIfNull(suffix);
        _suffix = suffix;
        return this;
    }

    public virtual string Build() => new MqttTopic(_prefix, _direction, _tag, _suffix).ToString();
}
