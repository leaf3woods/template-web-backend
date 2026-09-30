using Org.Product.Infrastructure.Adapters.Mqtt;

namespace Org.Product.WebApi.Workers;

public sealed class MqttWorker : BackgroundService
{
    private readonly IMqttConnection _connection;

    public MqttWorker(IMqttConnection connection)
    {
        _connection = connection;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => _connection.RunAsync(stoppingToken);
}
