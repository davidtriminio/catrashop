using Catrashop.Modules.AI.Application.Abstractions;
using Catrashop.Modules.Orders.Contracts;

namespace Catrashop.Modules.AI.Application.Tools;

/// <summary>
/// Looks up the status of the CALLER'S own order by number. Ownership is enforced here: the customer id comes
/// from <see cref="AgentContext.UserId"/> (the authenticated principal), never from the model's arguments, so the
/// assistant can never read another customer's order — a wrong/foreign number simply returns "not found".
/// </summary>
public sealed class OrderStatusTool : IAgentTool
{
    private readonly IOrderQueries _orders;
    public OrderStatusTool(IOrderQueries orders) => _orders = orders;

    public string Name => "order_status";
    public string Description => "Get the status of the current customer's own order by its number. Args: { \"orderNumber\": string }.";

    public async Task<string> InvokeAsync(string argumentsJson, AgentContext context, CancellationToken ct = default)
    {
        var orderNumber = ToolArgs.GetString(argumentsJson, "orderNumber");
        if (string.IsNullOrWhiteSpace(orderNumber)) return "Please provide an order number (e.g. ORD-20260908-ABC123).";

        // Ownership: scoped to the authenticated caller only.
        var order = await _orders.GetForCustomerByNumberAsync(context.UserId, orderNumber, ct);
        if (order is null) return $"No order '{orderNumber}' was found on your account.";

        return $"Order {order.OrderNumber}: {order.Status}. Total {order.GrandTotal:0.00} {order.Currency}, placed {order.CreatedAtUtc:yyyy-MM-dd}.";
    }
}
