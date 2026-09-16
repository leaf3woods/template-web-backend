using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Org.Product.Infrastructure;
using Org.Product.Infrastructure.Repositories;

namespace Org.Product.DbMigrator;

public sealed class ApiDbContextFactory : IDesignTimeDbContextFactory<ApiDbContext>
{
    public ApiDbContext CreateDbContext(string[] args)
    {
        // Design-time model and script generation never require a live database.
        var options = new DbContextOptionsBuilder<ApiDbContext>();
        DependencyInjection.ConfigureDatabase(options,
            "Host=localhost;Database=org_product_design;Username=design");
        return new ApiDbContext(options.Options);
    }
}
