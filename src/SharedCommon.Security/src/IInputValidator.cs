using SharedCommon.Core;

namespace SharedCommon.Security;

/// <summary>
/// Validates and sanitizes user-supplied strings against configured security rules.
/// Registered automatically by <see cref="ServiceCollectionExtensions.AddSharedCommonSecurity"/>.
/// </summary>
public interface IInputValidator
{
    /// <summary>
    /// Validates <paramref name="input"/> against the rules defined in <paramref name="context"/>.
    /// Returns <see cref="Result.Success"/> when the input is clean, or
    /// <see cref="Result.Validation"/> with a description of each violation.
    /// </summary>
    /// <param name="input">The string to validate. May be null (treated as empty).</param>
    /// <param name="context">Validation rules and metadata.</param>
    Result Validate(string? input, InputValidationContext context);

    /// <summary>
    /// Returns a sanitized copy of <paramref name="input"/> according to <paramref name="mode"/>.
    /// Never throws — returns an empty string on null input.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <param name="mode">Sanitization strategy.</param>
    string Sanitize(string? input, SanitizationMode mode);
}

/// <summary>
/// Context passed to <see cref="IInputValidator.Validate"/> that describes the rules to apply.
/// </summary>
public sealed class InputValidationContext
{
    /// <summary>Human-readable field name for error messages.</summary>
    public string FieldName { get; init; } = "input";

    /// <summary>Maximum allowed length. <c>null</c> disables the check.</summary>
    public int? MaxLength { get; init; }

    /// <summary>Reject strings that contain detected attack patterns (SQLi, XSS, path traversal). Default: <c>true</c>.</summary>
    public bool BlockSuspiciousPatterns { get; init; } = true;

    /// <summary>Allow null or empty values. Default: <c>false</c>.</summary>
    public bool AllowEmpty { get; init; } = false;
}

/// <summary>Sanitization strategy applied by <see cref="IInputValidator.Sanitize"/>.</summary>
public enum SanitizationMode
{
    /// <summary>HTML-encode special characters (<c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c>, etc.).</summary>
    HtmlEncode,

    /// <summary>Percent-encode characters not safe in a URL.</summary>
    UrlEncode,

    /// <summary>Escape characters that have special meaning inside JavaScript string literals.</summary>
    ScriptEncode,

    /// <summary>
    /// Escape SQL meta-characters.
    /// Prefer parameterized queries over this mode — it is provided for legacy compatibility only.
    /// </summary>
    SqlEscape
}
