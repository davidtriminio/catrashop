using Catrashop.BuildingBlocks.Messaging;
using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Orders.Contracts;

/// <summary>
/// Public, in-process contract the Payments module calls after processing a payment: confirm captures the
/// order (Paid + commit reserved stock) and fail marks it PaymentFailed. Idempotent — a repeated confirm on an
/// already-paid order is a no-op success.
/// </summary>
public interface IOrderWorkflow
{
    /// <summary>What the Payments module needs to charge an order: amount, currency, owner, and whether it awaits payment.</summary>
    Task<PayableOrder?> GetPayableAsync(Guid orderId, CancellationToken ct = default);

    Task<Result> ConfirmPaymentAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> FailPaymentAsync(Guid orderId, string? reason, CancellationToken ct = default);

    /// <summary>Move a paid/fulfilling order through RefundRequested → Refunded after a payment refund. Idempotent.</summary>
    Task<Result> RefundAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Accept an order whose money is collected later (cash on delivery, payment in the store): it moves to
    /// Confirmed and its reserved stock is committed, but it is NOT marked paid. Idempotent.
    /// </summary>
    Task<Result> ConfirmWithoutPaymentAsync(Guid orderId, CancellationToken ct = default);
}

/// <summary>
/// What the Payments module needs to charge an order. <paramref name="PaymentMethod"/> is the code the customer chose
/// at checkout (see <see cref="PaymentMethodCodes"/>), or null for an order created before payment methods existed.
/// </summary>
public sealed record PayableOrder(
    Guid OrderId, Guid CustomerId, string Currency, decimal Amount, bool IsAwaitingPayment, bool IsPaid,
    string Status = "", string? PaymentMethod = null);

/// <summary>
/// The payment methods a customer can choose, as codes shared by Orders (which validates the choice against the
/// delivery mode) and Payments (which runs the matching flow). <c>LINKPAGO</c> is intentionally absent for now.
/// </summary>
public static class PaymentMethodCodes
{
    public const string CashOnDelivery = "CONTRA_ENTREGA";
    public const string BankTransfer = "TRANSFERENCIA";
    public const string PayAtStore = "PAGO_EN_TIENDA";

    public static readonly IReadOnlyList<string> All = [CashOnDelivery, BankTransfer, PayAtStore];

    /// <summary>True for methods where the money is collected after the order is accepted (no receipt, no gateway).</summary>
    public static bool IsCollectedLater(string? code) => code is CashOnDelivery or PayAtStore;

    /// <summary>
    /// The methods allowed for a delivery mode. Home delivery: transfer, plus cash on delivery only where the
    /// destination zone covers it. Store pickup: pay at the store, or transfer. Never depends on the shipping cost.
    /// </summary>
    public static IReadOnlyList<string> AllowedFor(bool isPickup, bool cashOnDeliveryCovered) =>
        isPickup
            ? [PayAtStore, BankTransfer]
            : cashOnDeliveryCovered ? [CashOnDelivery, BankTransfer] : [BankTransfer];
}

/// <summary>
/// Public, in-process contract the Shipping module calls to read an order's destination and drive its
/// fulfillment transitions (Processing → Shipped → Delivered), each validated by the order state machine.
/// </summary>
public interface IOrderFulfillment
{
    Task<FulfillableOrder?> GetFulfillmentAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> StartProcessingAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> MarkShippedAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> MarkDeliveredAsync(Guid orderId, CancellationToken ct = default);
}

public sealed record FulfillableOrder(
    Guid OrderId, Guid CustomerId, string Status,
    string RecipientName, string Line1, string? Line2, string City, string? Region, string PostalCode, string Country);

/// <summary>
/// Public, in-process contract the Orders module calls at checkout to price a delivery or resolve a pickup store.
/// Defined here and implemented by the Shipping module (which already depends on Orders), so Orders never has to
/// reference Shipping. The cost always comes from the destination zone — never from the payment method or the client.
/// </summary>
public interface IShippingQuotation
{
    /// <summary>The cost and lead time of delivering to a department, or null when no active zone serves it.</summary>
    Task<DeliveryQuote?> QuoteDeliveryAsync(string departmentCode, CancellationToken ct = default);

    /// <summary>An active pickup store, or null when it does not exist or is not enabled.</summary>
    Task<PickupStoreInfo?> GetPickupStoreAsync(Guid storeId, CancellationToken ct = default);
}

public sealed record DeliveryQuote(
    string DepartmentCode, string DepartmentName, string ZoneCode, decimal Cost, string Currency,
    int EstimatedDaysMin, int EstimatedDaysMax, bool CashOnDeliveryCovered);

public sealed record PickupStoreInfo(Guid Id, string Name, string Line1, string City, string DepartmentCode, string DepartmentName);

/// <summary>A customer-facing summary of one order (public, read-only) — e.g. for the AI assistant's status tool.</summary>
public sealed record CustomerOrderView(string OrderNumber, string Status, decimal GrandTotal, string Currency, DateTimeOffset CreatedAtUtc);

/// <summary>
/// Public, in-process read query for a single customer's order by number. Ownership is enforced by the
/// <c>customerId</c> parameter: a caller can only ever see their own order, so a mismatched (or unknown) number
/// returns null — no cross-customer leakage.
/// </summary>
public interface IOrderQueries
{
    Task<CustomerOrderView?> GetForCustomerByNumberAsync(Guid customerId, string orderNumber, CancellationToken ct = default);
}

// --- Integration events (published via the outbox) ----------------------------------------------

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, string Currency, decimal GrandTotal, int ItemCount) : IntegrationEvent;

public sealed record OrderConfirmedIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, decimal GrandTotal, string Currency, string PaymentMethod) : IntegrationEvent;

public sealed record OrderPaidIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, decimal GrandTotal, string Currency) : IntegrationEvent;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, string? Reason) : IntegrationEvent;
