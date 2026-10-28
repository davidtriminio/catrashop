namespace CommerceHub.BuildingBlocks.Money;

/// <summary>Store-wide settings shared by every module. Bound from configuration section "Store".</summary>
public sealed class StoreOptions
{
    public const string SectionName = "Store";

    /// <summary>The single currency the store sells in (ISO 4217). Honduran lempira by default.</summary>
    public string Currency { get; set; } = "HNL";

    /// <summary>True when <paramref name="code"/> is the store currency (case-insensitive).</summary>
    public bool IsStoreCurrency(string? code) =>
        string.Equals(code?.Trim(), Currency, StringComparison.OrdinalIgnoreCase);

    public string CurrencyMessage => $"The currency must be {Currency}.";
}
