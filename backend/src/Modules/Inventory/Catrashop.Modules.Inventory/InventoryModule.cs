using Catrashop.BuildingBlocks.Modularity;
using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Inventory.Api;
using Catrashop.Modules.Inventory.Application.Abstractions;
using Catrashop.Modules.Inventory.Application.Services;
using Catrashop.Modules.Inventory.Contracts;
using Catrashop.Modules.Inventory.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catrashop.Modules.Inventory;

public sealed class InventoryModule : IModule
{
    public string Name => "Inventory";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<InventoryDbContext>(connectionString, "inventory");

        services.AddScoped<IInventoryUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>());
        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddScoped<IInventoryReadStore, InventoryReadStore>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        services.AddScoped<InventoryAdminService>();
        services.AddScoped<InventoryQueryService>();

        // Public in-process contract consumed by Orders during checkout.
        services.AddScoped<IInventoryOperations, InventoryOperationsService>();
        services.AddScoped<IStockQuery, StockQueryService>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<InventoryModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapInventoryEndpoints();
}
