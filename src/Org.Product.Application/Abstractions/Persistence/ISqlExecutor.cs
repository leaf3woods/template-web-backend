namespace Org.Product.Application.Abstractions.Persistence;

public interface ISqlExecutor
{
    Task<int> ExecuteAsync(SqlCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TResult>> QueryAsync<TResult>(
        SqlCommand command,
        CancellationToken cancellationToken = default
    );
}
