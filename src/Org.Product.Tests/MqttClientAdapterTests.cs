using System.Buffers;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MQTTnet;
using MQTTnet.Protocol;
using Org.Product.Application.Abstractions.Messaging;
using Org.Product.Infrastructure.Adapters.Mqtt;
using Org.Product.Infrastructure.Utilities.Options;
using Org.Product.Tests.Support;

namespace Org.Product.Tests;

public sealed class MqttClientAdapterTests
{
    [Fact]
    public async Task Disabled_DoesNotConnectAndRejectsPublish()
    {
        var stub = new MqttClientStub();
        await using var services = new ServiceCollection().BuildServiceProvider();
        using var adapter = CreateAdapter(stub, new MqttOptions(), services);

        await adapter.RunAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.PublishAsync("test/data", new byte[] { 1 }));
        stub.Client.Verify(client => client.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnabledBeforeConnection_PublishDoesNotStartClient()
    {
        var stub = new MqttClientStub();
        await using var services = new ServiceCollection().BuildServiceProvider();
        using var adapter = CreateAdapter(stub, CreateOptions(), services);

        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.PublishAsync("test/data", new byte[] { 1 }));
        stub.Client.Verify(client => client.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Connected_PublishWaitsForResultAndUsesConfiguredDefaults()
    {
        var result = new TaskCompletionSource<MqttClientPublishResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stub = new MqttClientStub { PublishAsync = (_, _) => result.Task };
        var options = CreateOptions();
        options.PublishQualityOfService = MqttQualityOfServiceLevel.ExactlyOnce;
        options.Retain = true;
        await using var services = new ServiceCollection().BuildServiceProvider();
        using var adapter = CreateAdapter(stub, options, services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var publish = adapter.PublishAsync("test/data", new byte[] { 1, 2 });
            Assert.False(publish.IsCompleted);
            result.SetResult(new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, string.Empty, []));
            await publish.WaitAsync(TimeSpan.FromSeconds(5));
            var sent = Assert.Single(stub.Published);
            Assert.Equal("test/data", sent.Topic);
            Assert.Equal(new byte[] { 1, 2 }, sent.Payload.ToArray());
            Assert.Equal(MqttQualityOfServiceLevel.ExactlyOnce, sent.QualityOfServiceLevel);
            Assert.True(sent.Retain);
        }
        finally
        {
            result.TrySetCanceled();
            await StopAsync(stopping, run);
        }
    }

    [Fact]
    public async Task BrokerRejectsPublish_PropagatesFailure()
    {
        var stub = new MqttClientStub
        {
            PublishAsync = (_, _) => Task.FromResult(new MqttClientPublishResult(1, MqttClientPublishReasonCode.NotAuthorized, string.Empty, [])),
        };
        await using var services = new ServiceCollection().BuildServiceProvider();
        using var adapter = CreateAdapter(stub, CreateOptions(), services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.PublishAsync("test/data", new byte[] { 1 }));
        }
        finally
        {
            await StopAsync(stopping, run);
        }
    }

