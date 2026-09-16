using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Org.Product.Infrastructure.Repositories;

public sealed class DatabaseMigrator
{
    private readonly ApiDbContext _context;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(ApiDbContext context, ILogger<DatabaseMigrator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        _logger.LogInformation("Applying migrations to {Database} on {DataSource}.",
            connection.Database, connection.DataSource);
        if (!_context.Database.GetMigrations().Any())
        {
            throw new InvalidOperationException("No migrations were found.");
        }

        await _context.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("Database migrations completed.");
    }
}
