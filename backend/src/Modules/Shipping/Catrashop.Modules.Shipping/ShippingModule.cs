using Catrashop.BuildingBlocks.Modularity;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Shipping.Api;
using Catrashop.Modules.Shipping.Application.Abstractions;
using Catrashop.Modules.Shipping.Application.Services;
using Catrashop.Modules.Shipping.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catrashop.Modules.Shipping;

public sealed class ShippingModule : IModule
{
    public string Name => "Shipping";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<ShippingDbContext>(connectionString, "shipping");

        services.AddScoped<IShippingUnitOfWork>(sp => sp.GetRequiredService<ShippingDbContext>());
        services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IShippingZoneRepository, ShippingZoneRepository>();
        services.AddScoped<IPickupStoreRepository, PickupStoreRepository>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        services.AddScoped<ShippingMethodService>();
        services.AddScoped<ShipmentService>();
        services.AddScoped<ShippingZoneService>();
        services.AddScoped<IShippingQuotation>(sp => sp.GetRequiredService<ShippingZoneService>());

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<ShippingModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapShippingEndpoints();
}
