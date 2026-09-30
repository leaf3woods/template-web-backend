using Org.Product.Application.Abstractions.Messaging.Topics;

namespace Org.Product.Tests;

public sealed class MqttTopicTests
{
    public static TheoryData<MqttTag> Tags => new(Enum.GetValues<MqttTag>());

    [Theory]
    [InlineData(TopicType.Sys, MqttDirection.Up, MqttTag.State, null, "sys/up/state")]
    [InlineData(TopicType.App, MqttDirection.Down, MqttTag.Notice, "batch/0001", "app/down/notice/batch/0001")]
    public void BasicTopic_BuildsAndParsesKnownPaths(TopicType prefix, MqttDirection direction, MqttTag tag, string? suffix, string expected)
    {
        var builder = TopicBuilder.CreateBuilder()
            .WithPrefix(prefix)
            .WithDirection(direction)
            .WithTag(tag);
        if (suffix is not null)
        {
            builder.WithSuffix(suffix);
        }

        Assert.Equal(expected, builder.Build());
        var parsed = MqttTopic.FromString(expected);
        Assert.Equal(prefix, parsed.Prefix);
        Assert.Equal(direction, parsed.Direction);
        Assert.Equal(tag, parsed.Tag);
        Assert.Equal(suffix, parsed.Suffix);
        Assert.Equal(expected, parsed.ToString());
    }

    [Fact]
    public void BuilderDefaults_AreExplicitAndParseable()
    {
        Assert.Equal("iot/up/state", TopicBuilder.CreateBuilder().Build());
        Assert.Equal("/iot/up/state", UserTopicBuilder.CreateUserBuilder().Build());
        Assert.True(MqttTopic.TryParse("iot/up/state", out _));
        Assert.True(UserTopic.TryParse("/iot/up/state", out var _));
    }

    [Fact]
    public void IotTopic_PreservesPathAndFluentCallsAfterSuffix()
    {
        var topic = IotTopicBuilder.CreateIotBuilder()
            .WithSuffix("batch/0001")
            .WithPrefix(TopicType.Iot)
            .WithDirection(MqttDirection.Down)
            .WithTag(MqttTag.Cmd)
            .WithDeviceType("rfidreader")
            .WithUri("connection-1")
            .Build();

        Assert.Equal("iot/down/type/rfidreader/uri/connection-1/cmd/batch/0001", topic);
        var parsed = IotTopic.FromIotString(topic);
        Assert.Equal("rfidreader", parsed.Type);
        Assert.Equal("connection-1", parsed.Uri);
        Assert.Equal("batch/0001", parsed.Suffix);
        Assert.Equal(topic, parsed.ToString());
    }

    [Theory]
    [InlineData(null, null, "/app/up/notice")]
    [InlineData(null, "device-1", "/app/up/user/device-1/notice")]
    [InlineData("client-1", null, "client-1/app/up/notice")]
    [InlineData("client-1", "device-1", "client-1/app/up/user/device-1/notice")]
    public void UserTopic_RoundTripsWithAndWithoutTargetAndUserTag(string? userTag, string? userCode, string expected)
    {
        var builder = UserTopicBuilder.CreateUserBuilder()
            .WithPrefix(TopicType.App)
            .WithTag(MqttTag.Notice);
        if (userTag is not null)
        {
            builder.WithUserTag(userTag);
        }

        if (userCode is not null)
        {
            builder.WithUri(userCode);
        }

        Assert.Equal(expected, builder.Build());
        var parsed = UserTopic.FromUserString(expected);
        Assert.Equal(TopicType.App, parsed.Prefix);
        Assert.Equal(MqttDirection.Up, parsed.Direction);
        Assert.Equal(userTag, parsed.UserTag);
        Assert.Equal(userCode, parsed.UserCode);
        Assert.Equal(expected, parsed.ToString());
    }

    [Fact]
    public void UserTopic_WithSuffixRetainsTargetAndFluentCalls()
    {
        var topic = UserTopicBuilder.CreateUserBuilder()
            .WithSuffix("part/01")
            .WithUserTag("client-1")
            .WithUri("device-1")
            .WithTag(MqttTag.File)
            .Build();

        Assert.Equal("client-1/iot/up/user/device-1/file/part/01", topic);
        var parsed = UserTopic.FromUserString(topic);
        Assert.Equal("device-1", parsed.UserCode);
        Assert.Equal("client-1", parsed.UserTag);
        Assert.Equal("part/01", parsed.Suffix);
    }

