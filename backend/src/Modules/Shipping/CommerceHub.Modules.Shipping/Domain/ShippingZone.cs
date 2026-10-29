using CommerceHub.BuildingBlocks.Domain;

namespace CommerceHub.Modules.Shipping.Domain;

/// <summary>
/// A group of departments that share one delivery cost and lead time. The cost of delivering an order depends only
/// on the zone of its destination — never on how the customer pays. A zone also says whether cash on delivery is
/// available there. Each department belongs to at most one zone (enforced by a unique index).
/// </summary>
public sealed class ShippingZone : AggregateRoot<Guid>
{
    private readonly List<ShippingZoneDepartment> _departments = new();

    private ShippingZone() { } // EF

    private ShippingZone(Guid id, string code, string name, decimal cost, string currency, int daysMin, int daysMax,
        bool cashOnDeliveryCovered, DateTimeOffset nowUtc) : base(id)
    {
        Code = code;
        Name = name;
        Cost = cost;
        Currency = currency;
        EstimatedDaysMin = daysMin;
        EstimatedDaysMax = daysMax;
        CashOnDeliveryCovered = cashOnDeliveryCovered;
        IsActive = true;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal Cost { get; private set; }
    public string Currency { get; private set; } = default!;
    public int EstimatedDaysMin { get; private set; }
    public int EstimatedDaysMax { get; private set; }

    /// <summary>Whether an order delivered here may be paid on delivery (used by the payment rules).</summary>
    public bool CashOnDeliveryCovered { get; private set; }

    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ShippingZoneDepartment> Departments => _departments.AsReadOnly();

    public static ShippingZone Create(string code, string name, decimal cost, string currency, int daysMin, int daysMax,
        bool cashOnDeliveryCovered, IEnumerable<string> departmentCodes, DateTimeOffset nowUtc)
    {
        var zone = new ShippingZone(Guid.NewGuid(), code.Trim().ToLowerInvariant(), name.Trim(), cost,
            currency.Trim().ToUpperInvariant(), daysMin, daysMax, cashOnDeliveryCovered, nowUtc);
        zone.ReplaceDepartments(departmentCodes);
        return zone;
    }

    public void Update(string name, decimal cost, string currency, int daysMin, int daysMax, bool cashOnDeliveryCovered,
        IEnumerable<string> departmentCodes, DateTimeOffset nowUtc)
    {
        Name = name.Trim();
        Cost = cost;
        Currency = currency.Trim().ToUpperInvariant();
        EstimatedDaysMin = daysMin;
        EstimatedDaysMax = daysMax;
        CashOnDeliveryCovered = cashOnDeliveryCovered;
        ReplaceDepartments(departmentCodes);
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTimeOffset nowUtc) { IsActive = true; UpdatedAtUtc = nowUtc; }
    public void Deactivate(DateTimeOffset nowUtc) { IsActive = false; UpdatedAtUtc = nowUtc; }

    /// <summary>Makes the zone cover exactly these departments, touching only the ones that changed.</summary>
    private void ReplaceDepartments(IEnumerable<string> departmentCodes)
    {
        var wanted = departmentCodes
            .Select(c => HondurasDepartments.Normalize(c) ?? c.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _departments.RemoveAll(d => !wanted.Contains(d.DepartmentCode));
        foreach (var code in wanted.Where(c => _departments.All(d => !string.Equals(d.DepartmentCode, c, StringComparison.OrdinalIgnoreCase))))
            _departments.Add(new ShippingZoneDepartment(Id, code));
    }
}

/// <summary>One department assigned to a zone.</summary>
public sealed class ShippingZoneDepartment
{
    private ShippingZoneDepartment() { } // EF

    internal ShippingZoneDepartment(Guid zoneId, string departmentCode)
    {
        ZoneId = zoneId;
        DepartmentCode = departmentCode;
    }

    public Guid ZoneId { get; private set; }
    public string DepartmentCode { get; private set; } = default!;
}
