using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace SharedCommon.Validation;

/// <summary>
/// Global MVC action filter that automatically validates all incoming model arguments
/// using registered <see cref="IValidator{T}"/> implementations before the action executes.
///
/// Returns HTTP 400 with a structured body when validation fails:
/// <code>
/// {
///   "success": false,
///   "errors": [
///     { "property": "customerId", "code": "NotEmpty", "message": "Customer ID is required." }
///   ]
/// }
/// </code>
///
/// Registered automatically when <see cref="ValidationOptions.AutomaticControllerValidation"/> is true.
/// </summary>
public sealed class AutoValidationFilter : IAsyncActionFilter
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var errors = new List<ValidationError>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            // Create a ValidationContext<T> via reflection so FluentValidation receives the
            // correctly-typed context rather than a raw ValidationContext<object>.
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(argumentType);
            var validationContext = (IValidationContext)Activator.CreateInstance(
                validationContextType, argument)!;

            var result = await validator
                .ValidateAsync(validationContext, context.HttpContext.RequestAborted)
                .ConfigureAwait(false);

            if (!result.IsValid)
                errors.AddRange(result.ToValidationErrors());
        }

        if (errors.Count > 0)
        {
            var responseBody = new { success = false, errors };

            context.Result = new ContentResult
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentType = "application/json",
                Content = JsonSerializer.Serialize(responseBody, _jsonOpts)
            };
            return;
        }

        await next().ConfigureAwait(false);
    }
}
