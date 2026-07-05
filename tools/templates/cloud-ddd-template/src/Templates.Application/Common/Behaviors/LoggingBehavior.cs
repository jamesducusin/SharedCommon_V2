namespace Templates.Application.Common.Behaviors;

using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// MediatR pipeline behavior for logging request and response.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "Starting request: {RequestName} {@Request}",
            requestName,
            request);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);

            stopwatch.Stop();
            logger.LogInformation(
                "Completed request: {RequestName} completed in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Request {RequestName} failed after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
