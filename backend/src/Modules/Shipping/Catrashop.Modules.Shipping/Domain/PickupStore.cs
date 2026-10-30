using Catrashop.BuildingBlocks.Domain;

namespace Catrashop.Modules.Shipping.Domain;

/// <summary>
/// A store where customers can collect their order (free of delivery cost). Only active stores are offered at
/// checkout; an order keeps a snapshot of the store's name and address.
/// </summary>
public sealed class PickupStore : AggregateRoot<Guid>
{
    private PickupStore() { } // EF

    private PickupStore(Guid id, string name, string line1, string city, string departmentCode, string? phone,
        string? hours, DateTimeOffset nowUtc) : base(id)
    {
        Name = name;
        Line1 = line1;
        City = city;
        DepartmentCode = departmentCode;
        Phone = phone;
        Hours = hours;
        IsActive = true;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string Name { get; private set; } = default!;
    public string Line1 { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string DepartmentCode { get; private set; } = default!;
    public string? Phone { get; private set; }
    public string? Hours { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PickupStore Create(string name, string line1, string city, string departmentCode, string? phone,
        string? hours, DateTimeOffset nowUtc) =>
        new(Guid.NewGuid(), name.Trim(), line1.Trim(), city.Trim(), departmentCode.Trim().ToUpperInvariant(),
            Clean(phone), Clean(hours), nowUtc);

    public void Update(string name, string line1, string city, string departmentCode, string? phone, string? hours,
        DateTimeOffset nowUtc)
    {
        Name = name.Trim();
        Line1 = line1.Trim();
        City = city.Trim();
        DepartmentCode = departmentCode.Trim().ToUpperInvariant();
        Phone = Clean(phone);
        Hours = Clean(hours);
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTimeOffset nowUtc) { IsActive = true; UpdatedAtUtc = nowUtc; }
    public void Deactivate(DateTimeOffset nowUtc) { IsActive = false; UpdatedAtUtc = nowUtc; }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
