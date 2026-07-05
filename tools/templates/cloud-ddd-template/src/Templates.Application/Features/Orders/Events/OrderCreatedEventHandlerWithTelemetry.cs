namespace Templates.Application.Features.Orders.Events;

using Templates.Application.Common.Telemetry;

/// <summary>
/// Example domain event handler showing telemetry patterns for event subscribers.
/// Demonstrates: event processing, message publishing, retry logic, and error handling.
/// </summary>
public class OrderCreatedEventHandlerWithTelemetry
{
    private readonly ITelemetryService _telemetry;
    // private readonly IEmailService _emailService;
    // private readonly IMessagePublisher _messagePublisher;
    // private readonly ILogger<OrderCreatedEventHandlerWithTelemetry> _logger;

    public OrderCreatedEventHandlerWithTelemetry(
        ITelemetryService telemetry)
        // IEmailService emailService,
        // IMessagePublisher messagePublisher,
        // ILogger<OrderCreatedEventHandlerWithTelemetry> logger)
    {
        _telemetry = telemetry;
        // _emailService = emailService;
        // _messagePublisher = messagePublisher;
        // _logger = logger;
    }

    /// <summary>
    /// Handle domain event with idempotency, retries, and distributed tracing.
    /// Demonstrates: event correlation, multi-step processing, error recovery.
    /// </summary>
    public async Task HandleAsync(
        OrderCreatedDomainEvent domainEvent,
        CancellationToken ct)
    {
        // Start operation span - link to original request via TraceId
        using var operationScope = _telemetry.StartOperation("HandleOrderCreatedEvent", "event");
        operationScope.SetTag("event.type", domainEvent.GetType().Name);
        operationScope.SetTag("order.id", domainEvent.OrderId);
        operationScope.SetTag("customer.id", domainEvent.CustomerId);
        operationScope.SetTag("event.timestamp", domainEvent.OccurredAt);

        // Store correlation ID for tracking across async boundaries
        var eventId = domainEvent.OrderId;

        try
        {
            // Step 1: Send confirmation email
            await SendConfirmationEmailAsync(domainEvent, operationScope, ct);

            // Step 2: Publish integration event
            await PublishIntegrationEventAsync(domainEvent, operationScope, ct);

            // Step 3: Update read model
            await UpdateReadModelAsync(domainEvent, operationScope, ct);

            // Step 4: Trigger downstream processes
            await TriggerDownstreamProcessesAsync(domainEvent, operationScope, ct);

            // Mark successful completion
            operationScope.MarkSucceeded();
            _telemetry.RecordMetric("events.processed", 1, new()
            {
                { "event_type", domainEvent.GetType().Name },
                { "success", true }
            });
        }
        catch (Exception ex)
        {
            operationScope.RecordException(ex);
            _telemetry.RecordMetric("events.failed", 1, new()
            {
                { "event_type", domainEvent.GetType().Name },
                { "error_type", ex.GetType().Name }
            });
            throw;
        }
    }

    /// <summary>
    /// Example 1: Email notification with retry logic.
    /// </summary>
    private async Task SendConfirmationEmailAsync(
        OrderCreatedDomainEvent domainEvent,
        IOperationScope operationScope,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("SendConfirmationEmail", "notification");
        scope.SetTag("recipient", "customer@example.com");

        int retryCount = 0;
        const int maxRetries = 3;

        while (true)
        {
            try
            {
                // Simulate email send
                var emailSent = await SendEmailAsync(
                    new EmailMessage
                    {
                        To = domainEvent.CustomerEmail,
                        Subject = $"Order Confirmation: {domainEvent.OrderNumber}",
                        Body = GenerateConfirmationEmail(domainEvent),
                        IsHtml = true
                    },
                    ct);

                scope.SetTag("email.sent", emailSent);
                scope.SetTag("retry_attempts", retryCount);
                scope.MarkSucceeded();

                _telemetry.RecordMetric("emails.sent", 1, new() { { "type", "order_confirmation" } });
                return;
            }
            catch (Exception ex) when (retryCount < maxRetries && IsTransientError(ex))
            {
                retryCount++;
                scope.SetTag("retry_attempt", retryCount);

                // Exponential backoff: 100ms, 400ms, 1600ms
                var backoffMs = 100 * (int)Math.Pow(4, retryCount - 1);
                await Task.Delay(backoffMs, ct);

                _telemetry.RecordMetric("emails.retry", 1, new() { { "attempt", retryCount } });
            }
            catch (Exception ex)
            {
                scope.RecordException(ex);
                _telemetry.RecordMetric("emails.failed", 1);
                throw;
            }
        }
    }

