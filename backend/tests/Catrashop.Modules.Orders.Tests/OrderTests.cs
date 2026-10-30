using Catrashop.Modules.Orders.Application.Contracts;
using Catrashop.Modules.Orders.Application.Validation;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Orders.Domain;
using Xunit;

namespace Catrashop.Modules.Orders.Tests;

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
    private const string Transfer = PaymentMethodCodes.BankTransfer;
    private const string PayAtStore = PaymentMethodCodes.PayAtStore;

    private static ShippingAddressRequest Address(string country = "HN", string? department = "HN-FM") =>
        new("Jane Doe", "Col. Palmira, Calle 5", null, "Tegucigalpa", null, "11101", country, department);

    [Fact]
    public void A_delivery_to_a_department_of_honduras_is_valid() =>
        Assert.True(Validator.Validate(new CheckoutRequest(Address(), PaymentMethod: Transfer)).IsValid);

    [Fact]
    public void A_delivery_needs_an_address_a_department_and_the_country_honduras()
    {
        Assert.False(Validator.Validate(new CheckoutRequest(null, PaymentMethod: Transfer)).IsValid);
        Assert.False(Validator.Validate(new CheckoutRequest(Address(department: null), PaymentMethod: Transfer)).IsValid);
        Assert.False(Validator.Validate(new CheckoutRequest(Address(country: "US"), PaymentMethod: Transfer)).IsValid);
    }

    [Fact]
    public void A_pickup_needs_a_store_and_the_name_of_who_collects_but_no_address()
    {
        var store = Guid.NewGuid();
        Assert.True(Validator.Validate(new CheckoutRequest(null, null, "Pickup", store, "Jane Doe", PaymentMethod: PayAtStore)).IsValid);
        Assert.False(Validator.Validate(new CheckoutRequest(null, null, "Pickup", null, "Jane Doe", PaymentMethod: PayAtStore)).IsValid);
        Assert.False(Validator.Validate(new CheckoutRequest(null, null, "Pickup", store, " ", PaymentMethod: PayAtStore)).IsValid);
    }

    [Theory]
    [InlineData("delivery", true)]
    [InlineData("PICKUP", true)]
    [InlineData("Courier", false)]
    [InlineData("", false)]
    public void The_delivery_mode_must_be_known(string mode, bool valid)
    {
        var request = new CheckoutRequest(Address(), null, mode, Guid.NewGuid(), "Jane Doe", Transfer);
        Assert.Equal(valid, Validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("LINKPAGO", false)]
    [InlineData("EFECTIVO", false)]
    [InlineData("CONTRA_ENTREGA", true)]
    [InlineData("transferencia", true)]
    [InlineData("PAGO_EN_TIENDA", true)]
    public void The_payment_method_is_required_and_must_be_one_we_offer(string? method, bool valid)
    {
        var request = new CheckoutRequest(Address(), PaymentMethod: method);
        Assert.Equal(valid, Validator.Validate(request).IsValid);
    }
}

public class PaymentMethodRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false, true, "CONTRA_ENTREGA,TRANSFERENCIA")]
    [InlineData(false, false, "TRANSFERENCIA")]
    [InlineData(true, false, "PAGO_EN_TIENDA,TRANSFERENCIA")]
    [InlineData(true, true, "PAGO_EN_TIENDA,TRANSFERENCIA")]
    public void The_delivery_option_decides_which_methods_are_offered(bool pickup, bool cashCovered, string expected) =>
        Assert.Equal(expected, string.Join(",", PaymentMethodCodes.AllowedFor(pickup, cashCovered)));

    [Fact]
    public void Link_payments_are_not_offered_for_now() =>
        Assert.DoesNotContain("LINKPAGO", PaymentMethodCodes.All);

    private static Order OrderPaidWith(string method)
    {
        var lines = new[] { (Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Widget", 100m, 1) };
        var address = new ShippingAddress("Jane Doe", "Calle 1", null, "Tegucigalpa", "Francisco Morazan", "11101", "HN");
        var order = Order.Create("ORD-51", Guid.NewGuid(), Guid.NewGuid(), "HNL", address, lines, 0m, 0m, Now, paymentMethod: method).Value;
        Assert.True(order.MarkPaymentPending(Now).IsSuccess);
        return order;
    }

    [Theory]
    [InlineData("CONTRA_ENTREGA")]
    [InlineData("PAGO_EN_TIENDA")]
    public void A_collected_later_order_is_confirmed_without_being_marked_paid(string method)
    {
        var order = OrderPaidWith(method);

        Assert.True(order.Confirm(Now).IsSuccess);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.False(order.HoldsReservation); // stock is committed, not merely reserved
        Assert.True(order.StartProcessing(Now).IsSuccess); // and it can be prepared and shipped
    }

    [Fact]
    public void A_transfer_order_cannot_be_confirmed_without_a_verified_payment()
    {
        var order = OrderPaidWith("TRANSFERENCIA");

        var result = order.Confirm(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.PaymentPending, order.Status);
        Assert.True(order.MarkPaid(Now).IsSuccess); // the only way in is a captured payment
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed, false)]
    [InlineData(OrderStatus.PaymentPending, OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Paid, false)]
    public void Confirmed_sits_between_payment_pending_and_processing(OrderStatus from, OrderStatus to, bool allowed) =>
        Assert.Equal(allowed, OrderStateMachine.CanTransition(from, to));
}

