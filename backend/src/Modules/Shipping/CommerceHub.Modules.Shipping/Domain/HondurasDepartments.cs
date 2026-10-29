namespace CommerceHub.Modules.Shipping.Domain;

/// <summary>A first-level administrative division of Honduras, identified by its ISO 3166-2 code (e.g. "HN-FM").</summary>
public sealed record Department(string Code, string Name);

/// <summary>
/// The 18 departments of Honduras — the fixed list national shipping is built on. Zones group departments; there is
/// deliberately no municipality catalog.
/// </summary>
public static class HondurasDepartments
{
    public static readonly IReadOnlyList<Department> All =
    [
        new("HN-AT", "Atlántida"),
        new("HN-CH", "Choluteca"),
        new("HN-CL", "Colón"),
        new("HN-CM", "Comayagua"),
        new("HN-CP", "Copán"),
        new("HN-CR", "Cortés"),
        new("HN-EP", "El Paraíso"),
        new("HN-FM", "Francisco Morazán"),
        new("HN-GD", "Gracias a Dios"),
        new("HN-IN", "Intibucá"),
        new("HN-IB", "Islas de la Bahía"),
        new("HN-LP", "La Paz"),
        new("HN-LE", "Lempira"),
        new("HN-OC", "Ocotepeque"),
        new("HN-OL", "Olancho"),
        new("HN-SB", "Santa Bárbara"),
        new("HN-VA", "Valle"),
        new("HN-YO", "Yoro"),
    ];

    private static readonly Dictionary<string, Department> ByCode =
        All.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);

    public static Department? Find(string? code) =>
        code is not null && ByCode.TryGetValue(code.Trim(), out var department) ? department : null;

    public static bool IsValid(string? code) => Find(code) is not null;

    /// <summary>The canonical (upper-case) code, or null when it is not a Honduran department.</summary>
    public static string? Normalize(string? code) => Find(code)?.Code;
}
