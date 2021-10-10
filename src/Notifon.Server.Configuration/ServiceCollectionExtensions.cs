﻿using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifon.Server.Configuration.Options;

namespace Notifon.Server.Configuration {
    public static class ServiceCollectionExtensions {
        public static IServiceCollection ConfigureOptions(this IServiceCollection services,
            IConfiguration configuration) {
            services
                .Configure<KafkaOptions>(configuration.GetSection(Constants.KafkaOptions))
                .Configure<RedisCacheOptions>(configuration.GetSection(Constants.RedisOptions))
                .Configure<RabbitMqOptions>(configuration.GetSection(Constants.RabbitMqOptions))
                .Configure<TelegramOptions>(configuration.GetSection(Constants.TelegramOptions))
                .Configure<MailGunOptions>(configuration.GetSection(Constants.MailGunOptions))
                .Configure<RetryPolicyOptions>(configuration.GetSection(Constants.RetryPolicyOptions));
            return services;
        }
    }
}