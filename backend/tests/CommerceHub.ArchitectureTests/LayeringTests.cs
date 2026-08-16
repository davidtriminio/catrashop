using NetArchTest.Rules;
using Xunit;

namespace CommerceHub.ArchitectureTests;

/// <summary>
/// Within a module the dependency direction is inward: Domain knows nothing of Application,
/// Infrastructure, or Api; Application never takes a hard dependency on the persistence driver. These
/// rules keep the domain model free of framework and persistence concerns.
/// </summary>
public sealed class LayeringTests
{
    private const string EfCore = "Microsoft.EntityFrameworkCore";
    private const string Npgsql = "Npgsql";

    public static TheoryData<string> ModuleNames()
    {
        var data = new TheoryData<string>();
        foreach (var module in ArchModules.All)
            data.Add(module.Name);
        return data;
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Domain_should_not_depend_on_outer_layers(string moduleName)
    {
        var module = ArchModules.All.Single(m => m.Name == moduleName);

        var result = Types.InAssembly(module.Assembly)
            .That().ResideInNamespaceStartingWith(module.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(module.Application, module.Infrastructure, module.Api)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"'{module.Name}' Domain depends on an outer layer: {FormatFailures(result)}.");
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Domain_should_not_depend_on_persistence_frameworks(string moduleName)
    {
        var module = ArchModules.All.Single(m => m.Name == moduleName);

        var result = Types.InAssembly(module.Assembly)
            .That().ResideInNamespaceStartingWith(module.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(EfCore, Npgsql)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"'{module.Name}' Domain depends on EF Core / Npgsql: {FormatFailures(result)}. " +
            "Persistence is an Infrastructure concern.");
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Application_should_not_depend_on_the_persistence_driver(string moduleName)
    {
        var module = ArchModules.All.Single(m => m.Name == moduleName);

        // EF Core abstractions may surface via a DbContext-backed unit of work, but the concrete
        // PostgreSQL driver must stay behind the Infrastructure layer.
        var result = Types.InAssembly(module.Assembly)
            .That().ResideInNamespaceStartingWith(module.Application)
            .ShouldNot()
            .HaveDependencyOn(Npgsql)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"'{module.Name}' Application depends on Npgsql directly: {FormatFailures(result)}.");
    }

    private static string FormatFailures(TestResult result) =>
        result.FailingTypeNames is { Count: > 0 }
            ? string.Join(", ", result.FailingTypeNames)
            : "(no failing types reported)";
}
