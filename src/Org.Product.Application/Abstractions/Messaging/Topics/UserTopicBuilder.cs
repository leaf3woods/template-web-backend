namespace Org.Product.Application.Abstractions.Messaging.Topics;

public sealed class UserTopicBuilder : TopicBuilder
{
    private string? _uri;
    private string? _userTag;

    private UserTopicBuilder() { }

    public static UserTopicBuilder CreateUserBuilder() => new();

    public override UserTopicBuilder WithPrefix(TopicType prefix)
    {
        base.WithPrefix(prefix);
        return this;
    }

    public override UserTopicBuilder WithDirection(MqttDirection direction)
    {
        base.WithDirection(direction);
        return this;
    }

    public override UserTopicBuilder WithTag(MqttTag tag)
    {
        base.WithTag(tag);
        return this;
    }

    public override UserTopicBuilder WithSuffix(string suffix)
    {
        base.WithSuffix(suffix);
        return this;
    }

    public UserTopicBuilder WithUri(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        _uri = uri;
        return this;
    }

    public UserTopicBuilder WithUserTag(string userTag)
    {
        ArgumentNullException.ThrowIfNull(userTag);
        _userTag = userTag;
        return this;
    }

    public override string Build() =>
        new UserTopic(_prefix, _direction, _tag, _uri, _userTag, _suffix).ToString();
}
