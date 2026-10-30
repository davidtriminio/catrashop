namespace Catrashop.Modules.Orders.Application.Configuration;

/// <summary>Checkout tunables. Bound from configuration section "Orders".</summary>
public sealed class OrderingOptions
{
    public const string SectionName = "Orders";

    /// <summary>Simulated tax rate as a percentage of the discounted subtotal (Section 8: "impuestos simulados").</summary>
    public decimal TaxRatePercent { get; set; } = 10m;

    /// <summary>How long checkout holds the inventory reservation before it expires.</summary>
    public int ReservationHoldMinutes { get; set; } = 30;
}
