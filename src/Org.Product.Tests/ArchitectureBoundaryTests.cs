using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Org.Product.Application.Abstractions.Persistence;
using Org.Product.Application.Services;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Repositories;
using Org.Product.Domain.Services;
using Org.Product.Domain.Services.Base;
using Org.Product.Infrastructure.Repositories;
using Org.Product.WebApi.Utilities.InjectionModules;

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

    [Fact]
    public void WebApiModules_AutoDiscoverAndUseWebApiDbContextPool()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] =
                        "Host=127.0.0.1;Port=1;Database=model_test;Username=unused",
                }
            )
            .Build();
        var services = new ServiceCollection();
        services.AddDbContextPool<ApiDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                .UseSnakeCaseNamingConvention());
        var factory = new AutofacServiceProviderFactory();
        var container = factory.CreateBuilder(services);
        container.RegisterAssemblyModules(typeof(PersistenceModule).Assembly);

        var serviceProvider = factory.CreateServiceProvider(container);
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var sqlExecutor = scope.ServiceProvider.GetRequiredService<ISqlExecutor>();
            var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationDomainService>();

            Assert.IsType<ApiDbContext>(context);
            Assert.IsType<Repository<User>>(repository);
            Assert.IsType<UnitOfWork>(unitOfWork);
            Assert.IsType<SqlExecutor>(sqlExecutor);
            Assert.IsType<AuthorizationDomainService>(authorization);
        }
        finally
        {
            (serviceProvider as IDisposable)?.Dispose();
        }
    }

    private static ApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>();
        options
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=model_test;Username=unused")
            .UseSnakeCaseNamingConvention();
        return new ApiDbContext(options.Options);
    }
}
