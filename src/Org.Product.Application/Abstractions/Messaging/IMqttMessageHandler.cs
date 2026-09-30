namespace Org.Product.Application.Abstractions.Messaging;

public interface IMqttMessageHandler
{
    Task HandleAsync(MqttMessage message, CancellationToken cancellationToken = default);
}
