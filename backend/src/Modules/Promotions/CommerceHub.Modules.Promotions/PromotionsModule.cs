using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Promotions.Api;
using CommerceHub.Modules.Promotions.Application.Abstractions;
using CommerceHub.Modules.Promotions.Application.Services;
using CommerceHub.Modules.Promotions.Contracts;
using CommerceHub.Modules.Promotions.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommerceHub.Modules.Promotions;

public sealed class PromotionsModule : IModule
{
    public string Name => "Promotions";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<PromotionsDbContext>(connectionString, "promotions");

        services.AddScoped<IPromotionsUnitOfWork>(sp => sp.GetRequiredService<PromotionsDbContext>());
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<ICouponRedemptionRepository, CouponRedemptionRepository>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        services.AddScoped<CouponAdminService>();

        // Public contract consumed by Orders at checkout / cancel / refund.
        services.AddScoped<IPromotionApplication, PromotionApplicationService>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<PromotionsModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapPromotionEndpoints();
}