    [Fact]
    public async Task PublishCancellation_ReachesClientAndReleasesOperationGate()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stub = new MqttClientStub
        {
            PublishAsync = async (_, token) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
                return new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, string.Empty, []);
            },
        };
        await using var services = new ServiceCollection().BuildServiceProvider();
        using var adapter = CreateAdapter(stub, CreateOptions(), services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            using var cancellation = new CancellationTokenSource();
            var publish = adapter.PublishAsync("test/data", new byte[] { 1 }, cancellation.Token);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publish);
        }
        finally
        {
            await StopAsync(stopping, run);
        }
    }

    [Fact]
    public async Task Reconnect_RestoresConfiguredSubscriptions()
    {
        var stub = new MqttClientStub();
        var options = CreateOptions();
        options.Subscriptions.Add(new MqttSubscriptionOptions { TopicFilter = "iot/up/+", QualityOfService = MqttQualityOfServiceLevel.ExactlyOnce });
        await using var services = new ServiceCollection()
            .AddScoped<IMqttMessageHandler>(_ => new MqttMessageHandlerStub((_, _) => Task.CompletedTask))
            .BuildServiceProvider();
        using var adapter = CreateAdapter(stub, options, services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await adapter.PublishAsync("test/data", new byte[] { 1 });
            await stub.DisconnectFromBrokerAsync();
            await stub.SecondConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await adapter.PublishAsync("test/data", new byte[] { 2 });
            Assert.Equal(2, stub.Subscriptions.Count);
            foreach (var subscriptions in stub.Subscriptions)
            {
                var filter = Assert.Single(subscriptions.TopicFilters);
                Assert.Equal("iot/up/+", filter.Topic);
                Assert.Equal(MqttQualityOfServiceLevel.ExactlyOnce, filter.QualityOfServiceLevel);
            }
        }
        finally
        {
            await StopAsync(stopping, run);
        }
    }

    [Fact]
    public async Task SubscriptionsWithoutHandlers_FailBeforeConnecting()
    {
        var stub = new MqttClientStub();
        var options = CreateOptions();
        options.Subscriptions.Add(new MqttSubscriptionOptions { TopicFilter = "iot/up/#" });
        await using var services = new ServiceCollection().BuildServiceProvider();
        using var adapter = CreateAdapter(stub, options, services);

        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.RunAsync(CancellationToken.None));
        stub.Client.Verify(client => client.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Messages_UseIndependentScopesAndCopyOnlyPayloadSlice()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlers = new ConcurrentQueue<MqttMessageHandlerStub>();
        var handledCount = 0;
        var stub = new MqttClientStub();
        await using var services = new ServiceCollection().AddScoped<IMqttMessageHandler>(_ =>
        {
            var handler = new MqttMessageHandlerStub(async (_, token) =>
            {
                await release.Task.WaitAsync(token);
                if (Interlocked.Increment(ref handledCount) == 2)
                {
                    handled.TrySetResult();
                }
            });
            handlers.Enqueue(handler);
            return handler;
        }).BuildServiceProvider();
        using var adapter = CreateAdapter(stub, CreateOptions(), services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await adapter.PublishAsync("ready", ReadOnlyMemory<byte>.Empty);
            var bytes = new byte[] { 99, 1, 2, 99 };
            var incoming = new MqttApplicationMessageBuilder().WithTopic("test/data").WithPayload(new ReadOnlySequence<byte>(bytes.AsMemory(1, 2))).Build();
            await stub.DeliverAsync(incoming);
            bytes[1] = 9;
            await stub.DeliverAsync(new MqttApplicationMessageBuilder().WithTopic("test/second").WithPayload("two").Build());
            release.SetResult();
            await handled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            release.TrySetResult();
            await StopAsync(stopping, run);
        }

        var used = handlers.Where(handler => handler.Message is not null).ToArray();
        Assert.Equal(2, used.Length);
        Assert.NotSame(used[0], used[1]);
        Assert.All(used, handler => Assert.True(handler.Disposed));
        Assert.Equal(new byte[] { 1, 2 }, used[0].Message!.Payload.ToArray());
        Assert.Equal(0, stub.AcknowledgementCount);
    }

    [Fact]
    public async Task HandlerCanPublishReply_AcknowledgesOnlyAfterProcessing()
    {
        var stub = new MqttClientStub();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        MqttClientAdapter? adapter = null;
        await using var services = new ServiceCollection().AddScoped<IMqttMessageHandler>(_ => new MqttMessageHandlerStub(async (_, token) =>
        {
            await release.Task.WaitAsync(token);
            await adapter!.PublishAsync("test/reply", new byte[] { 1 }, token);
        })).BuildServiceProvider();
        using var ownedAdapter = CreateAdapter(stub, CreateOptions(), services);
        adapter = ownedAdapter;
        stub.PublishAsync = async (_, token) =>
        {
            await stub.CallbackReturned.Task.WaitAsync(token);
            return new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, string.Empty, []);
        };
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var args = await stub.DeliverAsync(new MqttApplicationMessageBuilder().WithTopic("test/request").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
            Assert.False(args.AutoAcknowledge);
            Assert.Equal(0, stub.AcknowledgementCount);
            release.SetResult();
            await stub.Acknowledged.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal("test/reply", Assert.Single(stub.Published).Topic);
        }
        finally
        {
            release.TrySetResult();
            await StopAsync(stopping, run);
        }
    }

    [Fact]
    public async Task HandlerFailure_DoesNotAcknowledgeAndReconnects()
    {
        var stub = new MqttClientStub();
        await using var services = new ServiceCollection().AddScoped<IMqttMessageHandler>(_ => new MqttMessageHandlerStub((_, _) => throw new InvalidOperationException("test failure"))).BuildServiceProvider();
        using var adapter = CreateAdapter(stub, CreateOptions(), services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var args = await stub.DeliverAsync(new MqttApplicationMessageBuilder().WithTopic("test/request").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
            await stub.SecondConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, stub.AcknowledgementCount);
            Assert.True(args.ProcessingFailed);
        }
        finally
        {
            await StopAsync(stopping, run);
        }
    }

    [Fact]
    public async Task RejectedSubscription_DoesNotBecomeReady()
    {
        var stub = new MqttClientStub { SubscribeResultCode = MqttClientSubscribeResultCode.NotAuthorized };
        var options = CreateOptions();
        options.Subscriptions.Add(new MqttSubscriptionOptions { TopicFilter = "test/#" });
        await using var services = new ServiceCollection().AddScoped<IMqttMessageHandler>(_ => new MqttMessageHandlerStub((_, _) => Task.CompletedTask)).BuildServiceProvider();
        using var adapter = CreateAdapter(stub, options, services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.PublishAsync("test/data", new byte[] { 1 }));
            Assert.Empty(stub.Published);
        }
        finally
        {
            await StopAsync(stopping, run);
        }
    }

    [Fact]
    public async Task ReceiveQueueFull_DoesNotAcknowledgeAndReconnects()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stub = new MqttClientStub();
        var options = CreateOptions();
        options.ReceiveQueueCapacity = 1;
        await using var services = new ServiceCollection().AddScoped<IMqttMessageHandler>(_ => new MqttMessageHandlerStub(async (_, token) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
        })).BuildServiceProvider();
        using var adapter = CreateAdapter(stub, options, services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var incoming = new MqttApplicationMessageBuilder().WithTopic("test/request").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build();
            await stub.DeliverAsync(incoming);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await stub.DeliverAsync(incoming);
            var rejected = await stub.DeliverAsync(incoming);
            Assert.True(rejected.ProcessingFailed);
            await stub.SecondConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, stub.AcknowledgementCount);
        }
        finally
        {
            await StopAsync(stopping, run);
        }
    }

    [Fact]
    public async Task ReconnectDuringProcessing_DoesNotAcknowledgeOldPacketOnNewSession()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new MqttMessageHandlerStub(async (_, token) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(token);
        });
        var stub = new MqttClientStub();
        await using var services = new ServiceCollection().AddScoped<IMqttMessageHandler>(_ => handler).BuildServiceProvider();
        using var adapter = CreateAdapter(stub, CreateOptions(), services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await stub.FirstConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await stub.DeliverAsync(new MqttApplicationMessageBuilder().WithTopic("test/request").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await stub.DisconnectFromBrokerAsync();
            await stub.SecondConnection.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await adapter.PublishAsync("ready", ReadOnlyMemory<byte>.Empty);
            release.SetResult();
            await handler.DisposedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, stub.AcknowledgementCount);
        }
        finally
        {
            release.TrySetResult();
            await StopAsync(stopping, run);
        }
    }

    [Fact]
    public async Task StopDuringConnect_CancelsPendingConnection()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stub = new MqttClientStub();
        stub.Client.Setup(client => client.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .Returns(async (MqttClientOptions _, CancellationToken token) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
                return new MqttClientConnectResult();
            });
        await using var services = new ServiceCollection().BuildServiceProvider();
        using var adapter = CreateAdapter(stub, CreateOptions(), services);
        using var stopping = new CancellationTokenSource();
        var run = adapter.RunAsync(stopping.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await StopAsync(stopping, run);
        }
    }

    private static MqttOptions CreateOptions() => new()
    {
        Enabled = true,
        ClientId = "adapter-tests",
        ReconnectDelaySeconds = 1,
        OperationTimeoutSeconds = 2,
    };

    private static MqttClientAdapter CreateAdapter(MqttClientStub stub, MqttOptions options, IServiceProvider services) =>
        new(stub.Client.Object, Options.Create(options), services.GetRequiredService<IServiceScopeFactory>(), NullLogger<MqttClientAdapter>.Instance);

    private static async Task StopAsync(CancellationTokenSource stopping, Task run)
    {
        await stopping.CancelAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
