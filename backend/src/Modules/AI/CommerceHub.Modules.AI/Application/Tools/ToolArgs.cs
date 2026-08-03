using System.Text.Json;

namespace CommerceHub.Modules.AI.Application.Tools;

/// <summary>Safe extraction of tool arguments from the model's JSON (tolerant of malformed input).</summary>
internal static class ToolArgs
{
    public static string? GetString(string json, string name)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            return doc.RootElement.ValueKind == JsonValueKind.Object &&
                   doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static int? GetInt(string json, string name)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number &&
                v.TryGetInt32(out var i))
                return i;
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
