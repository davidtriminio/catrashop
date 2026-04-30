namespace CommerceHub.Modules.Customers.Domain;

/// <summary>A postal address belonging to a customer. At most one may be default-shipping and one default-billing.</summary>
public sealed class Address
{
    private Address() { } // EF

    internal Address(Guid id, Guid customerId, string label, string recipientName, string line1, string? line2,
        string city, string? region, string postalCode, string country, string? phone)
    {
        Id = id;
        CustomerId = customerId;
        Label = label;
        RecipientName = recipientName;
        Line1 = line1;
        Line2 = line2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
        Phone = phone;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Label { get; private set; } = default!;
    public string RecipientName { get; private set; } = default!;
    public string Line1 { get; private set; } = default!;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = default!;
    public string? Region { get; private set; }
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;
    public string? Phone { get; private set; }
    public bool IsDefaultShipping { get; private set; }
    public bool IsDefaultBilling { get; private set; }

    internal void Update(string label, string recipientName, string line1, string? line2, string city,
        string? region, string postalCode, string country, string? phone)
    {
        Label = label;
        RecipientName = recipientName;
        Line1 = line1;
        Line2 = line2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
        Phone = phone;
    }

    internal void SetDefaultShipping(bool value) => IsDefaultShipping = value;
    internal void SetDefaultBilling(bool value) => IsDefaultBilling = value;
}

/// <summary>A product a customer has saved for later. Unique per product within a customer.</summary>
public sealed class WishlistItem
{
    private WishlistItem() { } // EF

    internal WishlistItem(Guid id, Guid customerId, Guid productId, DateTimeOffset addedAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        ProductId = productId;
        AddedAtUtc = addedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTimeOffset AddedAtUtc { get; private set; }
}
