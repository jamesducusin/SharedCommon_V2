namespace Templates.Domain.Common.Exceptions;

/// <summary>
/// Thrown when an operation violates a business rule or constraint.
/// HTTP Status: 400 Bad Request or 409 Conflict (depending on context)
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public override string ErrorCode => "BUSINESS_RULE_VIOLATION";
    public override int StatusCode => 400;

    /// <summary>
    /// Initializes a new instance of BusinessRuleViolationException.
    /// </summary>
    /// <param name="message">Description of the violated business rule</param>
    /// <param name="ruleCode">Optional code identifying the specific rule</param>
    public BusinessRuleViolationException(string message, string? ruleCode = null)
        : base(message)
    {
        if (ruleCode != null)
            Details = new() { { "ruleCode", ruleCode } };
    }
}
