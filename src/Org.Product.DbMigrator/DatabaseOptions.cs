using Microsoft.EntityFrameworkCore;

namespace Org.Product.DbMigrator;

internal static class DatabaseOptions
{
    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
    }
}
