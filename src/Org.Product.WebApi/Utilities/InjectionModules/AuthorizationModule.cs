using Autofac;
using Microsoft.AspNetCore.Authorization;
using Org.Product.WebApi.Auth.AuthHandlers;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class AuthorizationModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<CustomRequireHandler>()
            .As<IAuthorizationHandler>()
            .InstancePerLifetimeScope();
    }
}
