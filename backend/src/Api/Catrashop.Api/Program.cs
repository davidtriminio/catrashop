using System.Text;
using Catrashop.Api.Integration;
using Catrashop.Api.Observability;
using Catrashop.Api.Performance;
using Catrashop.Api.Security;
using Catrashop.BuildingBlocks.Modularity;
using Catrashop.BuildingBlocks.Security;
using Catrashop.Infrastructure.Messaging;
using Catrashop.Modules.Cart;
using Catrashop.Modules.Catalog;
using Catrashop.Modules.Customers;
using Catrashop.Modules.Identity;
using Catrashop.Modules.Inventory;
using Catrashop.Modules.Notifications;
using Catrashop.Modules.Orders;
using Catrashop.Modules.Payments;
using Catrashop.Modules.Promotions;
using Catrashop.Modules.Shipping;
using Catrashop.Modules.AI;
using Catrashop.Modules.Identity.Application.Configuration;
using Catrashop.Modules.Identity.Infrastructure.Security;
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
builder.AddCatrashopObservability();

// --- Security hardening: headers, CORS, rate limiting, secret guard (Phase 20) ---------------
builder.AddCatrashopSecurity();
builder.Services.Configure<Catrashop.BuildingBlocks.Money.StoreOptions>(builder.Configuration.GetSection(Catrashop.BuildingBlocks.Money.StoreOptions.SectionName));

// --- Performance: response compression + output caching for anonymous reads (Phase 22) -------
builder.AddCatrashopPerformance();

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
builder.Services.AddCatrashopMessaging(builder.Configuration);

var app = builder.Build();

// Make every integration-event type known to the consumer (routing keys + deserialization).
IntegrationSetup.RegisterIntegrationEventTypes(app.Services.GetRequiredService<IntegrationEventTypeRegistry>());

// --- Pipeline --------------------------------------------------------------------------------
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Security hardening pipeline (headers, optional HTTPS/HSTS, CORS, rate limiting) — Phase 20.
app.UseCatrashopSecurity();

// Performance pipeline (response compression outside output caching) — Phase 22. Placed before auth so an
// anonymous catalog cache hit short-circuits before the auth/authz work runs.
app.UseCatrashopPerformance();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCatrashopObservability();

app.MapGet("/", () => Results.Ok(new
{
    service = "Catrashop API",
    status = "ok",
    modules = modules.Modules.Select(m => m.Name).ToArray()
}));

modules.MapEndpoints(app);

app.Run();

// Exposed so integration tests (WebApplicationFactory) can reference the entry point.
public partial class Program;
