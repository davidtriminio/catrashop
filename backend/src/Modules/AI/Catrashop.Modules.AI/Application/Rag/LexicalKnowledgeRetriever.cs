using System.Text.RegularExpressions;
using Catrashop.Modules.AI.Application.Abstractions;

namespace Catrashop.Modules.AI.Application.Rag;

/// <summary>
/// A simple lexical (term-overlap) retriever over <see cref="KnowledgeBase"/>. It scores each document by how
/// many distinct query terms it contains, normalized by query length, and returns the top matches. Deterministic
/// and dependency-free — a drop-in for a vector retriever behind <see cref="IKnowledgeRetriever"/>.
/// </summary>
public sealed partial class LexicalKnowledgeRetriever : IKnowledgeRetriever
{
    [GeneratedRegex(@"[a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex WordRegex();

    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    { "the", "a", "an", "is", "are", "do", "does", "how", "what", "when", "can", "i", "my", "to", "of", "for", "and", "you", "your" };

    public Task<IReadOnlyList<KnowledgeSnippet>> RetrieveAsync(string query, int topK, CancellationToken ct = default)
    {
        var terms = Tokenize(query);
        if (terms.Count == 0)
            return Task.FromResult<IReadOnlyList<KnowledgeSnippet>>(Array.Empty<KnowledgeSnippet>());

        var scored = KnowledgeBase.Documents
            .Select(doc =>
            {
                var docTerms = Tokenize(doc.Title + " " + doc.Content);
                var overlap = terms.Count(t => docTerms.Contains(t));
                return new KnowledgeSnippet(doc.Title, doc.Content, (double)overlap / terms.Count);
            })
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score)
            .Take(Math.Clamp(topK, 1, KnowledgeBase.Documents.Count))
            .ToList();

        return Task.FromResult<IReadOnlyList<KnowledgeSnippet>>(scored);
    }

    private static HashSet<string> Tokenize(string text) =>
        WordRegex().Matches(text)
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => w.Length > 2 && !Stop.Contains(w))
            .ToHashSet();
}
