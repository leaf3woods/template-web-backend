using System.Buffers;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using Org.Product.Application.Abstractions.Messaging;
using Org.Product.Infrastructure.Utilities.Options;

namespace Org.Product.Infrastructure.Adapters.Mqtt;

public sealed class MqttClientAdapter : IMqttPublisher, IMqttConnection, IDisposable
{
    private readonly IMqttClient _client;
    private readonly MqttOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttClientAdapter> _logger;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private Channel<ReceivedMessage>? _receiveQueue;
    private TaskCompletionSource _reconnectSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _running;
    private int _ready;
    private int _sessionId;

    public MqttClientAdapter(IMqttClient client, IOptions<MqttOptions> options, IServiceScopeFactory scopeFactory, ILogger<MqttClientAdapter> logger)
    {
        _client = client;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _client.ApplicationMessageReceivedAsync += ReceiveAsync;
        _client.DisconnectedAsync += DisconnectedAsync;
    }

    public async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("MQTT is disabled.");
        }

        MqttTopicValidator.ThrowIfInvalid(topic);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.OperationTimeoutSeconds));
        await _operationGate.WaitAsync(timeout.Token);
        try
        {
            if (Volatile.Read(ref _ready) == 0 || !_client.IsConnected)
            {
                throw new InvalidOperationException("MQTT is not connected and ready.");
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(new ReadOnlySequence<byte>(payload))
                .WithQualityOfServiceLevel(_options.PublishQualityOfService)
                .WithRetainFlag(_options.Retain)
                .Build();
            var result = await _client.PublishAsync(message, timeout.Token);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"MQTT publish was rejected: {result.ReasonCode}.");
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException("The MQTT connection is already running.");
        }

        using var receiverCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        Task? receiver = null;
        try
        {
            if (_options.Subscriptions.Count > 0)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                if (!scope.ServiceProvider.GetServices<IMqttMessageHandler>().Any())
                {
                    throw new InvalidOperationException("MQTT subscriptions require at least one IMqttMessageHandler.");
                }
            }

            var queue = Channel.CreateBounded<ReceivedMessage>(new BoundedChannelOptions(_options.ReceiveQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait,
            });
            _receiveQueue = queue;
            receiver = ProcessMessagesAsync(queue.Reader, receiverCancellation.Token);

            while (!stoppingToken.IsCancellationRequested)
            {
                _reconnectSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    await ConnectAsync(stoppingToken);
                    await _reconnectSignal.Task.WaitAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "MQTT connection or subscription failed; retrying.");
                }
                finally
                {
                    await DisconnectAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            _receiveQueue?.Writer.TryComplete();
            await receiverCancellation.CancelAsync();
            if (receiver is not null)
            {
                await receiver;
            }

            _receiveQueue = null;
            Volatile.Write(ref _running, 0);
        }
    }

    private async Task ConnectAsync(CancellationToken stoppingToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.OperationTimeoutSeconds));
        await _operationGate.WaitAsync(timeout.Token);
        try
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync(new MqttClientDisconnectOptions(), timeout.Token);
            }

            Interlocked.Increment(ref _sessionId);
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_options.Host, _options.Port)
                .WithClientId(_options.ClientId)
                .WithProtocolVersion(_options.ProtocolVersion)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_options.KeepAliveSeconds))
                .WithTimeout(TimeSpan.FromSeconds(_options.OperationTimeoutSeconds));
            if (_options.Username is not null)
            {
                builder.WithCredentials(_options.Username, _options.Password);
            }

            if (_options.UseTls)
            {
                builder.WithTlsOptions(tls => tls.UseTls());
            }

            var result = await _client.ConnectAsync(builder.Build(), timeout.Token);
            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new InvalidOperationException($"MQTT connection was rejected: {result.ResultCode}.");
            }

            if (_options.Subscriptions.Count > 0)
            {
                var subscriptions = new MqttClientSubscribeOptionsBuilder();
                foreach (var subscription in _options.Subscriptions)
                {
                    subscriptions.WithTopicFilter(filter => filter
                        .WithTopic(subscription.TopicFilter)
                        .WithQualityOfServiceLevel(subscription.QualityOfService));
                }

                var subscribed = await _client.SubscribeAsync(subscriptions.Build(), timeout.Token);
                if (subscribed.Items.Count != _options.Subscriptions.Count
                    || subscribed.Items.Any(item => item.ResultCode is not (MqttClientSubscribeResultCode.GrantedQoS0 or MqttClientSubscribeResultCode.GrantedQoS1 or MqttClientSubscribeResultCode.GrantedQoS2)))
                {
                    throw new InvalidOperationException("MQTT broker rejected a configured subscription.");
                }
            }

            Volatile.Write(ref _ready, 1);
            _logger.LogInformation("MQTT connected; {SubscriptionCount} subscriptions established.", _options.Subscriptions.Count);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task DisconnectAsync()
    {
        Volatile.Write(ref _ready, 0);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.OperationTimeoutSeconds));
        try
        {
            await _operationGate.WaitAsync(timeout.Token);
            try
            {
                if (_client.IsConnected)
                {
                    await _client.DisconnectAsync(new MqttClientDisconnectOptions(), timeout.Token);
                }
            }
            finally
            {
                _operationGate.Release();
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "MQTT disconnect failed.");
        }
    }

    private Task DisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        Volatile.Write(ref _ready, 0);
        _reconnectSignal.TrySetResult();
        return Task.CompletedTask;
    }

    private Task ReceiveAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        args.AutoAcknowledge = false;
        var message = new MqttMessage(args.ApplicationMessage.Topic, args.ApplicationMessage.Payload.ToArray(), args.ApplicationMessage.Retain);
        // Never wait on the network callback: handlers may publish replies requiring incoming ACKs.
        if (_receiveQueue is null || !_receiveQueue.Writer.TryWrite(new ReceivedMessage(message, args, Volatile.Read(ref _sessionId))))
        {
            args.ProcessingFailed = true;
            _logger.LogError("MQTT receive queue is unavailable or full; reconnecting.");
            _reconnectSignal.TrySetResult();
        }

        return Task.CompletedTask;
    }

    private async Task ProcessMessagesAsync(ChannelReader<ReceivedMessage> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var received in reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var handlers = scope.ServiceProvider.GetServices<IMqttMessageHandler>().ToArray();
                    if (handlers.Length == 0)
                    {
                        throw new InvalidOperationException("No MQTT message handler is registered.");
                    }

                    foreach (var handler in handlers)
                    {
                        await handler.HandleAsync(received.Message, cancellationToken);
                    }

                    if (received.Args.ApplicationMessage.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
                    {
                        continue;
                    }

                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(TimeSpan.FromSeconds(_options.OperationTimeoutSeconds));
                    await _operationGate.WaitAsync(timeout.Token);
                    try
                    {
                        // An old packet identifier must never be acknowledged on a new connection.
                        if (received.SessionId == Volatile.Read(ref _sessionId) && _client.IsConnected)
                        {
                            await received.Args.AcknowledgeAsync(timeout.Token);
                        }
                    }
                    finally
                    {
                        _operationGate.Release();
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    received.Args.ProcessingFailed = true;
                    _logger.LogError(exception, "MQTT message handling failed for topic {Topic}.", received.Message.Topic);
                    _reconnectSignal.TrySetResult();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public void Dispose()
    {
        _client.ApplicationMessageReceivedAsync -= ReceiveAsync;
        _client.DisconnectedAsync -= DisconnectedAsync;
        _operationGate.Dispose();
    }

    private sealed record ReceivedMessage
    {
        public ReceivedMessage(MqttMessage message, MqttApplicationMessageReceivedEventArgs args, int sessionId)
        {
            Message = message;
            Args = args;
            SessionId = sessionId;
        }

        public MqttMessage Message { get; }
        public MqttApplicationMessageReceivedEventArgs Args { get; }
        public int SessionId { get; }
    }
}
