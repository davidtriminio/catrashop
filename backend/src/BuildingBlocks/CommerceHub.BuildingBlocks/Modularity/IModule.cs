using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceHub.BuildingBlocks.Modularity;

/// <summary>
/// Contract every module implements so the composition root can discover and wire it without
/// referencing the module's internals. This is the seam that keeps the monolith modular: the Api
/// host knows only <see cref="IModule"/>, never a module's domain or infrastructure types.
/// </summary>
public interface IModule
{
    /// <summary>Stable module name, used in logs, health checks and diagnostics.</summary>
    string Name { get; }

    /// <summary>Register the module's services into the shared container.</summary>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Map the module's HTTP endpoints. Called once during startup.</summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
