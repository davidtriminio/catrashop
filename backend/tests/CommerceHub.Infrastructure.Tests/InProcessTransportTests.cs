using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CommerceHub.Infrastructure.Tests;

public sealed record InProcessSampleEvent(string Note) : IntegrationEvent;

public sealed class InProcessCapturingHandler : IIntegrationEventHandler<InProcessSampleEvent>
{
    public static readonly List<string> Received = new();
    public Task HandleAsync(InProcessSampleEvent e, CancellationToken ct = default)
    {
        Received.Add(e.Note);
        return Task.CompletedTask;
    }
}

public sealed class FailingHandler : IIntegrationEventHandler<InProcessSampleEvent>
{
    public Task HandleAsync(InProcessSampleEvent e, CancellationToken ct = default) =>
        throw new InvalidOperationException("boom");
}

public class InProcessTransportTests
{
    private static IConfiguration Config(string? transport) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(transport is null ? [] : new Dictionary<string, string?> { ["Messaging:Transport"] = transport })
            .Build();

    [Theory]
    [InlineData("InProcess", true)]
    [InlineData("inprocess", true)]
    [InlineData("RabbitMq", false)]
    [InlineData(null, false)]
    public void Transport_selection_reads_configuration(string? value, bool expected) =>
        Assert.Equal(expected, MessagingRegistration.UsesInProcessTransport(Config(value)));

    [Fact]
    public void InProcess_registers_no_rabbit_bus()
    {
        var services = new ServiceCollection().AddSingleton(Config("InProcess")).AddMessaging(Config("InProcess"));
        Assert.Contains(services, d => d.ServiceType == typeof(IEventBus) && d.ImplementationType == typeof(InProcessEventBus));
        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(RabbitMqEventBus));
        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(IntegrationEventConsumerService));
    }

    [Fact]
    public async Task Publish_dispatches_directly_to_handlers()
    {
        InProcessCapturingHandler.Received.Clear();
        var registry = new IntegrationEventTypeRegistry();
        registry.Register<InProcessSampleEvent>();
        var services = new ServiceCollection()
            .AddSingleton(registry)
            .AddSingleton(NullLoggerFactory.Instance)
            .AddLogging()
            .AddSingleton<IntegrationEventDispatcher>()
            .AddScoped<IIntegrationEventHandler<InProcessSampleEvent>, InProcessCapturingHandler>()
            .BuildServiceProvider();
        var bus = new InProcessEventBus(services, services.GetRequiredService<IntegrationEventDispatcher>());

        var json = System.Text.Json.JsonSerializer.Serialize(new InProcessSampleEvent("hola"));
        await bus.PublishAsync(typeof(InProcessSampleEvent).FullName!, json, Guid.NewGuid(), null);

        Assert.Equal(["hola"], InProcessCapturingHandler.Received);
    }

    [Fact]
    public async Task Handler_failure_propagates_so_the_outbox_retries()
    {
        var registry = new IntegrationEventTypeRegistry();
        registry.Register<InProcessSampleEvent>();
        var services = new ServiceCollection()
            .AddSingleton(registry)
            .AddLogging()
            .AddSingleton<IntegrationEventDispatcher>()
            .AddScoped<IIntegrationEventHandler<InProcessSampleEvent>, FailingHandler>()
            .BuildServiceProvider();
        var bus = new InProcessEventBus(services, services.GetRequiredService<IntegrationEventDispatcher>());

        var json = System.Text.Json.JsonSerializer.Serialize(new InProcessSampleEvent("x"));
        await Assert.ThrowsAsync<System.Reflection.TargetInvocationException>(() =>
            bus.PublishAsync(typeof(InProcessSampleEvent).FullName!, json, Guid.NewGuid(), null));
    }
}
