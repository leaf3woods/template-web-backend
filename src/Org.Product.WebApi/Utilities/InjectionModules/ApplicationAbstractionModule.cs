using Autofac;
using Org.Product.Application.Abstractions;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Application.Abstractions.Captchas;
using Org.Product.Domain.Entities.Account;
using Org.Product.Infrastructure.Adapters.Authentication;
using Org.Product.Infrastructure.Adapters.Captchas;
using Org.Product.Infrastructure.DomainServices;
using StackExchange.Redis;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class ApplicationAbstractionModule : Module
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
            .RegisterType<RedisUserSessionStore>()
            .As<ICacheStore<User, Guid>>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<RedisCaptchaChallengeStore>()
            .As<ICacheStore<Captcha, Guid>>()
            .InstancePerLifetimeScope();
        builder
            .RegisterAssemblyTypes(typeof(PasswordCredentialService).Assembly)
            .Where(type => type.IsAssignableTo<IApplicationAbstraction>())
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .Register(context => new FileSigningKeyProvider(
                context.Resolve<IConfiguration>()["Jwt:KeyFolder"]
                    ?? throw new InvalidOperationException("Missing Jwt:KeyFolder.")
            ))
            .SingleInstance();
    }
}
