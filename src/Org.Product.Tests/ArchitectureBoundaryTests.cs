using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Org.Product.Application.Services;
using Org.Product.Domain.Entities.Account;
using Org.Product.Infrastructure;
using Org.Product.Infrastructure.Repositories;

namespace Org.Product.Tests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void Application_DoesNotReferenceInfrastructureSigningOrRendering()
    {
        var names = typeof(UserService).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToArray();
        Assert.DoesNotContain("Org.Product.Infrastructure", names);
        Assert.DoesNotContain("SkiaSharp", names);
        Assert.DoesNotContain("System.IdentityModel.Tokens.Jwt", names);
        Assert.DoesNotContain("StackExchange.Redis", names);
    }

    [Fact]
    public void PersistenceModel_HasMigrationAndNoPendingChanges()
    {
        using var context = CreateContext();
        Assert.NotEmpty(context.Database.GetMigrations());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void InitialMigration_CanGenerateIdempotentScriptWithoutDatabase()
    {
        using var context = CreateContext();
        var script = context.GetService<IMigrator>().GenerateScript(
            options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("__EFMigrationsHistory", script);
        Assert.Contains("CREATE TABLE", script);
        Assert.Contains(User.DevUser.Id.ToString(), script);
    }

    private static ApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>();
        DependencyInjection.ConfigureDatabase(options,
            "Host=127.0.0.1;Port=1;Database=model_test;Username=unused");
        return new ApiDbContext(options.Options);
    }
}
