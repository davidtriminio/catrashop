namespace CommerceHub.Modules.Shipping.Application.Contracts;

// --- Requests -----------------------------------------------------------------------------------

public sealed record CreateShippingMethodRequest(string Code, string Name, decimal Cost, string Currency, int EstimatedDays);
public sealed record UpdateShippingMethodRequest(string Name, decimal Cost, string Currency, int EstimatedDays);

public sealed record CreateShipmentRequest(Guid OrderId, string MethodCode, string? Carrier);

// --- Responses ----------------------------------------------------------------------------------

public sealed record ShippingMethodResponse(Guid Id, string Code, string Name, decimal Cost, string Currency, int EstimatedDays, bool IsActive);

public sealed record ShipmentEventResponse(string Status, string? Note, DateTimeOffset OccurredAtUtc);

public sealed record ShipmentResponse(
    Guid Id,
    Guid OrderId,
    string Carrier,
    string TrackingNumber,
    string MethodCode,
    decimal Cost,
    string Currency,
    string Status,
    string RecipientName,
    string City,
    string Country,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ShippedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    IReadOnlyList<ShipmentEventResponse> Events);

/// <summary>Public tracking view — status timeline only, no full address (limited PII).</summary>
public sealed record TrackingResponse(
    string TrackingNumber, string Carrier, string Status, string City, string Country, IReadOnlyList<ShipmentEventResponse> Events);
