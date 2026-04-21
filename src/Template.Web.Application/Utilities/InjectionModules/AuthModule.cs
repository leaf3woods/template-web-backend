using Autofac;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Template.Web.Application.Utilities.InjectionModules
{
    public class AuthModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(type => type.Name.EndsWith("RequireHandler"))
                .AsImplementedInterfaces();
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(type => type.IsAssignableTo(typeof(IAuthorizationRequirement)))
                .AsImplementedInterfaces();
        }
    }
}