    [Theory]
    [MemberData(nameof(Tags))]
    public void SupportedTags_RoundTripAcrossAllTopicShapes(MqttTag tag)
    {
        var basic = new MqttTopic(TopicType.Sys, MqttDirection.Down, tag);
        var iot = new IotTopic(TopicType.App, MqttDirection.Up, tag, "device", "connection-1");
        var user = new UserTopic(TopicType.App, MqttDirection.Up, tag, "target-1", "client-1");

        Assert.Equal(basic, MqttTopic.FromString(basic.ToString()));
        Assert.Equal(iot, IotTopic.FromIotString(iot.ToString()));
        Assert.Equal(user, UserTopic.FromUserString(user.ToString()));
    }

    [Fact]
    public void DynamicIdentifiers_PreserveCaseUnicodeAndSpaces()
    {
        var iot = IotTopicBuilder.CreateIotBuilder()
            .WithDeviceType("RfidReader")
            .WithUri("Device A")
            .WithSuffix("批次/ABC")
            .Build();
        var user = UserTopicBuilder.CreateUserBuilder()
            .WithUserTag("ClientA")
            .WithUri("MesResult")
            .Build();

        Assert.Equal("iot/up/type/RfidReader/uri/Device A/state/批次/ABC", iot);
        Assert.Equal(iot, IotTopic.FromIotString(iot).ToString());
        Assert.Equal("ClientA/iot/up/user/MesResult/state", user);
        Assert.Equal(user, UserTopic.FromUserString(user).ToString());
    }

