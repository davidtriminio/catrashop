namespace CommerceHub.Modules.Cart.Application.Services;

/// <summary>Cart limits. Bound from configuration section "Cart".</summary>
public sealed class CartOptions
{
    public const string SectionName = "Cart";

    /// <summary>Most units of one variant a customer may hold in the cart (merged adds count together).</summary>
    public int MaxQuantityPerLine { get; set; } = 20;
}
