using Microsoft.Extensions.Options;
using Org.Product.Infrastructure.Utilities.Options;
using Org.Product.WebApi.Workers;

namespace Org.Product.WebApi.Utilities;

public static class MqttServiceCollectionExtensions
{
    public static IServiceCollection AddMqtt(this IServiceCollection services)
    {
        services.AddOptions<MqttOptions>()
            .BindConfiguration(MqttOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MqttOptions>, MqttOptionsValidator>();
        services.AddHostedService<MqttWorker>();
        return services;
    }
}
