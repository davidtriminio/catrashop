namespace Catrashop.Modules.AI.Application.Agent;

/// <summary>Bounds on the agent loop. Bound from configuration section "Ai".</summary>
public sealed class AgentOptions
{
    public const string SectionName = "Ai";

    /// <summary>Maximum model↔tool round-trips before the agent gives up (prevents runaway tool loops).</summary>
    public int MaxToolIterations { get; set; } = 3;

    /// <summary>Max characters accepted in a single user message.</summary>
    public int MaxMessageLength { get; set; } = 2000;
}
