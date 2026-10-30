using Catrashop.BuildingBlocks.Modularity;
using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Promotions.Api;
using Catrashop.Modules.Promotions.Application.Abstractions;
using Catrashop.Modules.Promotions.Application.Services;
using Catrashop.Modules.Promotions.Contracts;
using Catrashop.Modules.Promotions.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catrashop.Modules.Promotions;

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
