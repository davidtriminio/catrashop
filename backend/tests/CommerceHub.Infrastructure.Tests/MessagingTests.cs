using System.Text.Json;
using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CommerceHub.Infrastructure.Tests;

// A sample event + handler local to the tests.
public sealed record SampleIntegrationEvent(Guid OrderId, string Note) : IntegrationEvent;

public sealed class CapturingHandler : IIntegrationEventHandler<SampleIntegrationEvent>
{
    public static readonly List<SampleIntegrationEvent> Received = new();
    public Task HandleAsync(SampleIntegrationEvent e, CancellationToken ct = default)
    {
        Received.Add(e);
        return Task.CompletedTask;
    }
}

public class TypeRegistryTests
{
    [Fact]
    public void Registers_and_resolves_by_full_name()
    {
        var registry = new IntegrationEventTypeRegistry();
        registry.Register<SampleIntegrationEvent>();

        Assert.True(registry.TryResolve(typeof(SampleIntegrationEvent).FullName!, out var type));
        Assert.Equal(typeof(SampleIntegrationEvent), type);
        Assert.Contains(typeof(SampleIntegrationEvent).FullName!, registry.Names);
    }

    [Fact]
    public void Unknown_name_does_not_resolve()
    {
        var registry = new IntegrationEventTypeRegistry();
        Assert.False(registry.TryResolve("Nope.Missing", out _));
    }

    [Fact]
    public void Rejects_non_event_types()
    {
        var registry = new IntegrationEventTypeRegistry();
        Assert.Throws<ArgumentException>(() => registry.Register(typeof(string)));
    }
}

public class DispatcherTests
{
    private static IntegrationEventDispatcher NewDispatcher(IntegrationEventTypeRegistry registry) =>
        new(registry, NullLogger<IntegrationEventDispatcher>.Instance);

    [Fact]
    public async Task Dispatches_a_known_event_to_its_handler()
    {
        CapturingHandler.Received.Clear();
        var registry = new IntegrationEventTypeRegistry();
        registry.Register<SampleIntegrationEvent>();

        var provider = new ServiceCollection()
            .AddScoped<IIntegrationEventHandler<SampleIntegrationEvent>, CapturingHandler>()
            .BuildServiceProvider();

        var evt = new SampleIntegrationEvent(Guid.NewGuid(), "hello");
        // Serialized exactly as the outbox publisher does (default options).
        var json = JsonSerializer.Serialize(evt, evt.GetType());

        await NewDispatcher(registry).DispatchAsync(typeof(SampleIntegrationEvent).FullName!, json, provider, default);

        Assert.Single(CapturingHandler.Received);
        Assert.Equal(evt.OrderId, CapturingHandler.Received[0].OrderId);
        Assert.Equal("hello", CapturingHandler.Received[0].Note);
    }

    [Fact]
    public async Task Ignores_an_unknown_event_type_without_throwing()
    {
        var registry = new IntegrationEventTypeRegistry(); // nothing registered
        var provider = new ServiceCollection().BuildServiceProvider();

        // Should simply no-op (logged), not throw.
        await NewDispatcher(registry).DispatchAsync("Unknown.Type", "{}", provider, default);
    }

    [Fact]
    public async Task Known_event_with_no_handler_is_a_no_op()
    {
        var registry = new IntegrationEventTypeRegistry();
        registry.Register<SampleIntegrationEvent>();
        var provider = new ServiceCollection().BuildServiceProvider();

        var json = JsonSerializer.Serialize(new SampleIntegrationEvent(Guid.NewGuid(), "x"), typeof(SampleIntegrationEvent));
        await NewDispatcher(registry).DispatchAsync(typeof(SampleIntegrationEvent).FullName!, json, provider, default);
    }
}
