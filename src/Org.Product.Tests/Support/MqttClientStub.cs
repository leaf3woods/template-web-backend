using System.Collections.Concurrent;
using Moq;
using MQTTnet;
using MQTTnet.Packets;

namespace Org.Product.Tests.Support;

public sealed class MqttClientStub
{
    private Func<MqttApplicationMessageReceivedEventArgs, Task>? _received;
    private Func<MqttClientDisconnectedEventArgs, Task>? _disconnected;
    private int _connectCount;
    private int _acknowledgementCount;

    public MqttClientStub()
    {
        Client.SetupGet(client => client.IsConnected).Returns(() => IsConnected);
        Client.SetupAdd(client => client.ApplicationMessageReceivedAsync += It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>())
            .Callback<Func<MqttApplicationMessageReceivedEventArgs, Task>>(handler => _received += handler);
        Client.SetupRemove(client => client.ApplicationMessageReceivedAsync -= It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>())
            .Callback<Func<MqttApplicationMessageReceivedEventArgs, Task>>(handler => _received -= handler);
        Client.SetupAdd(client => client.DisconnectedAsync += It.IsAny<Func<MqttClientDisconnectedEventArgs, Task>>())
            .Callback<Func<MqttClientDisconnectedEventArgs, Task>>(handler => _disconnected += handler);
        Client.SetupRemove(client => client.DisconnectedAsync -= It.IsAny<Func<MqttClientDisconnectedEventArgs, Task>>())
            .Callback<Func<MqttClientDisconnectedEventArgs, Task>>(handler => _disconnected -= handler);
        Client.Setup(client => client.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .Returns((MqttClientOptions options, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                ConnectionOptions = options;
                IsConnected = ConnectResultCode == MqttClientConnectResultCode.Success;
                if (Interlocked.Increment(ref _connectCount) == 1)
                {
                    FirstConnection.TrySetResult();
                }
                else
                {
                    SecondConnection.TrySetResult();
                }

                return Task.FromResult(new MqttClientConnectResult { ResultCode = ConnectResultCode });
            });
        Client.Setup(client => client.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .Returns((MqttClientSubscribeOptions options, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                Subscriptions.Enqueue(options);
                var items = options.TopicFilters.Select(filter => new MqttClientSubscribeResultItem(filter,
                    SubscribeResultCode ?? (MqttClientSubscribeResultCode)filter.QualityOfServiceLevel)).ToArray();
                return Task.FromResult(new MqttClientSubscribeResult(1, items, string.Empty, []));
            });
        Client.Setup(client => client.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Returns((MqttApplicationMessage message, CancellationToken token) =>
            {
                Published.Enqueue(message);
                return PublishAsync is not null
                    ? PublishAsync(message, token)
                    : Task.FromResult(new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, string.Empty, []));
            });
        Client.Setup(client => client.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), It.IsAny<CancellationToken>()))
            .Returns((MqttClientDisconnectOptions _, CancellationToken _) => DisconnectFromBrokerAsync());
    }

    public Mock<IMqttClient> Client { get; } = new();
    public bool IsConnected { get; private set; }
    public MqttClientOptions? ConnectionOptions { get; private set; }
    public MqttClientConnectResultCode ConnectResultCode { get; set; } = MqttClientConnectResultCode.Success;
    public MqttClientSubscribeResultCode? SubscribeResultCode { get; set; }
    public Func<MqttApplicationMessage, CancellationToken, Task<MqttClientPublishResult>>? PublishAsync { get; set; }
    public ConcurrentQueue<MqttClientSubscribeOptions> Subscriptions { get; } = new();
    public ConcurrentQueue<MqttApplicationMessage> Published { get; } = new();
    public TaskCompletionSource FirstConnection { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource SecondConnection { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource CallbackReturned { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Acknowledged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int AcknowledgementCount => Volatile.Read(ref _acknowledgementCount);

    public async Task DisconnectFromBrokerAsync()
    {
        IsConnected = false;
        if (_disconnected is not null)
        {
            await _disconnected(new MqttClientDisconnectedEventArgs(true, new MqttClientConnectResult(), MqttClientDisconnectReason.UnspecifiedError, string.Empty, [], null));
        }
    }

    public async Task<MqttApplicationMessageReceivedEventArgs> DeliverAsync(MqttApplicationMessage message)
    {
        var args = new MqttApplicationMessageReceivedEventArgs("test", message, new MqttPublishPacket { PacketIdentifier = 1 }, (_, token) =>
        {
            token.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _acknowledgementCount);
            Acknowledged.TrySetResult();
            return Task.CompletedTask;
        });
        if (_received is not null)
        {
            await _received(args);
        }

        CallbackReturned.TrySetResult();
        return args;
    }
}
