using CommerceHub.BuildingBlocks.Domain;

namespace CommerceHub.Modules.Catalog.Domain;

/// <summary>
/// A catalog category. Categories form a tree via <see cref="ParentId"/> (null = top level). The
/// <see cref="Slug"/> is unique and used in customer-facing URLs.
/// </summary>
public sealed class Category : AggregateRoot<Guid>
{
    private Category() { } // EF

    private Category(Guid id, string name, string slug, string? description, Guid? parentId, DateTimeOffset nowUtc)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ParentId = parentId;
        IsActive = true;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Category Create(string name, string? description, Guid? parentId, DateTimeOffset nowUtc) =>
        new(Guid.NewGuid(), name.Trim(), BuildingBlocks.Domain.Slug.From(name), description?.Trim(), parentId, nowUtc);

    public void Rename(string name)
    {
        Name = name.Trim();
        Slug = BuildingBlocks.Domain.Slug.From(name);
        Touch();
    }

    public void Activate() { IsActive = true; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
