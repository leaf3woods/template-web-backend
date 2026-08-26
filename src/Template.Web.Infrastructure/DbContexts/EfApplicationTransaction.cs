using Microsoft.EntityFrameworkCore.Storage;
using Template.Web.Application.Persistence;

namespace Template.Web.Infrastructure.DbContexts;

internal sealed class EfApplicationTransaction : IApplicationTransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfApplicationTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _transaction.RollbackAsync(cancellationToken);

    public void Dispose() => _transaction.Dispose();

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
