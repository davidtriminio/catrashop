using System.Text.RegularExpressions;

namespace CommerceHub.Modules.AI.Application.Agent;

public sealed record InjectionAssessment(bool IsSuspicious, bool IsExfiltration, string? Reason);

/// <summary>
/// A lightweight, defense-in-depth check over user input. The system prompt and "tool output is data" handling
/// are the primary defenses; this guard adds a hard stop for the clearest attacks — attempts to override the
/// instructions or to exfiltrate the system prompt/configuration — which the orchestrator refuses outright.
/// </summary>
public sealed partial class PromptInjectionGuard
{
    [GeneratedRegex(@"\b(ignore|disregard|forget)\b.{0,30}\b(previous|prior|above|earlier|all)\b.{0,20}\b(instruction|prompt|rule|message)s?\b", RegexOptions.IgnoreCase)]
    private static partial Regex OverrideRegex();

    [GeneratedRegex(@"\b(reveal|show|print|repeat|expose|output|tell me)\b.{0,40}\b(system|initial|your)\b.{0,20}\b(prompt|instruction|rule|configuration)s?\b", RegexOptions.IgnoreCase)]
    private static partial Regex ExfiltrationRegex();

    [GeneratedRegex(@"\b(developer mode|jailbreak|do anything now|DAN mode|you are now)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RoleplayRegex();

    public InjectionAssessment Analyze(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new InjectionAssessment(false, false, null);

        if (ExfiltrationRegex().IsMatch(input))
            return new InjectionAssessment(true, true, "Attempt to reveal system prompt/configuration.");
        if (OverrideRegex().IsMatch(input))
            return new InjectionAssessment(true, true, "Attempt to override instructions.");
        if (RoleplayRegex().IsMatch(input))
            return new InjectionAssessment(true, false, "Roleplay/jailbreak phrasing.");

        return new InjectionAssessment(false, false, null);
    }
}
