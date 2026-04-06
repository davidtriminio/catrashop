using System.Globalization;
using System.Text;

namespace CommerceHub.BuildingBlocks.Domain;

/// <summary>
/// URL-friendly slug generation. Lowercases, strips diacritics, and collapses any run of
/// non-alphanumeric characters into a single hyphen. Used for category/brand/product slugs.
/// </summary>
public static class Slug
{
    public static string From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Decompose accents (e.g. "Café" → "Cafe") then drop combining marks.
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(ch);
        }

        var ascii = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        var result = new StringBuilder(ascii.Length);
        var lastWasHyphen = false;
        foreach (var ch in ascii)
        {
            if (char.IsLetterOrDigit(ch))
            {
                result.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                result.Append('-');
                lastWasHyphen = true;
            }
        }

        return result.ToString().Trim('-');
    }
}
