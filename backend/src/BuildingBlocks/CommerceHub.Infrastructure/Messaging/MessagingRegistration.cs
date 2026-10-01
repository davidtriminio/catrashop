using CommerceHub.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CommerceHub.Infrastructure.Messaging;

/// <summary>Composition-root helpers to wire the Outbox → RabbitMQ → consumer pipeline.</summary>
public static class MessagingRegistration
{
    /// <summary>
    /// Registers the event bus, the type registry, the dispatcher, and the publisher/consumer background
    /// services. Call <see cref="AddOutboxProcessor{TContext}"/> for each publishing module and register the
    /// event types (<see cref="IntegrationEventTypeRegistry"/>) and handlers separately.
    /// </summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IntegrationEventTypeRegistry>();
        services.AddSingleton<IntegrationEventDispatcher>();
        services.AddHostedService<OutboxPublisherService>();

        if (UsesInProcessTransport(configuration))
        {
            // Small single-node deployments: no broker, the outbox publisher dispatches straight to the handlers.
            services.AddSingleton<IEventBus, InProcessEventBus>();
        }
        else
        {
            services.AddSingleton<IEventBus, RabbitMqEventBus>();
            services.AddHostedService<IntegrationEventConsumerService>();
        }

        return services;
    }

    /// <summary>True when <c>Messaging:Transport</c> is "InProcess" (default is "RabbitMq").</summary>
    public static bool UsesInProcessTransport(IConfiguration configuration) =>
        string.Equals(configuration["Messaging:Transport"], "InProcess", StringComparison.OrdinalIgnoreCase);

    /// <summary>Registers the outbox drain for one module's DbContext (call once per publishing module).</summary>
    public static IServiceCollection AddOutboxProcessor<TContext>(this IServiceCollection services)
        where TContext : ModuleDbContext
    {
        services.AddScoped<IOutboxProcessor, OutboxProcessor<TContext>>();
        return services;
    }
}
