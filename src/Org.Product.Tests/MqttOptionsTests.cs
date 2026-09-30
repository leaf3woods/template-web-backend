using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Org.Product.Application.Abstractions.Messaging;
using Org.Product.Infrastructure.Adapters.Mqtt;
using Org.Product.Infrastructure.Utilities.Options;
using Org.Product.WebApi.Utilities;
using Org.Product.WebApi.Utilities.InjectionModules;
using Org.Product.WebApi.Workers;

namespace Org.Product.Tests;

public sealed class MqttOptionsTests
{
    [Fact]
    public void Disabled_DoesNotRequireBrokerConfiguration()
    {
        var options = new MqttOptions { Port = 0, ReceiveQueueCapacity = 0 };
        Assert.True(new MqttOptionsValidator().Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData("iot/+/data")]
    [InlineData("iot/#")]
    [InlineData("/app/up/user/+/notice")]
    public void ValidSubscriptionFilters_AreAccepted(string filter)
    {
        var options = new MqttOptions { Enabled = true, ClientId = "test" };
        options.Subscriptions.Add(new MqttSubscriptionOptions { TopicFilter = filter });
        Assert.True(new MqttOptionsValidator().Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("iot/#/data")]
    [InlineData("iot/device+")]
    [InlineData("iot/device#")]
    [InlineData("iot/\0")]
    public void InvalidSubscriptionFilters_FailValidation(string filter)
    {
        var options = new MqttOptions { Enabled = true, ClientId = "test" };
        options.Subscriptions.Add(new MqttSubscriptionOptions { TopicFilter = filter });
        Assert.True(new MqttOptionsValidator().Validate(null, options).Failed);
    }

    [Fact]
    public void InvalidConnectionValues_FailValidation()
    {
        var invalid = new MqttOptions
        {
            Enabled = true,
            Host = "",
            ClientId = "",
            Port = 0,
            OperationTimeoutSeconds = 0,
            ReconnectDelaySeconds = 0,
            PublishQualityOfService = (MqttQualityOfServiceLevel)99,
            ProtocolVersion = (MqttProtocolVersion)99,
        };
        Assert.True(new MqttOptionsValidator().Validate(null, invalid).Failed);
    }

    [Fact]
    public void DuplicateFilters_FailValidation()
    {
        var options = new MqttOptions { Enabled = true, ClientId = "test" };
        options.Subscriptions.Add(new MqttSubscriptionOptions { TopicFilter = "iot/#" });
        options.Subscriptions.Add(new MqttSubscriptionOptions { TopicFilter = "iot/#" });
        Assert.True(new MqttOptionsValidator().Validate(null, options).Failed);
    }

    [Fact]
    public void InvalidUtf8AndOversizedFilters_FailValidation()
    {
        var options = new MqttOptions { Enabled = true, ClientId = "test" };
        options.Subscriptions.Add(new MqttSubscriptionOptions { TopicFilter = new string('\uD800', 1) });
        Assert.True(new MqttOptionsValidator().Validate(null, options).Failed);
        options.Subscriptions[0].TopicFilter = new string('中', 22000);
        Assert.True(new MqttOptionsValidator().Validate(null, options).Failed);
        options.Subscriptions.Clear();
        options.ClientId = "test\0";
        Assert.True(new MqttOptionsValidator().Validate(null, options).Failed);
    }

    [Fact]
    public async Task Modules_PublisherAndWorkerShareOneClientAdapter()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddMqtt();
        var factory = new AutofacServiceProviderFactory();
        var builder = factory.CreateBuilder(services);
        builder.RegisterAssemblyModules(typeof(MqttModule).Assembly);
        await using var provider = (AutofacServiceProvider)factory.CreateServiceProvider(builder);

        var publisher = provider.GetRequiredService<IMqttPublisher>();
        var connection = provider.GetRequiredService<IMqttConnection>();
        Assert.Same(publisher, connection);
        Assert.IsType<MqttWorker>(Assert.Single(provider.GetServices<IHostedService>()));
        await connection.RunAsync(CancellationToken.None);
    }

    [Fact]
    public void BoundOptions_InvalidEnabledConfigurationThrows()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mqtt:Enabled"] = "true",
            ["Mqtt:ClientId"] = "",
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddMqtt();
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MqttOptions>>().Value);
    }
}