    [Fact]
    public void IotTopic_ChangingDirectionRequiresANewValue()
    {
        var original = IotTopic.FromIotString("iot/up/type/device/uri/connection-1/calibration");
        var reply = new IotTopic(original.Prefix, MqttDirection.Down, original.Tag, original.Type, original.Uri, original.Suffix);

        Assert.Equal(MqttDirection.Up, original.Direction);
        Assert.Equal("iot/down/type/device/uri/connection-1/calibration", reply.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("iot")]
    [InlineData("iot/up")]
    [InlineData("unknown/up/data")]
    [InlineData("0/up/data")]
    [InlineData("iot/0/data")]
    [InlineData("iot/up/0")]
    [InlineData("Iot/up/data")]
    [InlineData("iot/up/data/")]
    [InlineData("iot/up/data/a//b")]
    [InlineData("iot/up/data/+")]
    [InlineData("iot/up/data/#")]
    [InlineData("iot/up/data/\0")]
    public void BasicTopic_InvalidInputReturnsFalse(string? topic)
    {
        Assert.False(MqttTopic.TryParse(topic, out var result));
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("iot/up/type")]
    [InlineData("iot/up/type/device/uri/id")]
    [InlineData("iot/up/device/device/uri/id/data")]
    [InlineData("iot/up/type/device/id/id/data")]
    [InlineData("iot/up/type//uri/id/data")]
    [InlineData("iot/up/type/device/uri//data")]
    [InlineData("iot/up/type/device/uri/id/unknown")]
    [InlineData("iot/up/type/device/uri/id/data/")]
    [InlineData("iot/up/type/device/uri/+/data")]
    public void IotTopic_InvalidInputReturnsFalse(string? topic)
    {
        Assert.False(IotTopic.TryParse(topic, out var result));
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/app/up")]
    [InlineData("client/app/up/user/id")]
    [InlineData("client/app/up/user//data")]
    [InlineData("client/app/up/user/id/unknown")]
    [InlineData("client/app/up/user/id/data/")]
    [InlineData("client/app/up/user/id/data/a//b")]
    [InlineData("client/app/up/user/+/data")]
    [InlineData("client/app/up/unknown")]
    [InlineData("client/App/up/data")]
    public void UserTopic_InvalidInputReturnsFalse(string? topic)
    {
        Assert.False(UserTopic.TryParse(topic, out var result));
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a/b")]
    [InlineData("+")]
    [InlineData("#")]
    [InlineData("a\0b")]
    public void DynamicLevel_InvalidValuesAreRejected(string level)
    {
        Assert.Throws<ArgumentException>(() => IotTopicBuilder.CreateIotBuilder().WithDeviceType(level).WithUri("id").Build());
        Assert.Throws<ArgumentException>(() => IotTopicBuilder.CreateIotBuilder().WithDeviceType("device").WithUri(level).Build());
        Assert.Throws<ArgumentException>(() => UserTopicBuilder.CreateUserBuilder().WithUri(level).Build());
        if (level.Length != 0)
        {
            Assert.Throws<ArgumentException>(() => UserTopicBuilder.CreateUserBuilder().WithUserTag(level).Build());
        }
    }

    [Fact]
    public void IotBuilder_MissingDeviceFieldsAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() => IotTopicBuilder.CreateIotBuilder().Build());
        Assert.Throws<InvalidOperationException>(() => IotTopicBuilder.CreateIotBuilder().WithDeviceType("device").Build());
        Assert.Throws<InvalidOperationException>(() => IotTopicBuilder.CreateIotBuilder().WithUri("id").Build());
    }

    [Fact]
    public void InvalidUtf16_IsRejectedWithoutChangingTheIdentifier()
    {
        // Attribute string serialization replaces isolated surrogates; create one at runtime.
        var invalid = new string('\uD800', 1);
        Assert.False(MqttTopic.TryParse($"iot/up/data/{invalid}", out _));
        Assert.False(IotTopic.TryParse($"iot/up/type/{invalid}/uri/id/data", out var _));
        Assert.False(UserTopic.TryParse($"/app/up/user/{invalid}/data", out var _));
        Assert.Throws<ArgumentException>(() => IotTopicBuilder.CreateIotBuilder().WithDeviceType(invalid).WithUri("id").Build());
    }

    [Theory]
    [InlineData("")]
    [InlineData("/part")]
    [InlineData("part/")]
    [InlineData("part//01")]
    [InlineData("part/+")]
    [InlineData("part/#")]
    public void Suffix_InvalidPathLevelsAreRejected(string suffix)
    {
        Assert.Throws<ArgumentException>(() => TopicBuilder.CreateBuilder().WithSuffix(suffix).Build());
        Assert.Throws<ArgumentException>(() => IotTopicBuilder.CreateIotBuilder().WithDeviceType("device").WithUri("id").WithSuffix(suffix).Build());
        Assert.Throws<ArgumentException>(() => UserTopicBuilder.CreateUserBuilder().WithSuffix(suffix).Build());
    }

    [Fact]
    public void UnknownEnumValues_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TopicBuilder.CreateBuilder().WithPrefix((TopicType)99).Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => TopicBuilder.CreateBuilder().WithDirection((MqttDirection)99).Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => TopicBuilder.CreateBuilder().WithTag((MqttTag)99).Build());
    }

    [Fact]
    public void TopicLength_IsMeasuredInUtf8BytesIncludingStructuralLevels()
    {
        var suffix = new string('a', ushort.MaxValue - "iot/up/data/".Length);
        var topic = TopicBuilder.CreateBuilder().WithTag(MqttTag.Data).WithSuffix(suffix).Build();
        Assert.True(MqttTopic.TryParse(topic, out _));
        Assert.Throws<ArgumentException>(() => TopicBuilder.CreateBuilder().WithTag(MqttTag.Data).WithSuffix(suffix + "a").Build());

        var unicodeSuffix = new string('中', 22000);
        Assert.Throws<ArgumentException>(() => TopicBuilder.CreateBuilder().WithSuffix(unicodeSuffix).Build());
        Assert.False(MqttTopic.TryParse($"iot/up/state/{unicodeSuffix}", out _));

        var oversizedIdentifier = new string('a', ushort.MaxValue);
        Assert.Throws<ArgumentException>(() => IotTopicBuilder.CreateIotBuilder().WithDeviceType("device").WithUri(oversizedIdentifier).Build());
        Assert.Throws<ArgumentException>(() => UserTopicBuilder.CreateUserBuilder().WithUri(oversizedIdentifier).Build());
        Assert.False(IotTopic.TryParse($"iot/up/type/device/uri/{oversizedIdentifier}/data", out var _));
        Assert.False(UserTopic.TryParse($"/app/up/user/{oversizedIdentifier}/data", out var _));
    }

    [Fact]
    public void ThrowingParsers_InvalidInputProducesFormatException()
    {
        Assert.Throws<FormatException>(() => MqttTopic.FromString("invalid"));
        Assert.Throws<FormatException>(() => IotTopic.FromIotString("invalid"));
        Assert.Throws<FormatException>(() => UserTopic.FromUserString("invalid"));
    }
}
