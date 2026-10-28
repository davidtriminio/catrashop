using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Cart.Api;
using CommerceHub.Modules.Cart.Application.Abstractions;
using CommerceHub.Modules.Cart.Application.Services;
using CommerceHub.Modules.Cart.Contracts;
using CommerceHub.Modules.Cart.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommerceHub.Modules.Cart;

public sealed class CartModule : IModule
{
    public string Name => "Cart";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<CartDbContext>(connectionString, "cart");

        services.AddScoped<ICartUnitOfWork>(sp => sp.GetRequiredService<CartDbContext>());
        services.AddScoped<ICartRepository, CartRepository>();

        services.Configure<CartOptions>(configuration.GetSection(CartOptions.SectionName));
        services.AddScoped<CartService>();
        // Public contract consumed by Orders during checkout.
        services.AddScoped<ICartQuery>(sp => sp.GetRequiredService<CartService>());

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<CartModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapCartEndpoints();
}
