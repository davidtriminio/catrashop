using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceHub.BuildingBlocks.Modularity;

/// <summary>
/// Registers a set of <see cref="IModule"/>s with the host. The Api composition root builds this
/// list explicitly (no assembly scanning magic) so the set of active modules is obvious and testable.
/// </summary>
public sealed class ModuleRegistry
{
    private readonly List<IModule> _modules = new();

    public IReadOnlyList<IModule> Modules => _modules;

    public ModuleRegistry Add(IModule module)
    {
        _modules.Add(module);
        return this;
    }

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        foreach (var module in _modules)
            module.RegisterServices(services, configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        foreach (var module in _modules)
            module.MapEndpoints(endpoints);
    }
}
