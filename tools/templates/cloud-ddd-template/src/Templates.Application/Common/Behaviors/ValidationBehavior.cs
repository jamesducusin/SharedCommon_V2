namespace Templates.Application.Common.Behaviors;

using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedCommon.Core;

/// <summary>
/// MediatR pipeline behavior for validating requests using FluentValidation.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next().ConfigureAwait(false);

        logger.LogDebug("Validating request of type {RequestType}", typeof(TRequest).Name);

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Distinct()
            .ToList();

        if (failures.Count == 0)
            return await next().ConfigureAwait(false);

        logger.LogWarning(
            "Validation failed for request {RequestType} with {FailureCount} errors",
            typeof(TRequest).Name,
            failures.Count);

        throw new ValidationException(failures);
    }
}
