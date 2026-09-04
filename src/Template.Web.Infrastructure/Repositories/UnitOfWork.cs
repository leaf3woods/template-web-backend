using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Template.Web.Domain.Repositories;

namespace Template.Web.Infrastructure.Repositories;

public sealed class UnitOfWork(ApiDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active for this scope.");
        }

        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    public Task<int> ExecuteSqlAsync(
        SqlCommand command,
        CancellationToken cancellationToken = default
    )
    {
        Validate(command);
        return dbContext.Database.ExecuteSqlRawAsync(
            command.Text,
            CreateParameters(command.Parameters),
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<TResult>> QuerySqlAsync<TResult>(
        SqlCommand command,
        CancellationToken cancellationToken = default
    )
    {
        Validate(command);
        return await dbContext
            .Database.SqlQueryRaw<TResult>(command.Text, CreateParameters(command.Parameters))
            .ToListAsync(cancellationToken);
    }

    private static NpgsqlParameter[] CreateParameters(IReadOnlyCollection<SqlParameter> parameters)
    {
        return parameters
            .Select(parameter =>
            {
                var dbParameter = new NpgsqlParameter(
                    parameter.Name,
                    parameter.Value ?? DBNull.Value
                );
                if (parameter.DbType is DbType dbType)
                {
                    dbParameter.DbType = dbType;
                }

                return dbParameter;
            })
            .ToArray();
    }

    private static void Validate(SqlCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Text);
        ArgumentNullException.ThrowIfNull(command.Parameters);

        foreach (var parameter in command.Parameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(parameter.Name);
        }
    }

    private sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return transaction.CommitAsync(cancellationToken);
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return transaction.RollbackAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return transaction.DisposeAsync();
        }
    }
}
