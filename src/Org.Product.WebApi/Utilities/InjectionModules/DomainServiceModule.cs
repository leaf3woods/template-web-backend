using Autofac;
using Org.Product.Domain.Services.Base;
using Org.Product.Infrastructure.DomainServices;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public class DomainServiceModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(
                typeof(IDomainService).Assembly,
                typeof(CurrentUserDomainService).Assembly
            )
            .Where(type => type.IsAssignableTo<IDomainService>())
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }
}
