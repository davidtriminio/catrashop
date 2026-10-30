using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.AI.Application.Abstractions;
using Catrashop.Modules.AI.Application.Agent;
using Catrashop.Modules.AI.Application.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Catrashop.Modules.AI.Api;

internal static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai").WithTags("AI assistant").RequireAuthorization();

        group.MapPost("/chat", async (
            ChatRequest request, ICurrentUser user, AgentOrchestrator agent, IOptions<AgentOptions> options, CancellationToken ct) =>
        {
            if (user.UserId is not { } userId) return Results.Unauthorized();

            var message = request.Message?.Trim();
            if (string.IsNullOrWhiteSpace(message))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = ["A message is required."] });
            if (message.Length > options.Value.MaxMessageLength)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = [$"Message exceeds {options.Value.MaxMessageLength} characters."] });

            var history = MapHistory(request.History);
            var reply = await agent.ChatAsync(userId, message, history, ct);
            return Results.Ok(new ChatResponse(reply.Message, reply.ToolsUsed));
        })
        .WithSummary("Chat with the shopping assistant (bounded tools + RAG, scoped to your own data).");

        return app;
    }

    private static IReadOnlyList<ChatMessage>? MapHistory(IReadOnlyList<ChatTurnDto>? history)
    {
        if (history is null || history.Count == 0) return null;
        return history
            .Select(t => (Role: ParseRole(t.Role), t.Content))
            .Where(t => t.Role is ChatRole.User or ChatRole.Assistant && !string.IsNullOrWhiteSpace(t.Content))
            .Select(t => new ChatMessage(t.Role, t.Content))
            .ToList();
    }

    private static ChatRole ParseRole(string? role) => role?.ToLowerInvariant() switch
    {
        "assistant" => ChatRole.Assistant,
        "user" => ChatRole.User,
        _ => ChatRole.System, // filtered out downstream — never trusted from the client
    };
}
