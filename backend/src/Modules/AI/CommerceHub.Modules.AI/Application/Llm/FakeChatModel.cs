using System.Text.Json;
using System.Text.RegularExpressions;
using CommerceHub.Modules.AI.Application.Abstractions;

namespace CommerceHub.Modules.AI.Application.Llm;

/// <summary>
/// A deterministic, offline stand-in for a function-calling LLM (mirrors the FakePaymentProvider approach). It
/// maps the user's intent to a single tool call, then — once a tool result is present — returns a grounded final
/// answer. A real provider (a hosted LLM provider) implements the same <see cref="IChatModel"/> interface with no
/// changes to the orchestrator, tools, or guardrails.
/// </summary>
public sealed partial class FakeChatModel : IChatModel
{
    [GeneratedRegex(@"ORD-\d{8}-[A-Za-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex OrderNumberRegex();

    private static readonly string[] PolicyWords =
        { "return", "refund", "shipping", "ship", "deliver", "delivery", "policy", "payment", "pay", "coupon", "discount", "warranty", "track", "account", "how long" };
    private static readonly string[] ProductWords =
        { "find", "search", "looking for", "recommend", "buy", "show me", "product", "cheap", "price of", "do you have", "sell" };

    public Task<ChatCompletion> CompleteAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools, CancellationToken ct = default)
    {
        var last = messages[^1];

        // A tool result is back → answer from it (grounded; never fabricated).
        if (last.Role == ChatRole.Tool)
            return Task.FromResult(ChatCompletion.Final(last.Content));

        var text = last.Content;
        var lower = text.ToLowerInvariant();
        var available = tools.Select(t => t.Name).ToHashSet();

        // Order status (most specific): needs an order number.
        var orderMatch = OrderNumberRegex().Match(text);
        if (available.Contains("order_status") && (orderMatch.Success || Mentions(lower, "order", "where is my", "status of my")))
        {
            if (!orderMatch.Success)
                return Task.FromResult(ChatCompletion.Final(
                    "Sure — what's your order number? It looks like ORD-YYYYMMDD-XXXXXX and is on your order confirmation."));
            return Task.FromResult(ChatCompletion.Call(
                Call("order_status", new { orderNumber = orderMatch.Value })));
        }

        if (available.Contains("search_products") && ProductWords.Any(lower.Contains))
            return Task.FromResult(ChatCompletion.Call(Call("search_products", new { query = text })));

        if (available.Contains("knowledge_search") && PolicyWords.Any(lower.Contains))
            return Task.FromResult(ChatCompletion.Call(Call("knowledge_search", new { query = text })));

        return Task.FromResult(ChatCompletion.Final(
            "I can help you find products, check the status of your orders, and answer questions about shipping, " +
            "returns, payments and coupons. What would you like to do?"));
    }

    private static bool Mentions(string haystack, params string[] needles) => needles.Any(haystack.Contains);

    private static ToolCall Call(string name, object args) =>
        new(Guid.NewGuid().ToString("N")[..8], name, JsonSerializer.Serialize(args));
}
