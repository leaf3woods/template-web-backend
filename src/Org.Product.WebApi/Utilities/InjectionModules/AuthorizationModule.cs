using Autofac;
using Microsoft.AspNetCore.Authorization;
using Org.Product.WebApi.Auth.AuthHandlers;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class AuthorizationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SessionJwtBearerEvents>().InstancePerLifetimeScope();
        builder
            .RegisterType<CustomRequireHandler>()
            .As<IAuthorizationHandler>()
            .InstancePerLifetimeScope();
    }
}
