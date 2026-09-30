using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace Org.Product.Infrastructure.Utilities.Options;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseTls { get; set; }
    public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;
    public int KeepAliveSeconds { get; set; } = 30;
    public int ReconnectDelaySeconds { get; set; } = 10;
    public int OperationTimeoutSeconds { get; set; } = 10;
    public int ReceiveQueueCapacity { get; set; } = 256;
    public MqttQualityOfServiceLevel PublishQualityOfService { get; set; } = MqttQualityOfServiceLevel.AtLeastOnce;
    public bool Retain { get; set; }
    public List<MqttSubscriptionOptions> Subscriptions { get; set; } = [];
}

public sealed class MqttSubscriptionOptions
{
    public string TopicFilter { get; set; } = string.Empty;
    public MqttQualityOfServiceLevel QualityOfService { get; set; } = MqttQualityOfServiceLevel.AtLeastOnce;
}
