using CommerceHub.BuildingBlocks.Domain;
using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Cart.Domain;

/// <summary>
/// A customer's shopping cart. One active cart per customer. Holds line items with prices captured at
/// add-time, enforces a single currency, and carries a sliding expiry (renewed on every change) so an
/// abandoned cart can be recovered until it lapses. Checkout marks it <see cref="CartStatus.CheckedOut"/>.
/// </summary>
public sealed class Cart : AggregateRoot<Guid>
{
    public const int ExpiryDays = 7;

    private readonly List<CartItem> _items = new();

    private Cart() { } // EF

    private Cart(Guid id, Guid customerId, DateTimeOffset nowUtc) : base(id)
    {
        CustomerId = customerId;
        Currency = string.Empty; // set by the first item added
        Status = CartStatus.Active;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        ExpiresAtUtc = nowUtc.AddDays(ExpiryDays);
    }

    public Guid CustomerId { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public CartStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public decimal Subtotal => _items.Sum(i => i.LineTotal);
    public int ItemCount => _items.Sum(i => i.Quantity);

    public static Cart Create(Guid customerId, DateTimeOffset nowUtc) => new(Guid.NewGuid(), customerId, nowUtc);

    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;

    public Result AddItem(Guid productId, Guid variantId, string sku, string productName,
        decimal unitPrice, string currency, int quantity, DateTimeOffset nowUtc)
    {
        if (Status != CartStatus.Active)
            return CartErrors.NotActive;
        if (quantity <= 0)
            return CartErrors.QuantityMustBePositive;

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (_items.Count == 0)
            Currency = normalizedCurrency;
        else if (Currency != normalizedCurrency)
            return CartErrors.CurrencyMismatch(Currency, normalizedCurrency);

        var existing = _items.FirstOrDefault(i => i.VariantId == variantId);
        if (existing is not null)
            existing.IncreaseQuantity(quantity); // captured price kept from first add
        else
            _items.Add(new CartItem(Guid.NewGuid(), Id, productId, variantId, sku, productName, unitPrice, normalizedCurrency, quantity));

        Renew(nowUtc);
        return Result.Success();
    }

    public Result SetItemQuantity(Guid itemId, int quantity, DateTimeOffset nowUtc)
    {
        if (Status != CartStatus.Active)
            return CartErrors.NotActive;

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return CartErrors.ItemNotFound;

        if (quantity <= 0)
        {
            _items.Remove(item); // setting to zero removes the line
        }
        else
        {
            item.SetQuantity(quantity);
        }

        Renew(nowUtc);
        return Result.Success();
    }

    public Result RemoveItem(Guid itemId, DateTimeOffset nowUtc)
    {
        if (Status != CartStatus.Active)
            return CartErrors.NotActive;

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return CartErrors.ItemNotFound;

        _items.Remove(item);
        Renew(nowUtc);
        return Result.Success();
    }

    public void Clear(DateTimeOffset nowUtc)
    {
        _items.Clear();
        Currency = string.Empty;
        Renew(nowUtc);
    }

    public Result MarkCheckedOut(DateTimeOffset nowUtc)
    {
        if (Status != CartStatus.Active)
            return CartErrors.NotActive;

        Status = CartStatus.CheckedOut;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    private void Renew(DateTimeOffset nowUtc)
    {
        UpdatedAtUtc = nowUtc;
        ExpiresAtUtc = nowUtc.AddDays(ExpiryDays);
    }
}
