using Autofac;
using Template.Web.Application.Services.Base;
using Template.Web.Application.Utilities;

namespace Template.Web.WebApi.Utilities.InjectionModules;

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
