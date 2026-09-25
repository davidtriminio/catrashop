using System.Text;
using System.Text.Json;
using CommerceHub.Modules.AI.Application.Abstractions;

namespace CommerceHub.Modules.AI.Application.Tools;

/// <summary>RAG tool: retrieves store-policy/help snippets for a question. Public content, no ownership needed.</summary>
public sealed class KnowledgeSearchTool : IAgentTool
{
    private readonly IKnowledgeRetriever _retriever;
    public KnowledgeSearchTool(IKnowledgeRetriever retriever) => _retriever = retriever;

    public string Name => "knowledge_search";
    public string Description => "Look up CommerceHub store policies and help (shipping, returns, payments, coupons, accounts). Args: { \"query\": string }.";

    public async Task<string> InvokeAsync(string argumentsJson, AgentContext context, CancellationToken ct = default)
    {
        var query = ToolArgs.GetString(argumentsJson, "query");
        if (string.IsNullOrWhiteSpace(query)) return "No query provided.";

        var hits = await _retriever.RetrieveAsync(query, 3, ct);
        if (hits.Count == 0) return "No relevant policy or help article was found.";

        var sb = new StringBuilder();
        foreach (var h in hits)
            sb.Append("- ").Append(h.Title).Append(": ").Append(h.Content).Append('\n');
        return sb.ToString().TrimEnd();
    }
}
