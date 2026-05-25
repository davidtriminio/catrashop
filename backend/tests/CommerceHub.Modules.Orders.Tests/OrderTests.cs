using CommerceHub.Modules.Orders.Domain;
using Xunit;

namespace CommerceHub.Modules.Orders.Tests;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    private static ShippingAddress Address() =>
        new("Jane Doe", "1 Main St", null, "Springfield", "IL", "62704", "US");

    private static Order NewOrder(decimal unitPrice = 10m, int qty = 2, decimal tax = 4m, decimal discount = 0m)
    {
        var lines = new[] { (Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Widget", unitPrice, qty) };
        var result = Order.Create("ORD-1", Guid.NewGuid(), Guid.NewGuid(), "USD", Address(), lines, discount, tax, Now);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    // --- Totals ---------------------------------------------------------------------------------

    [Fact]
    public void Totals_are_subtotal_minus_discount_plus_tax()
    {
        var order = NewOrder(unitPrice: 10m, qty: 3, tax: 3m, discount: 5m);

        Assert.Equal(30m, order.Subtotal);
        Assert.Equal(28m, order.GrandTotal); // 30 - 5 + 3
    }

    [Fact]
    public void Create_with_no_lines_fails()
    {
        var result = Order.Create("ORD-2", Guid.NewGuid(), Guid.NewGuid(), "USD", Address(),
            Array.Empty<(Guid, Guid, string, string, decimal, int)>(), 0m, 0m, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("orders.no_lines", result.Error.Code);
    }

    [Fact]
    public void New_order_starts_pending_with_history()
    {
        var order = NewOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(order.StatusHistory);
        Assert.True(order.HoldsReservation);
    }

    // --- State machine: the happy path ----------------------------------------------------------

    [Fact]
    public void Full_lifecycle_transitions_succeed_in_order()
    {
        var order = NewOrder();

        Assert.True(order.MarkPaymentPending(Now).IsSuccess);
        Assert.True(order.MarkPaid(Now).IsSuccess);
        Assert.True(order.StartProcessing(Now).IsSuccess);
        Assert.True(order.MarkShipped(Now).IsSuccess);
        Assert.True(order.MarkDelivered(Now).IsSuccess);

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(6, order.StatusHistory.Count); // Pending + 5 transitions
        Assert.False(order.HoldsReservation); // stock committed after payment
    }

    // --- State machine: illegal transitions are rejected ----------------------------------------

    [Fact]
    public void A_paid_order_cannot_go_back_to_pending()
    {
        var order = NewOrder();
        order.MarkPaymentPending(Now);
        order.MarkPaid(Now);

        var result = order.MarkPaymentPending(Now);

        Assert.True(result.IsFailure);
        Assert.Equal("orders.invalid_transition", result.Error.Code);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void An_unpaid_order_cannot_ship()
    {
        var order = NewOrder();
        order.MarkPaymentPending(Now);

        var result = order.MarkShipped(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.PaymentPending, order.Status);
    }

    [Fact]
    public void A_cancelled_order_is_terminal()
    {
        var order = NewOrder();
        Assert.True(order.Cancel("changed mind", Now).IsSuccess);

        Assert.True(OrderStateMachine.IsTerminal(OrderStatus.Cancelled));
        Assert.True(order.MarkPaymentPending(Now).IsFailure);
    }

    [Fact]
    public void Payment_can_be_retried_after_failure()
    {
        var order = NewOrder();
        order.MarkPaymentPending(Now);
        Assert.True(order.MarkPaymentFailed("declined", Now).IsSuccess);

        // Failure keeps the reservation and allows another attempt.
        Assert.True(order.HoldsReservation);
        Assert.True(order.MarkPaymentPending(Now).IsSuccess);
    }

    [Theory]
    [InlineData(OrderStatus.Paid, OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Refunded, OrderStatus.Paid, false)]
    public void State_machine_allows_only_defined_transitions(OrderStatus from, OrderStatus to, bool allowed) =>
        Assert.Equal(allowed, OrderStateMachine.CanTransition(from, to));
}
