using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SharedCommon.Utilities;

/// <summary>Extension methods for <see cref="string"/> manipulation.</summary>
public static partial class StringExtensions
{
    [GeneratedRegex(@"[^a-z0-9\-]", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex SlugInvalidChars();

    [GeneratedRegex(@"-{2,}", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex SlugMultipleDashes();

    /// <summary>Converts the string to a URL-safe slug (lowercase, ASCII, hyphens only).</summary>
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        var slug = builder.ToString().ToLowerInvariant().Replace(' ', '-');
        slug = SlugInvalidChars().Replace(slug, string.Empty);
        slug = SlugMultipleDashes().Replace(slug, "-");
        return slug.Trim('-');
    }

    /// <summary>
    /// Truncates the string to <paramref name="maxLength"/> characters,
    /// appending <paramref name="suffix"/> when truncation occurs.
    /// </summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        if (maxLength <= suffix.Length) return suffix[..maxLength];
        return string.Concat(value.AsSpan(0, maxLength - suffix.Length), suffix);
    }

    /// <summary>
    /// Masks all but the last <paramref name="visibleChars"/> characters with <paramref name="maskChar"/>.
    /// Safe for logging sensitive values (tokens, emails, etc.).
    /// </summary>
    public static string Mask(this string value, int visibleChars = 4, char maskChar = '*')
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length <= visibleChars) return new string(maskChar, value.Length);
        return string.Concat(
            new string(maskChar, value.Length - visibleChars),
            value.AsSpan(value.Length - visibleChars));
    }

    /// <summary>Returns <c>true</c> when the string is <c>null</c> or empty.</summary>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) =>
        string.IsNullOrEmpty(value);

    /// <summary>Returns <c>true</c> when the string is <c>null</c>, empty, or whitespace.</summary>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value) =>
        string.IsNullOrWhiteSpace(value);

    /// <summary>Converts the string to title case using the current culture.</summary>
    public static string ToTitleCase(this string value) =>
        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
}
