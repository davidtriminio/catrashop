namespace Catrashop.Modules.Orders.Domain;

/// <summary>A line on an order. Prices are captured from the cart at checkout and never change afterwards.</summary>
public sealed class OrderItem
{
    private OrderItem() { } // EF

    internal OrderItem(Guid id, Guid orderId, Guid productId, Guid variantId, string sku, string productName,
        decimal unitPrice, int quantity)
    {
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        VariantId = variantId;
        Sku = sku;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid VariantId { get; private set; }
    public string Sku { get; private set; } = default!;
    public string ProductName { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>An immutable record of a status change, forming the order's timeline ("historial").</summary>
public sealed class OrderStatusHistory
{
    private OrderStatusHistory() { } // EF

    internal OrderStatusHistory(Guid id, Guid orderId, OrderStatus status, string? note, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        OrderId = orderId;
        Status = status;
        Note = note;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}

/// <summary>A shipping address captured onto the order at checkout (a snapshot, independent of the customer's book).</summary>
public sealed class ShippingAddress
{
    private ShippingAddress() { } // EF

    public ShippingAddress(string recipientName, string line1, string? line2, string city, string? region,
        string postalCode, string country)
    {
        RecipientName = recipientName;
        Line1 = line1;
        Line2 = line2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
    }

    public string RecipientName { get; private set; } = default!;
    public string Line1 { get; private set; } = default!;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = default!;
    public string? Region { get; private set; }
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;
}
