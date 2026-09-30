namespace Org.Product.Application.Abstractions.Messaging;

public interface IMqttPublisher : IApplicationAbstraction
{
    Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);
}
