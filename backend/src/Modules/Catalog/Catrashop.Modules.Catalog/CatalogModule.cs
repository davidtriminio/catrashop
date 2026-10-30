using Catrashop.BuildingBlocks.Modularity;
using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Catalog.Api;
using Catrashop.Modules.Catalog.Application.Abstractions;
using Catrashop.Modules.Catalog.Application.Services;
using Catrashop.Modules.Catalog.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catrashop.Modules.Catalog;

public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<CatalogDbContext>(connectionString, "catalog");

        services.AddScoped<ICatalogUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICatalogReadStore, CatalogReadStore>();

        // Public in-process query consumed by the Cart module to capture prices at add-to-cart.
        services.AddScoped<Contracts.IProductCatalogQuery, ProductCatalogQuery>();

        services.AddScoped<CategoryService>();
        services.AddScoped<BrandService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CatalogQueryService>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<CatalogModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapCatalogEndpoints();
}
