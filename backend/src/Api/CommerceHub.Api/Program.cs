using CommerceHub.BuildingBlocks.Modularity;

var builder = WebApplication.CreateBuilder(args);

// --- Module registry: the explicit list of modules composing this monolith. -----------------
// Modules are added here as each phase lands (Identity, Catalog, Inventory, ...). Keeping the
// list explicit (rather than assembly scanning) makes the active surface obvious and testable.
var modules = new ModuleRegistry();

// --- Cross-cutting services ------------------------------------------------------------------
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// --- Per-module services ---------------------------------------------------------------------
modules.RegisterServices(builder.Services, builder.Configuration);

var app = builder.Build();

// --- Pipeline --------------------------------------------------------------------------------
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Liveness: the process is up. Readiness: dependencies (DB, broker) are reachable — wired as
// those dependencies come online in later phases.
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapGet("/", () => Results.Ok(new
{
    service = "CommerceHub API",
    status = "ok",
    modules = modules.Modules.Select(m => m.Name).ToArray()
}));

// --- Per-module endpoints --------------------------------------------------------------------
modules.MapEndpoints(app);

app.Run();

// Exposed so integration tests (WebApplicationFactory) can reference the entry point.
public partial class Program;
