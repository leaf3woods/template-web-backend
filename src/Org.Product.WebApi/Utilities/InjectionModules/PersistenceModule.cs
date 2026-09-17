using Autofac;
using Org.Product.Application.Abstractions.Persistence;
using Org.Product.Domain.Repositories;
using Org.Product.Infrastructure.Repositories;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class PersistenceModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterGeneric(typeof(Repository<>))
            .As(typeof(IRepository<>))
            .InstancePerLifetimeScope();

        builder
            .RegisterAssemblyTypes(typeof(UnitOfWork).Assembly)
            .Where(type => type.IsAssignableTo<IUnitOfWork>() || type.IsAssignableTo<ISqlExecutor>())
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }
}
