using System.Reflection;
using Autofac;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Template.Web.Domain.Services;
using Template.Web.Infrastructure.DbContexts;

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
            .Register(context =>
                ConnectionMultiplexer.Connect(
                    context.Resolve<IConfiguration>().GetConnectionString("Redis")!
                )
            )
            .As<IConnectionMultiplexer>()
            .SingleInstance();
    }
}
