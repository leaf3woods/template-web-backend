using Org.Product.Application.Abstractions.Messaging;

namespace Org.Product.Tests.Support;

public sealed class MqttMessageHandlerStub : IMqttMessageHandler, IAsyncDisposable
{
    private readonly Func<MqttMessage, CancellationToken, Task> _handle;

    public MqttMessageHandlerStub(Func<MqttMessage, CancellationToken, Task> handle)
    {
        _handle = handle;
    }

    public MqttMessage? Message { get; private set; }
    public bool Disposed { get; private set; }
    public TaskCompletionSource DisposedSignal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task HandleAsync(MqttMessage message, CancellationToken cancellationToken = default)
    {
        Message = message;
        return _handle(message, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        DisposedSignal.TrySetResult();
        return ValueTask.CompletedTask;
    }
}
