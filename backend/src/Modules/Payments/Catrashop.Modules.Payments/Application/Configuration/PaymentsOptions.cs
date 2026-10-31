namespace Catrashop.Modules.Payments.Application.Configuration;

/// <summary>Payments settings. Bound from configuration section "Payments".</summary>
public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>Active provider name (matches an <c>IPaymentProvider.Name</c>). Defaults to the simulated provider.</summary>
    public string Provider { get; set; } = "fake";

    /// <summary>Shared secret used to verify inbound webhook signatures. Supplied via config/env, never committed.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>The bank accounts customers may transfer to. Shown at checkout; the code is what a payment records.</summary>
    public List<BankAccountOption> BankAccounts { get; set; } = [];

    /// <summary>
    /// Folder where transfer receipts are stored — on the server's disk, outside the repository. Defaults to a
    /// "Catrashop/receipts" folder under the OS application-data directory; set it explicitly in production.
    /// </summary>
    public string? ReceiptsPath { get; set; }

    /// <summary>Largest receipt accepted, in bytes (default 5 MB).</summary>
    public long MaxReceiptBytes { get; set; } = 5 * 1024 * 1024;
}

public sealed class BankAccountOption
{
    public string Code { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;
    public string Holder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
}
