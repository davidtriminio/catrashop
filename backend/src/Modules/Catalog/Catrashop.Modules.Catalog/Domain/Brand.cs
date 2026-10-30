using Catrashop.BuildingBlocks.Domain;

namespace Catrashop.Modules.Catalog.Domain;

/// <summary>A product brand/manufacturer. Slug is unique and URL-facing.</summary>
public sealed class Brand : AggregateRoot<Guid>
{
    private Brand() { } // EF

    private Brand(Guid id, string name, string slug, string? description, DateTimeOffset nowUtc) : base(id)
    {
        Name = name;
        Slug = slug;
        Description = description;
        IsActive = true;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Brand Create(string name, string? description, DateTimeOffset nowUtc) =>
        new(Guid.NewGuid(), name.Trim(), BuildingBlocks.Domain.Slug.From(name), description?.Trim(), nowUtc);

    public void Activate() { IsActive = true; UpdatedAtUtc = DateTimeOffset.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAtUtc = DateTimeOffset.UtcNow; }
}
