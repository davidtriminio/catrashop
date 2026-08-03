namespace CommerceHub.Modules.AI.Application.Abstractions;

public enum ChatRole { System, User, Assistant, Tool }

/// <summary>One message in the model conversation. Tool messages carry the tool name + call id.</summary>
public sealed record ChatMessage(ChatRole Role, string Content, string? Name = null, string? ToolCallId = null);

/// <summary>The name + description of a tool the model is allowed to call (the allow-list is the boundary).</summary>
public sealed record ToolDefinition(string Name, string Description);

/// <summary>A model's request to call one tool, with JSON arguments.</summary>
public sealed record ToolCall(string Id, string Name, string ArgumentsJson);

/// <summary>A model turn: either final text (no tool calls) or one/more tool calls to execute and feed back.</summary>
public sealed record ChatCompletion(string? Text, IReadOnlyList<ToolCall> ToolCalls)
{
    public bool WantsTools => ToolCalls.Count > 0;
    public static ChatCompletion Final(string text) => new(text, Array.Empty<ToolCall>());
    public static ChatCompletion Call(params ToolCall[] calls) => new(null, calls);
}

/// <summary>Chat-completion provider (swap the fake dev model for a real LLM behind this interface).</summary>
public interface IChatModel
{
    Task<ChatCompletion> CompleteAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools, CancellationToken ct = default);
}

/// <summary>
/// The trusted execution context for a tool call. <see cref="UserId"/> is the authenticated caller, set by the
/// orchestrator from the JWT — never from the model — so ownership-scoped tools can only ever touch the caller's
/// own data.
/// </summary>
public sealed record AgentContext(Guid UserId);

/// <summary>
/// A bounded capability the assistant may invoke. Each tool is a specific, allow-listed operation over a module's
/// public read contract; there is no tool for arbitrary SQL/shell/HTTP — the set of tools is the security
/// boundary. Ownership is enforced inside the tool using <see cref="AgentContext.UserId"/>.
/// </summary>
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    Task<string> InvokeAsync(string argumentsJson, AgentContext context, CancellationToken ct = default);
}

public sealed record KnowledgeSnippet(string Title, string Content, double Score);

/// <summary>Retrieval over the (non-PII) knowledge base for RAG grounding.</summary>
public interface IKnowledgeRetriever
{
    Task<IReadOnlyList<KnowledgeSnippet>> RetrieveAsync(string query, int topK, CancellationToken ct = default);
}

/// <summary>The orchestrator's answer plus which tools ran (for transparency / debugging).</summary>
public sealed record AgentReply(string Message, IReadOnlyList<string> ToolsUsed);
