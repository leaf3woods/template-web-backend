using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Org.Product.Application.Abstractions.Persistence;
using Org.Product.Application.Abstractions.Security;
using Org.Product.Domain.Repositories;
using Org.Product.Infrastructure.Adapters.Security;
using Org.Product.Infrastructure.Adapters.Security.Options;
using Org.Product.Infrastructure.Repositories;
using StackExchange.Redis;

namespace Org.Product.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContextPool<ApiDbContext>(options => ConfigureDatabase(options, connectionString));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISqlExecutor, SqlExecutor>();
        return services;
    }

    public static void ConfigureDatabase(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
    }

    public static IServiceCollection AddSecurityInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Missing Redis connection string.")));
        services.AddScoped<IUserSessionStore, RedisUserSessionStore>();
        services.AddScoped<ICaptchaChallengeStore, RedisCaptchaChallengeStore>();
        services.AddScoped<IRolePermissionStore, RedisRolePermissionStore>();
        services.AddSingleton<IPasswordCredentialService, PasswordCredentialService>();
        services.AddSingleton<ICaptchaGenerator, SkiaCaptchaGenerator>();
        services.AddSingleton<FileSigningKeyProvider>(_ =>
            new FileSigningKeyProvider(configuration["Jwt:KeyFolder"]
                ?? throw new InvalidOperationException("Missing Jwt:KeyFolder.")));
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddOptions<AccessTokenOptions>()
            .BindConfiguration(AccessTokenOptions.SectionName)
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Issuer)
                && !string.IsNullOrWhiteSpace(options.Audience)
                && options.ExpireMin > 0,
                "Jwt requires Issuer, Audience and a positive ExpireMin.")
            .ValidateOnStart();
        services.AddOptions<CaptchaRenderingOptions>()
            .BindConfiguration(CaptchaRenderingOptions.SectionName)
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.FontFamily)
                && options.Height > 0 && options.Width > 0,
                "Captcha requires FontFamily and positive dimensions.")
            .ValidateOnStart();
        return services;
    }
}
