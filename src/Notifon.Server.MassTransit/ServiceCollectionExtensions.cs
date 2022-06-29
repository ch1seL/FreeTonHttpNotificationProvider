using System;
using MassTransit;
using MassTransit.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notifon.Server.Business.Events;
using Notifon.Server.Business.Requests.Api;
using Notifon.Server.Business.Requests.Endpoint;
using Notifon.Server.Business.Requests.EverClient;
using Notifon.Server.Configuration.Options;
using Notifon.Server.Kafka;
using Notifon.Server.SignalR;

namespace Notifon.Server.MassTransit;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddMassTransit(this IServiceCollection services, bool useRabbitMq) {
        services
            .AddMediator(x => {
                x.AddConsumer<GetServerStatusConsumer>();
                x.AddRequestClient<GetServerStatus>();
                x.AddConsumer<SubmitClientConsumer>();
                x.AddRequestClient<SubmitClient>();
                x.AddConsumer<DecryptEncryptedMessageConsumer>();
                x.AddRequestClient<DecryptEncryptedMessage>();
                x.AddConsumer<FormatDecryptedMessageConsumer>();
                x.AddRequestClient<FormatDecryptedMessage>();
                x.AddConsumer<EverSendMessageConsumer>();
                x.AddRequestClient<EverSendMessage>();
                x.AddConsumer<EverDeployConsumer>();
                x.AddRequestClient<EverDeploy>();
            })
            .AddMassTransit(x => {
                x.AddDelayedMessageScheduler();
                x.AddConsumer<PublishMessageHttpConsumer, PublishMessageHttpConsumerDefinition>();
                x.AddConsumer<PublishMessageTelegramConsumer, PublishMessageTelegramConsumerDefinition>();
                x.AddConsumer<PublishMessageMailgunConsumer, PublishMessageMailgunConsumerDefinition>();
                x.AddConsumer<PublishMessageByUserIdConsumer, PublishMessageByUserIdConsumerDefinition>();
                x.AddConsumer<PublishMessageFcmConsumer, PublishMessageFcmConsumerDefinition>();
                x.AddRider(RiderRegistrationConfiguratorExtensions.KafkaRegistrationConfigurator);
                x.AddSignalRHub<SignalRHub>();
                x.SetKebabCaseEndpointNameFormatter();

                if (useRabbitMq) {
                    x.UsingRabbitMq((context, cfg) => {
                        SetupRabbitMqHost(cfg, context);
                        ConfigureContext(cfg, context);
                        cfg.ConfigureEndpoints(context);
                    });
                } else {
                    x.UsingInMemory((context, cfg) => {
                        ConfigureContext(cfg, context);
                        cfg.UseMessageScope(context);
                        cfg.ConfigureEndpoints(context);
                    });
                }
            });

        return services;
    }

    private static void ConfigureContext(IBusFactoryConfigurator cfg, IServiceProvider provider) {
        cfg.UseDelayedMessageScheduler();
        cfg.UsePublishFilter(typeof(PublishMessageDecryptMessageFilter<>), provider);
        cfg.UsePublishFilter(typeof(PublishMessageLoggingFilter<>), provider);
        cfg.UsePrometheusMetrics();
    }

    private static void SetupRabbitMqHost(IRabbitMqBusFactoryConfigurator cfg, IServiceProvider provider) {
        var options = provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        cfg.Host(options.Host, r => {
            r.Username(options.Username);
            r.Password(options.Password);
        });
    }
}