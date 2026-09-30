# Optional MQTT integration

WebApi has an optional MQTTnet 5 client. It is disabled by default and does not connect to a broker until enabled. Application depends on `IMqttPublisher`, `IMqttMessageHandler`, and `MqttMessage`; only Infrastructure depends on MQTTnet.

`AddMqtt()` uses the host options and hosted-service APIs. WebApi automatically discovers `MqttModule`, which registers the third-party client factory and scans Application implementations of `IMqttMessageHandler` as scoped services. The existing `IApplicationAbstraction` scan registers `MqttClientAdapter` as one singleton exposed through both `IMqttPublisher` and the Infrastructure lifecycle interface `IMqttConnection`. `MqttWorker` owns the process lifecycle. DbMigrator does not register or start MQTT.

## Configuration

Enable publishing with a unique client ID for each process instance. Host and client ID are required when enabled. Credentials are optional for brokers permitting anonymous connections; supply credentials through environment variables or user secrets.

```json
{
  "Mqtt": {
    "Enabled": true,
    "Host": "localhost",
    "Port": 1883,
    "ClientId": "Org.Product.WebApi-01",
    "ProtocolVersion": "V311",
    "UseTls": false,
    "KeepAliveSeconds": 30,
    "ReconnectDelaySeconds": 10,
    "OperationTimeoutSeconds": 10,
    "ReceiveQueueCapacity": 256,
    "PublishQualityOfService": 1,
    "Retain": false,
    "Subscriptions": []
  }
}
```

`V311` explicitly selects MQTT 3.1.1; `V500` selects MQTT 5.0. This overrides MQTTnet 5's default protocol choice. Set `UseTls` and the broker's TLS port to use platform certificate validation. The supplied Compose broker listens on plain TCP 1883; its port mapping does not enable TLS.

`Mqtt__Username`, `Mqtt__Password`, and `Mqtt__ClientId` are environment-variable examples. `PublishQualityOfService` and subscription `QualityOfService` accept 0, 1, or 2. Reconnect delay must be 1–86400 seconds, operation timeout 1–3600 seconds, keep-alive 1–65535 seconds, and queue capacity positive. Enabled configuration is validated on host startup; disabled MQTT does not require valid broker settings.

## Publishing

Inject `IMqttPublisher` into an application use case and call:

```csharp
await mqttPublisher.PublishAsync(topic, payload, cancellationToken);
```

`topic` is a published topic name, and `payload` is `ReadOnlyMemory<byte>`. See [topic conventions and builders](mqtt-topics.md) for constructing project-specific names. QoS and retain use the configured defaults. Publishing neither starts nor reconnects the client. Disabled MQTT and a client that is not yet connected/subscribed raise `InvalidOperationException`; broker rejection, timeout, and caller cancellation are propagated.

Completion means MQTTnet's publish operation completed successfully. QoS 0 completes without a broker acknowledgement; QoS 1/2 wait for the applicable protocol exchange. This does not prove a device handled the message or a downstream business transaction committed. There is no disconnected publish buffer or database outbox.

## Receiving

Add subscription filters to configuration and implement at least one `IMqttMessageHandler` in Application. For example, an echo handler can demonstrate request/reply without introducing a domain dependency:

```csharp
using Org.Product.Application.Abstractions.Messaging;

namespace Org.Product.Application.Messaging;

public sealed class MqttEchoHandler : IMqttMessageHandler
{
    private readonly IMqttPublisher _publisher;

    public MqttEchoHandler(IMqttPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task HandleAsync(MqttMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Topic != "template/request")
        {
            return;
        }

        await _publisher.PublishAsync("template/reply", message.Payload, cancellationToken);
    }
}
```

Configure its subscription:

```json
"Subscriptions": [
  {
    "TopicFilter": "template/request",
    "QualityOfService": 1
  }
]
```

The snippet is an extension example, not a built-in echo use case. Application handlers are discovered automatically; they must not inherit the singleton marker `IApplicationAbstraction`. They may inject scoped repositories and unit-of-work contracts. Each mutation use case owns its commit, as with HTTP use cases. All registered handlers are invoked sequentially for each message and must select the topics they handle; their relative order is not a business contract. Configured subscriptions without a registered handler fail before connecting.

Filters support `+` as an entire level and `#` as the final entire level. Duplicate filters, malformed wildcards, null characters, invalid UTF-16, and encodings exceeding 65535 UTF-8 bytes fail validation. Empty levels in a filter remain valid MQTT syntax. Subscription filters are separate from the published-topic builders. Configuration is fixed for the process lifetime; restart to change subscriptions. There is no dynamic subscription-management interface yet.

## Lifecycle and delivery boundaries

The worker connects and establishes configured subscriptions before publishing becomes ready. It checks CONNACK and SUBACK results, retries failed connection/subscription attempts after the configured delay, and re-subscribes after disconnection. Connections use a clean session/start with no retained session state; this implementation does not provide offline subscription persistence.

The receive callback copies the actual payload sequence into a bounded in-memory queue and returns without waiting for application processing. One consumer creates a fresh asynchronous DI scope per message. This lets handlers publish replies without blocking the protocol's incoming acknowledgement processing. Publish, connect, disconnect, subscription, and manual acknowledgement operations are serialized to coordinate lifecycle changes.

QoS 1/2 messages are acknowledged after all handlers complete successfully. Handler failure or queue overflow is logged, leaves the message unacknowledged, and requests a reconnect. A packet from an earlier connection is never acknowledged on a new connection. Queue overflow can lose messages, and clean sessions do not guarantee redelivery of failed/unacknowledged messages after reconnect. QoS 0 has no acknowledgement or redelivery guarantee. The queue is not durable; use appropriate persistent sessions, idempotency, and durable inbox/outbox storage if your use case requires reliable business delivery. A handler's side effects and MQTT acknowledgement are not atomic.

Host shutdown cancels connection/retry/message work and attempts a disconnect within the operation timeout. Handlers must honor their cancellation token. Credentials and message payloads are not logged; message-handling errors include the topic for diagnosis.

## Validation

```powershell
dotnet test Org.Product.sln --filter FullyQualifiedName~Mqtt
```

Tests cover disabled startup, shared singleton registration, configuration and filter validation, subscription restoration/rejection, publish completion/failure/cancellation, independent message scopes, payload slicing, replies and acknowledgement order, queue overflow, stale packet identifiers, and cancellation during connection. `MqttIntegrationTests` use the test-only `MQTTnet.Server` package to start temporary brokers bound to loopback and exercise request/reply under MQTT 3.1.1 and 5.0 with QoS 1 and 2. They do not use the configured broker, PostgreSQL, Redis, or Docker.
