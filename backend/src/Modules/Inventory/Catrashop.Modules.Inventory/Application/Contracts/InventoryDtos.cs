namespace Catrashop.Modules.Inventory.Application.Contracts;

// --- Admin requests -----------------------------------------------------------------------------

public sealed record CreateInventoryItemRequest(string Sku, int InitialOnHand, int LowStockThreshold);
public sealed record ReceiveStockRequest(int Quantity, string? Reason);
public sealed record AdjustStockRequest(int Delta, string? Reason);
public sealed record SetThresholdRequest(int Threshold);

// --- Responses ----------------------------------------------------------------------------------

public sealed record InventoryItemResponse(
    Guid Id,
    string Sku,
    int OnHand,
    int Reserved,
    int Available,
    int LowStockThreshold,
    bool IsLowStock);

public sealed record StockMovementResponse(
    Guid Id,
    string Type,
    int Quantity,
    string? Reason,
    Guid? Reference,
    DateTimeOffset OccurredAtUtc);
