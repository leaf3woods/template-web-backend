using Autofac;
using Template.Web.Application.Services.Base;
using Template.Web.Domain.Services;
using System.Reflection;

namespace Template.Web.WebApi.Utilities.InjectionModules
{
    public class AppServiceModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(Assembly.Load("Template.Web." + nameof(Application)))
                .Where(type => type.IsAssignableTo(typeof(IBaseService)))
                .AsImplementedInterfaces()
                .PropertiesAutowired();

            builder.RegisterAssemblyTypes(Assembly.Load("Template.Web." + nameof(Domain)), Assembly.Load("Template.Web." + nameof(Infrastructure)))
                .Where(type => type.IsAssignableTo<IDomainService>())
                .AsImplementedInterfaces()
                .PropertiesAutowired();
        }
    }
}