    /// <summary>
    /// Example 2: Publish integration event to message broker.
    /// </summary>
    private async Task PublishIntegrationEventAsync(
        OrderCreatedDomainEvent domainEvent,
        IOperationScope operationScope,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("PublishIntegrationEvent", "event");
        scope.SetTag("event.exchange", "templates.events");
        scope.SetTag("event.routing_key", "order.created");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Create integration event
            var integrationEvent = new OrderCreatedIntegrationEvent
            {
                OrderId = domainEvent.OrderId,
                CustomerId = domainEvent.CustomerId,
                OrderNumber = domainEvent.OrderNumber,
                Total = domainEvent.Total,
                CreatedAt = domainEvent.OccurredAt,
                // Correlation ID for tracing across services
                CorrelationId = operationScope.TraceId
            };

            // Publish with retry
            // await _messagePublisher.PublishAsync(
            //     "order.created",
            //     integrationEvent,
            //     ct);

            await Task.Delay(10, ct);

            sw.Stop();

            scope.SetTag("message_size_bytes", 512); // Estimate
            scope.SetTag("publish_duration_ms", sw.ElapsedMilliseconds);
            scope.MarkSucceeded();

            _telemetry.RecordMetric("integration_events.published", 1, new()
            {
                { "event_type", "OrderCreated" }
            });
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            _telemetry.RecordMetric("integration_events.publish_failed", 1);
            throw;
        }
    }

    /// <summary>
    /// Example 3: Update read model for query optimization.
    /// </summary>
    private async Task UpdateReadModelAsync(
        OrderCreatedDomainEvent domainEvent,
        IOperationScope operationScope,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("UpdateReadModel", "mutation");
        scope.SetTag("model", "OrderReadModel");
        scope.SetTag("operation", "insert");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Update eventual consistency model
            // await _readModelService.UpdateOrderReadModelAsync(
            //     new OrderReadModel
            //     {
            //         OrderId = domainEvent.OrderId,
            //         OrderNumber = domainEvent.OrderNumber,
            //         CustomerId = domainEvent.CustomerId,
            //         Total = domainEvent.Total,
            //         Status = "Pending",
            //         CreatedAt = domainEvent.OccurredAt
            //     },
            //     ct);

            await Task.Delay(5, ct);

            sw.Stop();

            scope.SetTag("operation.duration_ms", sw.ElapsedMilliseconds);
            scope.MarkSucceeded();

            _telemetry.RecordMetric("read_model.updates", 1, new() { { "model", "OrderReadModel" } });
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            // Read model failure shouldn't fail event processing
            _telemetry.RecordMetric("read_model.update_failed", 1);
        }
    }

    /// <summary>
    /// Example 4: Trigger downstream processes (sagas, workflows).
    /// </summary>
    private async Task TriggerDownstreamProcessesAsync(
        OrderCreatedDomainEvent domainEvent,
        IOperationScope operationScope,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("TriggerDownstreamProcesses", "event");
        scope.SetTag("processes", 3);

        try
        {
            var tasks = new List<Task>
            {
                TriggerInventoryReservationAsync(domainEvent, ct),
                TriggerPaymentProcessingAsync(domainEvent, ct),
                TriggerFulfillmentAsync(domainEvent, ct)
            };

            // Run in parallel with timeout
            var combinedTask = Task.WhenAll(tasks);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await combinedTask;

            scope.SetTag("triggered_count", 3);
            scope.MarkSucceeded();

            _telemetry.RecordMetric("downstream.triggered", 1);
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            _telemetry.RecordMetric("downstream.trigger_failed", 1);
            throw;
        }
    }

    // Helper methods

    private static string GenerateConfirmationEmail(OrderCreatedDomainEvent domainEvent)
    {
        return $@"
            <h2>Order Confirmation</h2>
            <p>Thank you for your order!</p>
            <p><strong>Order Number:</strong> {domainEvent.OrderNumber}</p>
            <p><strong>Total:</strong> ${domainEvent.Total}</p>
        ";
    }

    private async Task<bool> SendEmailAsync(EmailMessage email, CancellationToken ct)
    {
        // Simulate email sending
        await Task.Delay(50, ct);
        return true;
    }

    private async Task TriggerInventoryReservationAsync(
        OrderCreatedDomainEvent domainEvent,
        CancellationToken ct)
    {
        await Task.Delay(10, ct);
    }

    private async Task TriggerPaymentProcessingAsync(
        OrderCreatedDomainEvent domainEvent,
        CancellationToken ct)
    {
        await Task.Delay(20, ct);
    }

    private async Task TriggerFulfillmentAsync(
        OrderCreatedDomainEvent domainEvent,
        CancellationToken ct)
    {
        await Task.Delay(15, ct);
    }

    private static bool IsTransientError(Exception ex)
    {
        // Determine if error is transient (retry-able)
        return ex is TimeoutException or IOException;
    }
}

// Domain event
public record OrderCreatedDomainEvent(
    Guid OrderId,
    Guid CustomerId,
    string OrderNumber,
    string CustomerEmail,
    decimal Total,
    DateTime OccurredAt);

// Integration event for message broker
public record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string OrderNumber,
    decimal Total,
    DateTime CreatedAt,
    string CorrelationId);

// Supporting types
public record EmailMessage(
    string To,
    string Subject,
    string Body,
    bool IsHtml = false);
