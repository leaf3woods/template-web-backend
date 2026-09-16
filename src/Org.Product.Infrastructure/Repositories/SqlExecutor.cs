using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Org.Product.Application.Abstractions.Persistence;

namespace Org.Product.Infrastructure.Repositories;

public sealed class SqlExecutor : ISqlExecutor
{
    private readonly ApiDbContext _dbContext;

    public SqlExecutor(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> ExecuteAsync(
        SqlCommand command,
        CancellationToken cancellationToken = default
    )
    {
        Validate(command);
        return _dbContext.Database.ExecuteSqlRawAsync(
            command.Text,
            CreateParameters(command.Parameters),
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<TResult>> QueryAsync<TResult>(
        SqlCommand command,
        CancellationToken cancellationToken = default
    )
    {
        Validate(command);
        return await _dbContext
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

}
