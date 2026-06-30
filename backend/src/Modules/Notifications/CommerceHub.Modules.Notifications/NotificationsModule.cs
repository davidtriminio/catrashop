using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Notifications.Api;
using CommerceHub.Modules.Notifications.Application.Abstractions;
using CommerceHub.Modules.Notifications.Application.Services;
using CommerceHub.Modules.Notifications.Application.Templates;
using CommerceHub.Modules.Notifications.Contracts;
using CommerceHub.Modules.Notifications.Infrastructure.Email;
using CommerceHub.Modules.Notifications.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommerceHub.Modules.Notifications;

public sealed class NotificationsModule : IModule
{
    public string Name => "Notifications";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<NotificationsDbContext>(connectionString, "notifications");

        services.AddScoped<INotificationsUnitOfWork>(sp => sp.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<INotificationDispatchRepository, NotificationDispatchRepository>();

        services.AddSingleton<INotificationTemplateRenderer, NotificationTemplateRenderer>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();

        services.AddScoped<NotificationQueryService>();
        services.AddScoped<NotificationPreferenceService>();

        // Public contract consumed by other modules and (from Phase 15) the messaging event consumers.
        services.AddScoped<INotificationDispatcher, NotificationDispatchService>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<NotificationsModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapNotificationEndpoints();
}
