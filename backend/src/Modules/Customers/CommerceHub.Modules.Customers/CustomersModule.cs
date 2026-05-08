using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Customers.Api;
using CommerceHub.Modules.Customers.Application.Abstractions;
using CommerceHub.Modules.Customers.Application.Services;
using CommerceHub.Modules.Customers.Contracts;
using CommerceHub.Modules.Customers.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommerceHub.Modules.Customers;

public sealed class CustomersModule : IModule
{
    public string Name => "Customers";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<CustomersDbContext>(connectionString, "customers");

        services.AddScoped<ICustomerUnitOfWork>(sp => sp.GetRequiredService<CustomersDbContext>());
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddScoped<CustomerService>();
        services.AddScoped<ICustomerProvisioning>(sp => sp.GetRequiredService<CustomerService>());
        services.AddScoped<AddressService>();
        services.AddScoped<WishlistService>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<CustomersModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapCustomerEndpoints();
}
