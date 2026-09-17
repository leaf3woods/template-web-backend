using Microsoft.EntityFrameworkCore;
using Org.Product.Infrastructure.Repositories;

namespace Org.Product.DbMigrator;

internal static class DatabaseOptions
{
    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
    }
}
