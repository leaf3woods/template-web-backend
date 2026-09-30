using Autofac;
using MQTTnet;
using Org.Product.Application.Abstractions.Messaging;

namespace Org.Product.WebApi.Utilities.InjectionModules;

public sealed class MqttModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(_ => new MqttClientFactory().CreateMqttClient())
            .As<IMqttClient>()
            .SingleInstance();
        builder.RegisterAssemblyTypes(typeof(IMqttMessageHandler).Assembly)
            .Where(type => type.IsAssignableTo<IMqttMessageHandler>())
            .As<IMqttMessageHandler>()
            .InstancePerLifetimeScope();
    }
}
