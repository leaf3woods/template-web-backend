using Autofac;
using Org.Product.Application.Abstractions.Security;
using Org.Product.Infrastructure.Adapters.Security;
using StackExchange.Redis;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class SecurityInfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .Register(context =>
                ConnectionMultiplexer.Connect(
                    context.Resolve<IConfiguration>().GetConnectionString("Redis")
                    ?? throw new InvalidOperationException("Missing Redis connection string.")
                )
            )
            .As<IConnectionMultiplexer>()
            .SingleInstance();
        builder
            .RegisterAssemblyTypes(typeof(RedisUserSessionStore).Assembly)
            .Where(type => type.IsAssignableTo<ISecurityStore>())
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        builder
            .RegisterAssemblyTypes(typeof(PasswordCredentialService).Assembly)
            .Where(type => type.IsAssignableTo<ISecurityService>())
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .Register(context =>
                new FileSigningKeyProvider(
                    context.Resolve<IConfiguration>()["Jwt:KeyFolder"]
                    ?? throw new InvalidOperationException("Missing Jwt:KeyFolder.")
                )
            )
            .SingleInstance();
    }
}
