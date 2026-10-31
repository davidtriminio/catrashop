using Catrashop.BuildingBlocks.Domain;

namespace Catrashop.Modules.Shipping.Domain;

/// <summary>
/// A shipping option customers can be offered (e.g. "standard", "express"): its flat cost and estimated
/// delivery time. Reference data managed by operations; a shipment captures the method's cost when created.
/// </summary>
public sealed class ShippingMethod : AggregateRoot<Guid>
{
    private ShippingMethod() { } // EF

    private ShippingMethod(Guid id, string code, string name, decimal cost, string currency, int estimatedDays, DateTimeOffset nowUtc)
        : base(id)
    {
        Code = code;
        Name = name;
        Cost = cost;
        Currency = currency;
        EstimatedDays = estimatedDays;
        IsActive = true;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal Cost { get; private set; }
    public string Currency { get; private set; } = default!;
    public int EstimatedDays { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ShippingMethod Create(string code, string name, decimal cost, string currency, int estimatedDays, DateTimeOffset nowUtc) =>
        new(Guid.NewGuid(), code.Trim().ToLowerInvariant(), name.Trim(), cost, currency.Trim().ToUpperInvariant(), estimatedDays, nowUtc);

    public void Update(string name, decimal cost, string currency, int estimatedDays, DateTimeOffset nowUtc)
    {
        Name = name.Trim();
        Cost = cost;
        Currency = currency.Trim().ToUpperInvariant();
        EstimatedDays = estimatedDays;
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTimeOffset nowUtc) { IsActive = true; UpdatedAtUtc = nowUtc; }
    public void Deactivate(DateTimeOffset nowUtc) { IsActive = false; UpdatedAtUtc = nowUtc; }
}
