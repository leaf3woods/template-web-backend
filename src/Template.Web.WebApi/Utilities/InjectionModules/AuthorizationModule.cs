using Autofac;
using Microsoft.AspNetCore.Authorization;
using Template.Web.WebApi.Auth.AuthHandlers;

namespace Template.Web.WebApi.Utilities.InjectionModules;

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
