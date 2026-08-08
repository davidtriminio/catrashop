using System.Text;
using CommerceHub.Modules.AI.Application.Abstractions;
using CommerceHub.Modules.Catalog.Contracts;

namespace CommerceHub.Modules.AI.Application.Tools;

/// <summary>Searches active products by name/description. Public catalog data (no PII, no ownership).</summary>
public sealed class SearchProductsTool : IAgentTool
{
    private readonly IProductCatalogQuery _catalog;
    public SearchProductsTool(IProductCatalogQuery catalog) => _catalog = catalog;

    public string Name => "search_products";
    public string Description => "Search the product catalog by keywords. Args: { \"query\": string, \"max\": number? }.";

    public async Task<string> InvokeAsync(string argumentsJson, AgentContext context, CancellationToken ct = default)
    {
        var query = ToolArgs.GetString(argumentsJson, "query");
        if (string.IsNullOrWhiteSpace(query)) return "No search terms provided.";
        var max = ToolArgs.GetInt(argumentsJson, "max") ?? 5;

        var results = await _catalog.SearchProductsAsync(query, max, ct);
        if (results.Count == 0) return $"No products matched \"{query}\".";

        var sb = new StringBuilder();
        foreach (var r in results)
        {
            sb.Append("- ").Append(r.Name);
            if (r.FromPrice is { } price) sb.Append(" — from ").Append(price.ToString("0.00")).Append(' ').Append(r.Currency);
            sb.Append(" (").Append(r.Category).Append(", /products/").Append(r.Slug).Append(")\n");
        }
        return sb.ToString().TrimEnd();
    }
}
