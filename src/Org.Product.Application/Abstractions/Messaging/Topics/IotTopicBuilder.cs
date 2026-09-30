namespace Org.Product.Application.Abstractions.Messaging.Topics;

public sealed class IotTopicBuilder : TopicBuilder
{
    private string? _deviceType;
    private string? _deviceUri;

    private IotTopicBuilder() { }

    public static IotTopicBuilder CreateIotBuilder() => new();

    public override IotTopicBuilder WithPrefix(TopicType prefix)
    {
        base.WithPrefix(prefix);
        return this;
    }

    public override IotTopicBuilder WithDirection(MqttDirection direction)
    {
        base.WithDirection(direction);
        return this;
    }

    public override IotTopicBuilder WithTag(MqttTag tag)
    {
        base.WithTag(tag);
        return this;
    }

    public override IotTopicBuilder WithSuffix(string suffix)
    {
        base.WithSuffix(suffix);
        return this;
    }

    public IotTopicBuilder WithDeviceType(string deviceType)
    {
        ArgumentNullException.ThrowIfNull(deviceType);
        _deviceType = deviceType;
        return this;
    }

    public IotTopicBuilder WithUri(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        _deviceUri = uri;
        return this;
    }

    public override string Build() =>
        new IotTopic(
            _prefix,
            _direction,
            _tag,
            _deviceType ?? throw new InvalidOperationException("An IoT topic requires a device type."),
            _deviceUri ?? throw new InvalidOperationException("An IoT topic requires a device URI."),
            _suffix
        ).ToString();
}
