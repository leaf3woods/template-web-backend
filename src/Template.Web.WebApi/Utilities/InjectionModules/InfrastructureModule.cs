using System.Reflection;
using Autofac;
using StackExchange.Redis;
using Template.Web.Domain.Repositories;
using Template.Web.Domain.Services;
using Template.Web.Infrastructure.Repositories;

namespace Template.Web.WebApi.Utilities.InjectionModules;

public sealed class InfrastructureModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(Assembly.Load("Template.Web.Infrastructure"))
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
