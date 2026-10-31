using System.Reflection;
using Catrashop.BuildingBlocks.Messaging;
using Xunit;

namespace Catrashop.ArchitectureTests;

/// <summary>
/// Integration-event handlers are wiring: they translate one module's events into another module's
/// commands, so placing them inside a module would couple the two. The project keeps every handler in
/// the API composition root (<c>Catrashop.Api.Integration</c>). This rule enforces that no business
/// module carries a handler of its own.
/// </summary>
public sealed class CompositionRootTests
{
    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_should_not_implement_integration_event_handlers(string moduleName)
    {
        var module = ArchModules.All.Single(m => m.Name == moduleName);

        var handlers = module.Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(ImplementsIntegrationEventHandler)
            .Select(t => t.FullName)
            .ToArray();

        Assert.True(handlers.Length == 0,
            $"Module '{module.Name}' implements integration-event handlers ({string.Join(", ", handlers)}). " +
            "Handlers belong in the API composition root so modules stay decoupled.");
    }

    private static bool ImplementsIntegrationEventHandler(Type type) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));

    public static TheoryData<string> ModuleNames()
    {
        var data = new TheoryData<string>();
        foreach (var module in ArchModules.All)
            data.Add(module.Name);
        return data;
    }
}
