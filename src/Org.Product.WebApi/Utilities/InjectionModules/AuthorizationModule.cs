using Autofac;
using Microsoft.AspNetCore.Authorization;
using Org.Product.Domain.Services.Base;
using Org.Product.WebApi.Auth.AuthHandlers;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class AuthorizationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(IDomainService).Assembly)
            .Where(type => type.IsAssignableTo<IDomainService>())
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .RegisterAssemblyTypes(typeof(CustomRequireHandler).Assembly)
            .AssignableTo<IAuthorizationHandler>()
            .As<IAuthorizationHandler>()
            .InstancePerLifetimeScope();
        builder
            .RegisterAssemblyTypes(typeof(SessionJwtBearerEvents).Assembly)
            .AssignableTo<SessionJwtBearerEvents>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
