namespace Templates.Application.Features.Orders.Validation;

using Templates.Application.Common.Telemetry;

/// <summary>
/// Example validation handler using FluentValidation with comprehensive telemetry.
/// Demonstrates: validation metrics, failure tracking, error categorization, and performance monitoring.
/// </summary>
public class OrderValidationHandlerWithTelemetry
{
    private readonly ITelemetryService _telemetry;
    // private readonly IValidator<CreateOrderCommand> _validator;

    public OrderValidationHandlerWithTelemetry(
        ITelemetryService telemetry)
        // IValidator<CreateOrderCommand> validator)
    {
        _telemetry = telemetry;
        // _validator = validator;
    }

    /// <summary>
    /// Validate order command with detailed failure tracking and telemetry.
    /// Demonstrates: multi-level validation, error aggregation, performance metrics.
    /// </summary>
    public ValidationResult ValidateOrder(CreateOrderCommand command)
    {
        using var scope = _telemetry.StartOperation("ValidateOrder", "validation");
        scope.SetTag("command.type", nameof(CreateOrderCommand));
        scope.SetTag("order.id", command.OrderId);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var errors = new List<ValidationError>();

            // Level 1: Basic structural validation
            var structuralErrors = ValidateStructure(command, scope);
            errors.AddRange(structuralErrors);

            // Level 2: Business rule validation
            if (!errors.Any(e => e.ErrorCode == "STRUCTURAL"))
            {
                var businessErrors = ValidateBusinessRules(command, scope);
                errors.AddRange(businessErrors);
            }

            // Level 3: Cross-field validation
            if (!errors.Any(e => e.Severity == "Error"))
            {
                var crossFieldErrors = ValidateCrossFields(command, scope);
                errors.AddRange(crossFieldErrors);
            }

            sw.Stop();

            // Record results
            scope.SetTag("validation.duration_ms", sw.ElapsedMilliseconds);
            scope.SetTag("errors.count", errors.Count);
            scope.SetTag("errors.critical", errors.Count(e => e.Severity == "Error"));
            scope.SetTag("errors.warning", errors.Count(e => e.Severity == "Warning"));

            // Record metrics
            _telemetry.RecordMetric("validation.duration_ms", (double)sw.ElapsedMilliseconds);
            _telemetry.RecordMetric("validation.errors", errors.Count);

            if (errors.Any(e => e.Severity == "Error"))
            {
                scope.SetTag("validation.result", "failed");
                _telemetry.RecordMetric("validation.failures", 1, new()
                {
                    { "reason", GetFailureCategory(errors) }
                });

                scope.MarkFailed($"Validation failed with {errors.Count} errors");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            scope.SetTag("validation.result", "passed");
            _telemetry.RecordMetric("validation.passed", 1);
            scope.MarkSucceeded();

            return new ValidationResult { IsValid = true, Errors = errors };
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            _telemetry.RecordMetric("validation.error", 1);
            throw;
        }
    }

