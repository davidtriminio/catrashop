using Catrashop.Modules.Shipping.Domain;
using Xunit;

namespace Catrashop.Modules.Shipping.Tests;

public class ShipmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    private static Shipment NewShipment() =>
        Shipment.Create(Guid.NewGuid(), "Catrashop Logistics", "CH0123456789AB", "standard", 5.00m, "USD",
            new Destination("Jane Doe", "1 Main St", null, "Springfield", "IL", "62704", "US"), Now);

    [Fact]
    public void New_shipment_is_pending_with_a_creation_event()
    {
        var s = NewShipment();

        Assert.Equal(ShipmentStatus.Pending, s.Status);
        Assert.Single(s.Events);
        Assert.Null(s.ShippedAtUtc);
    }

    [Fact]
    public void Happy_path_pending_to_shipped_to_delivered()
    {
        var s = NewShipment();

        Assert.True(s.MarkShipped(Now).IsSuccess);
        Assert.Equal(ShipmentStatus.Shipped, s.Status);
        Assert.NotNull(s.ShippedAtUtc);

        Assert.True(s.MarkDelivered(Now).IsSuccess);
        Assert.Equal(ShipmentStatus.Delivered, s.Status);
        Assert.NotNull(s.DeliveredAtUtc);
        Assert.Equal(3, s.Events.Count); // created + shipped + delivered
    }

    [Fact]
    public void Cannot_deliver_before_shipping()
    {
        var s = NewShipment();

        var result = s.MarkDelivered(Now);

        Assert.True(result.IsFailure);
        Assert.Equal("shipping.invalid_transition", result.Error.Code);
        Assert.Equal(ShipmentStatus.Pending, s.Status);
    }

    [Fact]
    public void Cannot_cancel_after_shipping()
    {
        var s = NewShipment();
        s.MarkShipped(Now);

        Assert.True(s.Cancel("oops", Now).IsFailure);
        Assert.Equal(ShipmentStatus.Shipped, s.Status);
    }

    [Fact]
    public void Pending_can_be_cancelled()
    {
        var s = NewShipment();

        Assert.True(s.Cancel("out of stock", Now).IsSuccess);
        Assert.Equal(ShipmentStatus.Cancelled, s.Status);
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending, ShipmentStatus.Shipped, true)]
    [InlineData(ShipmentStatus.Shipped, ShipmentStatus.Delivered, true)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Shipped, false)]
    [InlineData(ShipmentStatus.Cancelled, ShipmentStatus.Shipped, false)]
    public void State_machine_allows_only_defined_transitions(ShipmentStatus from, ShipmentStatus to, bool allowed) =>
        Assert.Equal(allowed, ShipmentStateMachine.CanTransition(from, to));

    [Fact]
    public void Shipping_method_create_normalizes_code_and_currency()
    {
        var m = ShippingMethod.Create("  Express ", "Express (2 day)", 12.5m, "usd", 2, Now);

        Assert.Equal("express", m.Code);
        Assert.Equal("USD", m.Currency);
        Assert.True(m.IsActive);
    }
}
