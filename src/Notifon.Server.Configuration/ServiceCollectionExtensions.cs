using System;
using EverscaleNet.Client.Models;
using EverscaleNet.Models;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Notifon.Server.Configuration.Options;

namespace Notifon.Server.Configuration;

public static class ServiceCollectionExtensions {
    public static IServiceCollection ConfigureOptions(this IServiceCollection services,
                                                      IConfiguration configuration) {
        services
            .Configure<AppOptions>(configuration.GetSection(Sections.App))
            .Configure<KafkaOptions>(configuration.GetSection(Sections.Kafka))
            .Configure<RedisCacheOptions>(configuration.GetSection(Sections.Redis))
            .Configure<RabbitMqOptions>(configuration.GetSection(Sections.RabbitMq))
            .Configure<TelegramOptions>(configuration.GetSection(Sections.Telegram))
            .Configure<MailGunOptions>(configuration.GetSection(Sections.MailGun))
            .Configure<RetryPolicyOptions>(configuration.GetSection(Sections.RetryPolicy))
            .Configure<EverClientOptions>(options => options.Network = configuration.GetSection(Sections.EverClientNetwork).Get<NetworkConfig>())
            .Configure<HealthCheckPublisherOptions>(options => {
                options.Delay = TimeSpan.FromSeconds(2);
                options.Predicate = check => check.Tags.Contains("ready");
            });
        return services;
    }
}