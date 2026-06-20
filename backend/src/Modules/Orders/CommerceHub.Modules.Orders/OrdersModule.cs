using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Orders.Api;
using CommerceHub.Modules.Orders.Application.Abstractions;
using CommerceHub.Modules.Orders.Application.Configuration;
using CommerceHub.Modules.Orders.Application.Services;
using CommerceHub.Modules.Orders.Contracts;
using CommerceHub.Modules.Orders.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommerceHub.Modules.Orders;

public sealed class OrdersModule : IModule
{
    public string Name => "Orders";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrderingOptions>(configuration.GetSection(OrderingOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<OrdersDbContext>(connectionString, "orders");

        services.AddScoped<IOrderUnitOfWork>(sp => sp.GetRequiredService<OrdersDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderReadStore, OrderReadStore>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        services.AddScoped<OrderCheckoutService>();
        services.AddScoped<OrderManagementService>();
        services.AddScoped<OrderQueryService>();

        // Public contracts consumed by other modules.
        services.AddScoped<IOrderWorkflow, OrderWorkflowService>();       // Payments (Phase 9)
        services.AddScoped<IOrderFulfillment>(sp => sp.GetRequiredService<OrderManagementService>()); // Shipping (Phase 10)

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<OrdersModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapOrderEndpoints();
}
