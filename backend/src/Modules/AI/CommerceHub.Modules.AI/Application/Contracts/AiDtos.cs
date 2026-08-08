namespace CommerceHub.Modules.AI.Application.Contracts;

/// <summary>A prior turn supplied by the client (the assistant is otherwise stateless this phase).</summary>
public sealed record ChatTurnDto(string Role, string Content);

public sealed record ChatRequest(string Message, IReadOnlyList<ChatTurnDto>? History = null);

public sealed record ChatResponse(string Reply, IReadOnlyList<string> ToolsUsed);