    /// <summary>
    /// Level 1: Structural validation (required fields, type checks).
    /// </summary>
    private List<ValidationError> ValidateStructure(
        CreateOrderCommand command,
        IOperationScope scope)
    {
        using var levelScope = _telemetry.StartOperation("ValidateStructure", "validation");
        levelScope.SetTag("level", 1);

        var errors = new List<ValidationError>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Required fields
            if (command.OrderId == Guid.Empty)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.OrderId),
                    ErrorMessage = "Order ID is required",
                    ErrorCode = "STRUCTURAL",
                    Severity = "Error"
                });
            }

            if (string.IsNullOrWhiteSpace(command.OrderNumber))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.OrderNumber),
                    ErrorMessage = "Order number is required",
                    ErrorCode = "STRUCTURAL",
                    Severity = "Error"
                });
            }

            if (command.CustomerId == Guid.Empty)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.CustomerId),
                    ErrorMessage = "Customer ID is required",
                    ErrorCode = "STRUCTURAL",
                    Severity = "Error"
                });
            }

            // Type/range checks
            if (command.Total <= 0)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.Total),
                    ErrorMessage = "Order total must be greater than zero",
                    ErrorCode = "RANGE",
                    Severity = "Error"
                });
            }

            if (command.Items == null || !command.Items.Any())
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.Items),
                    ErrorMessage = "Order must contain at least one item",
                    ErrorCode = "STRUCTURAL",
                    Severity = "Error"
                });
            }

            sw.Stop();

            levelScope.SetTag("errors", errors.Count);
            levelScope.SetTag("duration_ms", sw.ElapsedMilliseconds);
            levelScope.MarkSucceeded();

            _telemetry.RecordMetric("validation.structural.errors", errors.Count);

            return errors;
        }
        catch (Exception ex)
        {
            levelScope.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Level 2: Business rule validation (domain constraints, state checks).
    /// </summary>
    private List<ValidationError> ValidateBusinessRules(
        CreateOrderCommand command,
        IOperationScope scope)
    {
        using var levelScope = _telemetry.StartOperation("ValidateBusinessRules", "validation");
        levelScope.SetTag("level", 2);

        var errors = new List<ValidationError>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Order number uniqueness (in real scenario, check database)
            // This is business rule: duplicate orders within time window not allowed
            if (OrderNumberExistsRecently(command.OrderNumber))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.OrderNumber),
                    ErrorMessage = "An order with this number was recently created",
                    ErrorCode = "BUSINESS_DUPLICATE",
                    Severity = "Error"
                });

                levelScope.SetTag("business_rule_violation", "duplicate_order");
            }

            // Customer status check (in real scenario, check customer service)
            if (!IsCustomerEligible(command.CustomerId))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.CustomerId),
                    ErrorMessage = "Customer is not eligible to place orders",
                    ErrorCode = "BUSINESS_ELIGIBLE",
                    Severity = "Error"
                });

                levelScope.SetTag("business_rule_violation", "ineligible_customer");
            }

            // Item count limit (business rule: max 100 items per order)
            if (command.Items.Count > 100)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.Items),
                    ErrorMessage = "Order cannot contain more than 100 items",
                    ErrorCode = "BUSINESS_LIMIT",
                    Severity = "Error"
                });

                levelScope.SetTag("business_rule_violation", "item_limit_exceeded");
            }

            // Total validation (sum of items should match total)
            var calculatedTotal = command.Items.Sum(i => i.Quantity * i.UnitPrice);
            if (Math.Abs(command.Total - calculatedTotal) > 0.01m) // Allow 1 cent difference for rounding
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.Total),
                    ErrorMessage = $"Total mismatch: expected {calculatedTotal:C}, got {command.Total:C}",
                    ErrorCode = "BUSINESS_CALCULATION",
                    Severity = "Warning" // Warning vs Error based on severity
                });

                levelScope.SetTag("warning", "total_mismatch");
            }

            sw.Stop();

            levelScope.SetTag("errors", errors.Count);
            levelScope.SetTag("duration_ms", sw.ElapsedMilliseconds);
            levelScope.MarkSucceeded();

            _telemetry.RecordMetric("validation.business_rules.errors", errors.Count);

            return errors;
        }
        catch (Exception ex)
        {
            levelScope.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Level 3: Cross-field and correlation validation.
    /// </summary>
    private List<ValidationError> ValidateCrossFields(
        CreateOrderCommand command,
        IOperationScope scope)
    {
        using var levelScope = _telemetry.StartOperation("ValidateCrossFields", "validation");
        levelScope.SetTag("level", 3);

        var errors = new List<ValidationError>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Correlate with items
            foreach (var item in command.Items)
            {
                if (item.Quantity <= 0 || item.UnitPrice <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        PropertyName = nameof(command.Items),
                        ErrorMessage = $"Item {item.ProductId}: Quantity and price must be positive",
                        ErrorCode = "CROSS_FIELD",
                        Severity = "Error"
                    });
                }
            }

            // Order date sanity check
            if (command.CreatedAt > DateTime.UtcNow.AddSeconds(5))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(command.CreatedAt),
                    ErrorMessage = "Order creation date cannot be in the future",
                    ErrorCode = "CROSS_FIELD",
                    Severity = "Warning"
                });
            }

            sw.Stop();

            levelScope.SetTag("errors", errors.Count);
            levelScope.SetTag("duration_ms", sw.ElapsedMilliseconds);
            levelScope.MarkSucceeded();

            _telemetry.RecordMetric("validation.cross_field.errors", errors.Count);

            return errors;
        }
        catch (Exception ex)
        {
            levelScope.RecordException(ex);
            throw;
        }
    }

    // Helper methods

    private static bool OrderNumberExistsRecently(string orderNumber)
    {
        // Simulate database check
        return false;
    }

    private static bool IsCustomerEligible(Guid customerId)
    {
        // Simulate customer service call
        return true;
    }

    private static string GetFailureCategory(List<ValidationError> errors)
    {
        if (errors.Any(e => e.ErrorCode == "STRUCTURAL"))
            return "structural";
        if (errors.Any(e => e.ErrorCode.StartsWith("BUSINESS")))
            return "business_rule";
        return "other";
    }
}

// Supporting types
public record CreateOrderCommand(
    Guid OrderId,
    Guid CustomerId,
    string OrderNumber,
    decimal Total,
    DateTime CreatedAt,
    List<OrderItemCommand> Items);

public record OrderItemCommand(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record ValidationError(
    string PropertyName,
    string ErrorMessage,
    string ErrorCode,
    string Severity);

public record ValidationResult(
    bool IsValid = true,
    List<ValidationError>? Errors = null);
