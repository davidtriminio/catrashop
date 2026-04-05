using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Identity.Api;
using CommerceHub.Modules.Identity.Application.Abstractions;
using CommerceHub.Modules.Identity.Application.Authentication;
using CommerceHub.Modules.Identity.Application.Configuration;
using CommerceHub.Modules.Identity.Application.Passwords;
using CommerceHub.Modules.Identity.Application.Users;
using CommerceHub.Modules.Identity.Infrastructure.Persistence;
using CommerceHub.Modules.Identity.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommerceHub.Modules.Identity;

/// <summary>
/// Composition of the Identity module: options, persistence, security services, application services,
/// validators, and HTTP endpoints. The API host only knows this <see cref="IModule"/>.
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey) && o.SigningKey.Length >= 32,
                "Identity:Jwt:SigningKey must be configured with at least 32 characters.")
            .ValidateOnStart();

        services.Configure<IdentitySecurityOptions>(configuration.GetSection(IdentitySecurityOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddModuleDbContext<IdentityDbContext>(connectionString, "identity");

        // Application services (scoped: one unit of work per request).
        services.AddScoped<AuthenticationService>();
        services.AddScoped<PasswordRecoveryService>();
        services.AddScoped<UserQueryService>();

        // Persistence & infrastructure.
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
        services.AddScoped<ISecurityAuditLogger, SecurityAuditLogger>();

        // Security primitives (stateless → singletons).
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssemblyContaining<IdentityModule>(ServiceLifetime.Scoped);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapIdentityEndpoints();
}
