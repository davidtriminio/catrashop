using CommerceHub.Modules.Orders.Application.Contracts;
using CommerceHub.Modules.Orders.Application.Validation;
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

    // --- National shipping ------------------------------------------------------------------------

    private static Order OrderWith(OrderDelivery delivery, decimal tax = 4m)
    {
        var lines = new[] { (Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Widget", 100m, 2) };
        var result = Order.Create("ORD-9", Guid.NewGuid(), Guid.NewGuid(), "HNL", Address(), lines, 10m, tax, Now, delivery);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public void Shipping_is_added_to_the_grand_total_and_is_not_taxed()
    {
        var order = OrderWith(new OrderDelivery(DeliveryMode.Delivery, "HN-FM", null, null, 30m));

        Assert.Equal(DeliveryMode.Delivery, order.DeliveryMode);
        Assert.Equal("HN-FM", order.DepartmentCode);
        Assert.Equal(30m, order.ShippingTotal);
        Assert.Equal(224m, order.GrandTotal); // 200 - 10 + 4 tax + 30 shipping
    }

    [Fact]
    public void A_pickup_order_keeps_its_store_and_costs_nothing_to_ship()
    {
        var storeId = Guid.NewGuid();
        var order = OrderWith(new OrderDelivery(DeliveryMode.Pickup, "HN-FM", storeId, "Tienda Centro", 0m));

        Assert.Equal(DeliveryMode.Pickup, order.DeliveryMode);
        Assert.Equal(storeId, order.PickupStoreId);
        Assert.Equal("Tienda Centro", order.PickupStoreName);
        Assert.Equal(194m, order.GrandTotal);
    }

    [Fact]
    public void An_order_created_without_delivery_information_has_no_shipping_charge()
    {
        var order = NewOrder(unitPrice: 10m, qty: 2, tax: 4m);

        Assert.Equal(DeliveryMode.Delivery, order.DeliveryMode);
        Assert.Equal(0m, order.ShippingTotal);
        Assert.Equal(24m, order.GrandTotal);
    }
}

public class CheckoutRequestValidationTests
{
    private static readonly CheckoutRequestValidator Validator = new();

    private static ShippingAddressRequest Address(string country = "HN", string? department = "HN-FM") =>
        new("Jane Doe", "Col. Palmira, Calle 5", null, "Tegucigalpa", null, "11101", country, department);

    [Fact]
    public void A_delivery_to_a_department_of_honduras_is_valid() =>
        Assert.True(Validator.Validate(new CheckoutRequest(Address())).IsValid);

    [Fact]
    public void A_delivery_needs_an_address_a_department_and_the_country_honduras()
    {
        Assert.False(Validator.Validate(new CheckoutRequest(null)).IsValid);
        Assert.False(Validator.Validate(new CheckoutRequest(Address(department: null))).IsValid);
        Assert.False(Validator.Validate(new CheckoutRequest(Address(country: "US"))).IsValid);
    }

    [Fact]
    public void A_pickup_needs_a_store_and_the_name_of_who_collects_but_no_address()
    {
        var store = Guid.NewGuid();
        Assert.True(Validator.Validate(new CheckoutRequest(null, null, "Pickup", store, "Jane Doe")).IsValid);
        Assert.False(Validator.Validate(new CheckoutRequest(null, null, "Pickup", null, "Jane Doe")).IsValid);
        Assert.False(Validator.Validate(new CheckoutRequest(null, null, "Pickup", store, " ")).IsValid);
    }

    [Theory]
    [InlineData("delivery", true)]
    [InlineData("PICKUP", true)]
    [InlineData("Courier", false)]
    [InlineData("", false)]
    public void The_delivery_mode_must_be_known(string mode, bool valid)
    {
        var request = new CheckoutRequest(Address(), null, mode, Guid.NewGuid(), "Jane Doe");
        Assert.Equal(valid, Validator.Validate(request).IsValid);
    }
}
