using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Payments.Api;
using CommerceHub.Modules.Payments.Application.Abstractions;
using CommerceHub.Modules.Payments.Application.Configuration;
using CommerceHub.Modules.Payments.Application.Services;
using CommerceHub.Modules.Payments.Infrastructure;
using CommerceHub.Modules.Payments.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommerceHub.Modules.Payments;

public sealed class PaymentsModule : IModule
{
    public string Name => "Payments";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymentsOptions>(configuration.GetSection(PaymentsOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<PaymentsDbContext>(connectionString, "payments");

        services.AddScoped<IPaymentUnitOfWork>(sp => sp.GetRequiredService<PaymentsDbContext>());
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        // Provider + signature verifier are stateless. Selecting among multiple providers by
        // PaymentsOptions.Provider is a future extension; today the simulated provider is the only one.
        services.AddSingleton<IPaymentProvider, FakePaymentProvider>();
        services.AddSingleton<IWebhookSignatureVerifier, HmacWebhookSignatureVerifier>();

        services.AddScoped<PaymentService>();
        services.AddScoped<WebhookService>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<PaymentsModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapPaymentEndpoints();
}
