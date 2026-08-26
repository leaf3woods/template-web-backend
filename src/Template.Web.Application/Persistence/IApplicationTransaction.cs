namespace Template.Web.Application.Persistence;

/// <summary>
///     Represents an application-managed database transaction.
/// </summary>
public interface IApplicationTransaction : IDisposable, IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
