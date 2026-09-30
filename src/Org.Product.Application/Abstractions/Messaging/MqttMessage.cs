namespace Org.Product.Application.Abstractions.Messaging;

public sealed class MqttMessage
{
    public MqttMessage(string topic, ReadOnlyMemory<byte> payload, bool retain = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);
        Topic = topic;
        Payload = payload;
        Retain = retain;
    }

    public string Topic { get; }
    public ReadOnlyMemory<byte> Payload { get; }
    public bool Retain { get; }
}
