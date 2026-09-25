using NetArchTest.Rules;
using Xunit;

namespace CommerceHub.ArchitectureTests;

/// <summary>
/// The defining constraint of the modular monolith: modules communicate only through published
/// <c>Contracts</c> (in-process) and integration events — never by reaching into another module's
/// Domain, Application, Infrastructure, or Api internals. These rules make that boundary executable.
/// </summary>
public sealed class ModuleBoundaryTests
{
    public static TheoryData<string> ModuleNames()
    {
        var data = new TheoryData<string>();
        foreach (var module in ArchModules.All)
            data.Add(module.Name);
        return data;
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_should_not_depend_on_another_modules_internals(string moduleName)
    {
        var module = ArchModules.All.Single(m => m.Name == moduleName);

        // Everything another module keeps private: its Domain/Application/Infrastructure/Api layers.
        var forbidden = ArchModules.All
            .Where(other => other.Name != module.Name)
            .SelectMany(other => other.InternalNamespaces)
            .ToArray();

        var result = Types.InAssembly(module.Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Module '{module.Name}' reaches into another module's internals: " +
            $"{FormatFailures(result)}. Cross-module access must go through Contracts or integration events.");
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_should_not_depend_on_the_Api_host(string moduleName)
    {
        var module = ArchModules.All.Single(m => m.Name == moduleName);

        var result = Types.InAssembly(module.Assembly)
            .ShouldNot()
            .HaveDependencyOn(ArchModules.ApiHostNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Module '{module.Name}' depends on the API composition root ('{ArchModules.ApiHostNamespace}'): " +
            $"{FormatFailures(result)}. Modules must not know about the host that composes them.");
    }

    private static string FormatFailures(TestResult result) =>
        result.FailingTypeNames is { Count: > 0 }
            ? string.Join(", ", result.FailingTypeNames)
            : "(no failing types reported)";
}
