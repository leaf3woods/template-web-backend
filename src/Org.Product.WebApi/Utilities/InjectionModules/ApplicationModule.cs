using Autofac;
using Org.Product.Application.Services.Base;
using Org.Product.Application.Utilities;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class ApplicationModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(IBaseService).Assembly)
            .Where(type => type.IsAssignableTo<IBaseService>())
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder.RegisterGeneric(typeof(PaginatedListConverter<,>));
    }
}
