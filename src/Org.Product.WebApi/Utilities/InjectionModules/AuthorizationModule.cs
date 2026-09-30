using Autofac;
using Microsoft.AspNetCore.Authorization;
using Org.Product.WebApi.Auth.AuthHandlers;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class AuthorizationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
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
