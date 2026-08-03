using CommerceHub.BuildingBlocks.Modularity;
using CommerceHub.Modules.AI.Api;
using CommerceHub.Modules.AI.Application.Abstractions;
using CommerceHub.Modules.AI.Application.Agent;
using CommerceHub.Modules.AI.Application.Llm;
using CommerceHub.Modules.AI.Application.Rag;
using CommerceHub.Modules.AI.Application.Tools;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceHub.Modules.AI;

public sealed class AiModule : IModule
{
    public string Name => "AI";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));

        // Chat model + RAG retriever (swap FakeChatModel for a real provider behind IChatModel).
        services.AddSingleton<IChatModel, FakeChatModel>();
        services.AddSingleton<IKnowledgeRetriever, LexicalKnowledgeRetriever>();
        services.AddSingleton<PromptInjectionGuard>();

        // The tool allow-list — the security boundary. Add a tool here to grant a capability; nothing else can.
        services.AddScoped<IAgentTool, KnowledgeSearchTool>();
        services.AddScoped<IAgentTool, SearchProductsTool>();
        services.AddScoped<IAgentTool, OrderStatusTool>();

        services.AddScoped<AgentOrchestrator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapAiEndpoints();
}
