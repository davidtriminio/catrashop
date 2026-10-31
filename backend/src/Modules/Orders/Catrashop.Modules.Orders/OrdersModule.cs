using Catrashop.BuildingBlocks.Modularity;
using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Orders.Api;
using Catrashop.Modules.Orders.Application.Abstractions;
using Catrashop.Modules.Orders.Application.Configuration;
using Catrashop.Modules.Orders.Application.Services;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Orders.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catrashop.Modules.Orders;

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
        services.AddScoped<IOrderQueries>(sp => sp.GetRequiredService<OrderQueryService>()); // AI assistant (Phase 17)

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<OrdersModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapOrderEndpoints();
}
