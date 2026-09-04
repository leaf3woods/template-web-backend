namespace Org.Product.Domain.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default
    );

    Task<int> ExecuteSqlAsync(SqlCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TResult>> QuerySqlAsync<TResult>(
        SqlCommand command,
        CancellationToken cancellationToken = default
    );
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
