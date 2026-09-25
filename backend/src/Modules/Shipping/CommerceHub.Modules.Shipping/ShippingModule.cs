using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Shipping.Api;
using CommerceHub.Modules.Shipping.Application.Abstractions;
using CommerceHub.Modules.Shipping.Application.Services;
using CommerceHub.Modules.Shipping.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommerceHub.Modules.Shipping;

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
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        services.AddScoped<ShippingMethodService>();
        services.AddScoped<ShipmentService>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<ShippingModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapShippingEndpoints();
}
