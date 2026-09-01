using System.Text;
using CommerceHub.Api.Integration;
using CommerceHub.Api.Observability;
using CommerceHub.Api.Security;
using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.BuildingBlocks.Security;
using CommerceHub.Infrastructure.Messaging;
using CommerceHub.Modules.Cart;
using CommerceHub.Modules.Catalog;
using CommerceHub.Modules.Customers;
using CommerceHub.Modules.Identity;
using CommerceHub.Modules.Inventory;
using CommerceHub.Modules.Notifications;
using CommerceHub.Modules.Orders;
using CommerceHub.Modules.Payments;
using CommerceHub.Modules.Promotions;
using CommerceHub.Modules.Shipping;
using CommerceHub.Modules.AI;
using CommerceHub.Modules.Identity.Application.Configuration;
using CommerceHub.Modules.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Module registry: the explicit list of modules composing this monolith. -----------------
var modules = new ModuleRegistry()
    .Add(new IdentityModule())
    .Add(new CatalogModule())
    .Add(new InventoryModule())
    .Add(new CustomersModule())
    .Add(new CartModule())
    .Add(new PromotionsModule())
    .Add(new OrdersModule())
    .Add(new PaymentsModule())
    .Add(new ShippingModule())
    .Add(new NotificationsModule())
    .Add(new AiModule());

// --- Observability: structured logs, traces, metrics, health probes (Phase 19) --------------
builder.AddCommerceHubObservability();

// --- Security hardening: headers, CORS, rate limiting, secret guard (Phase 20) ---------------
builder.AddCommerceHubSecurity();

// --- Cross-cutting services ------------------------------------------------------------------
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// --- Authentication (JWT bearer) -------------------------------------------------------------
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep claim types verbatim (sub, email, role, permission)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                string.IsNullOrWhiteSpace(jwt.SigningKey) ? new string('0', 32) : jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = JwtTokenGenerator.RoleClaimType,
            NameClaimType = "sub"
        };
    });

// --- Authorization (permission-based, on-demand policies) ------------------------------------
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// --- Per-module services ---------------------------------------------------------------------
modules.RegisterServices(builder.Services, builder.Configuration);

// --- Messaging: Outbox → RabbitMQ → in-process handlers (Phase 15) ---------------------------
builder.Services.AddCommerceHubMessaging(builder.Configuration);

var app = builder.Build();

// Make every integration-event type known to the consumer (routing keys + deserialization).
IntegrationSetup.RegisterIntegrationEventTypes(app.Services.GetRequiredService<IntegrationEventTypeRegistry>());

// --- Pipeline --------------------------------------------------------------------------------
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Security hardening pipeline (headers, optional HTTPS/HSTS, CORS, rate limiting) — Phase 20.
app.UseCommerceHubSecurity();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCommerceHubObservability();

app.MapGet("/", () => Results.Ok(new
{
    service = "CommerceHub API",
    status = "ok",
    modules = modules.Modules.Select(m => m.Name).ToArray()
}));

modules.MapEndpoints(app);

app.Run();

// Exposed so integration tests (WebApplicationFactory) can reference the entry point.
public partial class Program;
