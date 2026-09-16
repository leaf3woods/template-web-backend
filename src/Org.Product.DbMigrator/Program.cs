using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Org.Product.Infrastructure;
using Org.Product.Infrastructure.Repositories;

try
{
    if (args.Contains("--help"))
    {
        Console.WriteLine("""
            Org.Product.DbMigrator
              --script <path>  Generate an idempotent SQL script without connecting to a database.
              (no arguments)  Apply pending migrations using ConnectionStrings__Postgres.
            """);
        return 0;
    }

    if (args.Length == 2 && args[0] == "--script")
    {
        using var context = new Org.Product.DbMigrator.ApiDbContextFactory().CreateDbContext([]);
        var script = context.GetService<IMigrator>().GenerateScript(
            options: MigrationsSqlGenerationOptions.Idempotent);
        await File.WriteAllTextAsync(args[1], script);
        Console.WriteLine($"Migration script written to {Path.GetFullPath(args[1])}");
        return 0;
    }

    if (args.Length != 0)
    {
        Console.Error.WriteLine("Unknown arguments. Use --help.");
        return 2;
    }

    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        ContentRootPath = AppContext.BaseDirectory,
    });
    builder.Logging.ClearProviders().AddSimpleConsole();
    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddScoped<DatabaseMigrator>();
    using var host = builder.Build();
    await host.StartAsync();
    try
    {
        await using var scope = host.Services.CreateAsyncScope();
        var cancellation = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().RunAsync(cancellation);
    }
    finally
    {
        await host.StopAsync();
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Database migration failed: {exception.Message}");
    return 1;
}
