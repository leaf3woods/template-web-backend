using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Template.Web.Infrastructure.DbContexts;

namespace Template.Web.Application.Utilities.InjectionModules
{
    public class SingletonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterInstance(new LoggerFactory().CreateLogger("Adapter"))
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterType<InitialDatabase>().SingleInstance();
            builder
                .Register(context =>
                    ConnectionMultiplexer.Connect(
                        context.Resolve<IConfiguration>().GetConnectionString("Redis")!
                    )
                )
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterGeneric(typeof(PaginatedListConverter<,>));
        }
    }
}
