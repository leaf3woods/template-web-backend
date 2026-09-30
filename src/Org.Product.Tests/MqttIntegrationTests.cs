using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Org.Product.Application.Abstractions.Messaging;
using Org.Product.Infrastructure.Adapters.Mqtt;
using Org.Product.Infrastructure.Utilities.Options;
using Org.Product.Tests.Support;

namespace Org.Product.Tests;

public sealed class MqttIntegrationTests
{
    [Theory]
    [InlineData(MqttProtocolVersion.V311, MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttProtocolVersion.V311, MqttQualityOfServiceLevel.ExactlyOnce)]
    [InlineData(MqttProtocolVersion.V500, MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttProtocolVersion.V500, MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task LocalBroker_RequestHandlerCanPublishReply(MqttProtocolVersion protocol, MqttQualityOfServiceLevel qualityOfService)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var options = new MqttOptions
        {
            Enabled = true,
            Host = "127.0.0.1",
            Port = port,
            ClientId = $"template-adapter-{Guid.NewGuid():N}",
            ProtocolVersion = protocol,
            ReconnectDelaySeconds = 1,
            OperationTimeoutSeconds = 5,
            PublishQualityOfService = qualityOfService,
            Subscriptions = [new MqttSubscriptionOptions { TopicFilter = "template/request", QualityOfService = qualityOfService }],
        };
        using var server = new MqttServerFactory().CreateMqttServer(new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
            .WithDefaultEndpointBoundIPV6Address(IPAddress.IPv6Loopback)
            .WithDefaultEndpointPort(port)
            .Build());
        var subscribed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.ClientSubscribedTopicAsync += args =>
        {
            if (args.ClientId == options.ClientId)
            {
                subscribed.TrySetResult();
            }

            return Task.CompletedTask;
        };
        await server.StartAsync();
        try
        {
            using var client = new MqttClientFactory().CreateMqttClient();
            using var peer = new MqttClientFactory().CreateMqttClient();
            MqttClientAdapter? adapter = null;
            await using var services = new ServiceCollection().AddScoped<IMqttMessageHandler>(_ => new MqttMessageHandlerStub(
                (message, token) => adapter!.PublishAsync("template/reply", message.Payload, token))).BuildServiceProvider();
            using var ownedAdapter = new MqttClientAdapter(client, Options.Create(options), services.GetRequiredService<IServiceScopeFactory>(), NullLogger<MqttClientAdapter>.Instance);
            adapter = ownedAdapter;
            using var stopping = new CancellationTokenSource();
            var run = adapter.RunAsync(stopping.Token);
            try
            {
                var reply = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                peer.ApplicationMessageReceivedAsync += args =>
                {
                    reply.TrySetResult(args.ApplicationMessage.Payload.ToArray());
                    return Task.CompletedTask;
                };
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var connected = await peer.ConnectAsync(new MqttClientOptionsBuilder()
                    .WithTcpServer("127.0.0.1", port)
                    .WithClientId($"template-peer-{Guid.NewGuid():N}")
                    .WithProtocolVersion(protocol)
                    .WithCleanSession()
                    .Build(), timeout.Token);
                Assert.Equal(MqttClientConnectResultCode.Success, connected.ResultCode);
                await peer.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(filter => filter.WithTopic("template/reply").WithQualityOfServiceLevel(qualityOfService))
                    .Build(), timeout.Token);
                await subscribed.Task.WaitAsync(timeout.Token);
                await peer.PublishAsync(new MqttApplicationMessageBuilder()
                    .WithTopic("template/request")
                    .WithPayload(new byte[] { 1, 2, 3 })
                    .WithQualityOfServiceLevel(qualityOfService)
                    .Build(), timeout.Token);

                Assert.Equal(new byte[] { 1, 2, 3 }, await reply.Task.WaitAsync(timeout.Token));
                await peer.DisconnectAsync(new MqttClientDisconnectOptions(), timeout.Token);
            }
            finally
            {
                await stopping.CancelAsync();
                await run.WaitAsync(TimeSpan.FromSeconds(5));
            }

            Assert.False(client.IsConnected);
        }
        finally
        {
            await server.StopAsync(new MqttServerStopOptions()).WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
