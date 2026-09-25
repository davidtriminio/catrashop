namespace CommerceHub.Modules.Payments.Application.Configuration;

/// <summary>Payments settings. Bound from configuration section "Payments".</summary>
public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>Active provider name (matches an <c>IPaymentProvider.Name</c>). Defaults to the simulated provider.</summary>
    public string Provider { get; set; } = "fake";

    /// <summary>Shared secret used to verify inbound webhook signatures. Supplied via config/env, never committed.</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
