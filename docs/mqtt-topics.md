# MQTT topic conventions

The pure topic types and fluent builders live in `Org.Product.Application/Abstractions/Messaging/Topics`. They define application message naming without referencing MQTTnet, a database, or MES device enums. Builders are created per use and require no dependency-injection registration.

These types implement topic construction and parsing. The optional [MQTT integration](mqtt.md) supplies the client, publisher, configuration subscriptions, and hosted worker; it is disabled by default. The Compose broker remains a deployment example and must be started/configured separately.

## Topic shapes

| Type | Path |
| --- | --- |
| `MqttTopic` | `{prefix}/{direction}/{tag}` |
| `IotTopic` | `{prefix}/{direction}/type/{deviceType}/uri/{connectionId}/{tag}` |
| `UserTopic` with a target | `{userTag}/{prefix}/{direction}/user/{userCode}/{tag}` |
| `UserTopic` without a target | `{userTag}/{prefix}/{direction}/{tag}` |

All shapes optionally append `/{suffix}`. A suffix may contain multiple non-empty levels such as `batch/0001`. Prefix defaults to `Iot`, direction to `Up`, and tag to `State`, preserving the original builder defaults.

An omitted or empty `UserTag` keeps the original leading slash: `/app/up/user/device-1/notice`. A leading slash changes the MQTT topic and must not be trimmed. A user target is optional; an IoT device type and URI are required. `WithUri` continues to set `UserCode` on `UserTopicBuilder` for compatibility with the original fluent API.

## Construction

```csharp
using Org.Product.Application.Abstractions.Messaging.Topics;

var systemTopic = TopicBuilder.CreateBuilder()
    .WithPrefix(TopicType.Sys)
    .WithTag(MqttTag.State)
    .Build();
// sys/up/state

var deviceTopic = IotTopicBuilder.CreateIotBuilder()
    .WithPrefix(TopicType.Iot)
    .WithDirection(MqttDirection.Down)
    .WithDeviceType("rfidreader")
    .WithUri("connection-1")
    .WithTag(MqttTag.Cmd)
    .WithSuffix("batch/0001")
    .Build();
// iot/down/type/rfidreader/uri/connection-1/cmd/batch/0001

var notificationTopic = UserTopicBuilder.CreateUserBuilder()
    .WithPrefix(TopicType.App)
    .WithTag(MqttTag.Notice)
    .WithUri("device-1")
    .Build();
// /app/up/user/device-1/notice
```

`Build()` returns a string. The topic records expose getter-only properties and validate their constructor arguments. Builders remain mutable; reuse them only within one construction flow, not across concurrent callers.

## Parsing and replies

Select the parser for the expected shape: `MqttTopic.TryParse`, `IotTopic.TryParse`, or `UserTopic.TryParse`. Each returns `false` and a null result for invalid input. The throwing counterparts retain the original names: `FromString`, `FromIotString`, and `FromUserString`; invalid input raises `FormatException`.

```csharp
if (IotTopic.TryParse(deviceTopic, out IotTopic? received))
{
    var reply = new IotTopic(
        received.Prefix,
        MqttDirection.Up,
        received.Tag,
        received.Type,
        received.Uri,
        received.Suffix);

    var replyTopic = reply.ToString();
}
```

Creating a reply does not mutate the received topic. `IotTopic.Type` remains a string; mapping it to a device enum or choosing a business handler belongs to the consuming use case.

## Validation and migration differences

- Prefix, direction, and tag use lowercase enum names. The `type`, `uri`, and `user` markers are fixed lowercase segments. Parsers validate these exact spellings and reject numeric enum values.
- Dynamic identifiers and suffixes preserve their case, Unicode, and spaces. The old implementation lowercased the entire topic. When communicating with an existing consumer that expects lowercase identifiers, supply those established lowercase values explicitly; `MesResult` and `mesresult` are different topics.
- Device type, device URI, user target, and a non-empty user tag occupy one level each, so they cannot contain `/`, `+`, `#`, or a null character. Explicitly empty device identifiers or user targets are rejected. Suffix levels must also be non-empty.
- Topic names reject invalid UTF-16 input, null characters, wildcards, and total UTF-8 encodings larger than 65535 bytes. Length validation includes the structural segments.
- `WithSuffix` now participates in construction and parsing on every builder. The original base builder ignored it.
- The user parser now reads the user-tag prefix and target from their actual positions. The IoT parser checks level counts and fixed markers before accessing identifiers. All declared tags can be built and parsed.
- IoT construction no longer falls back to a short basic topic when its URI is omitted; use `TopicBuilder` for that shape. Mutable object initializers and in-place direction changes are replaced by validated constructors.

These are application topic conventions, not restrictions on every MQTT topic. MQTT permits empty levels, but these builders permit one only for the established missing-user-tag prefix. MQTT subscription filters have different rules: `+` and `#` can be used in filters, but not in published topic names. Keep filter validation separate; a subscription filter must not be built by putting wildcards into these identifiers. See the [MQTT 3.1.1 specification, section 4.7](https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html).

## Verification

```powershell
dotnet test Org.Product.sln --filter FullyQualifiedName~MqttTopicTests
```

The tests cover known paths, omitted user tags, all supported tags, suffixes, preserved identifiers, malformed input, missing device fields, and UTF-8 length limits. They do not require PostgreSQL, Redis, or an MQTT broker.
