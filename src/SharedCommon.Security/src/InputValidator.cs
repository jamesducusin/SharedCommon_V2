using SharedCommon.Core;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace SharedCommon.Security;

/// <summary>
/// Default <see cref="IInputValidator"/> implementation.
/// Validates string inputs against length and suspicious-pattern rules,
/// and sanitizes with framework-provided encoders.
/// </summary>
public sealed partial class InputValidator : IInputValidator
{
    // Common attack patterns: SQL injection keywords, XSS vectors, path traversal.
    [GeneratedRegex(
        @"('|--|;|\/\*|\*\/|xp_|exec\s|union\s|select\s|insert\s|drop\s|delete\s|update\s|<script|javascript:|onerror=|onload=|\.\.[\\/])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 200)]
    private static partial Regex SuspiciousPatternRegex();

    /// <inheritdoc />
    public Result Validate(string? input, InputValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(input))
        {
            if (!context.AllowEmpty)
                return new Result.Validation(
                    new Dictionary<string, string[]>
                    {
                        [context.FieldName] = [$"{context.FieldName} must not be empty."]
                    });

            return new Result.Success();
        }

        var errors = new Dictionary<string, string[]>();

        if (context.MaxLength.HasValue && input.Length > context.MaxLength.Value)
            errors[context.FieldName] = [$"{context.FieldName} exceeds maximum length of {context.MaxLength.Value} characters."];

        if (context.BlockSuspiciousPatterns)
        {
            try
            {
                if (SuspiciousPatternRegex().IsMatch(input))
                {
                    errors.TryAdd(context.FieldName, []);
                    errors[context.FieldName] = [.. errors[context.FieldName], $"{context.FieldName} contains disallowed characters or patterns."];
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Treat timeout as suspicious — reject for safety.
                errors.TryAdd(context.FieldName, [$"{context.FieldName} could not be validated within the allowed time."]);
            }
        }

        return errors.Count > 0
            ? new Result.Validation(errors)
            : new Result.Success();
    }

    /// <inheritdoc />
    public string Sanitize(string? input, SanitizationMode mode)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        return mode switch
        {
            SanitizationMode.HtmlEncode => HtmlEncoder.Default.Encode(input),
            SanitizationMode.UrlEncode => Uri.EscapeDataString(input),
            SanitizationMode.ScriptEncode => JavaScriptEncoder.Default.Encode(input),
            SanitizationMode.SqlEscape => input
                .Replace("'", "''", StringComparison.Ordinal)
                .Replace(";", string.Empty, StringComparison.Ordinal)
                .Replace("--", string.Empty, StringComparison.Ordinal),
            _ => input
        };
    }
}
