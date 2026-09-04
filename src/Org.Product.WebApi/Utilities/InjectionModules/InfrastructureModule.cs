using Autofac;
using StackExchange.Redis;
using Org.Product.Domain.Repositories;
using Org.Product.Domain.Services;
using Org.Product.Infrastructure.Repositories;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class InfrastructureModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(ApiDbContext).Assembly)
            .Where(type => type.IsAssignableTo<IDomainService>())
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder.RegisterType<InitialDatabase>().InstancePerLifetimeScope();
        builder
            .RegisterGeneric(typeof(Repository<>))
            .As(typeof(IRepository<>))
            .InstancePerLifetimeScope();
        builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
        builder
            .Register(context =>
                ConnectionMultiplexer.Connect(
                    context.Resolve<IConfiguration>().GetConnectionString("Redis")!
                )
            )
            .As<IConnectionMultiplexer>()
            .SingleInstance();
    }
}
