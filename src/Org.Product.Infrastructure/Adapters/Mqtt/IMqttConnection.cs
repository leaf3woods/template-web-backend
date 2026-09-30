namespace Org.Product.Infrastructure.Adapters.Mqtt;

public interface IMqttConnection
{
    Task RunAsync(CancellationToken stoppingToken);
}
