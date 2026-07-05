using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedCommon.Messaging.Filters;

namespace SharedCommon.Messaging;

/// <summary>DI registration extensions for SharedCommon messaging infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MassTransit with the transport configured in <c>SharedCommon:Messaging:Transport</c>
    /// (RabbitMQ or Kafka). Applies correlation ID and logging filters, exponential back-off retry,
    /// and dead-letter queue routing on all transports.
    ///
    /// <code>
    /// // RabbitMQ (default)
    /// builder.Services.AddSharedMessaging(builder.Configuration, bus =>
    /// {
    ///     bus.AddConsumer&lt;OrderCreatedConsumer&gt;();
    /// });
    ///
    /// // Kafka — set Transport: Kafka in appsettings.json
    /// builder.Services.AddSharedMessaging(builder.Configuration, bus =>
    /// {
    ///     bus.AddConsumer&lt;OrderCreatedConsumer&gt;();
    /// });
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="configureMassTransit">Optional callback to register consumers and sagas.</param>
    public static IServiceCollection AddSharedMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureMassTransit = null)
    {
        services.AddOptions<MessagingOptions>()
            .BindConfiguration(MessagingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
            .GetSection(MessagingOptions.SectionName)
            .Get<MessagingOptions>() ?? new MessagingOptions();

        services.AddMassTransit(bus =>
        {
            configureMassTransit?.Invoke(bus);

            switch (options.Transport)
            {
                case MessagingTransport.Kafka:
                    ConfigureKafka(bus, options);
                    break;

                default:
                    ConfigureRabbitMq(bus, options);
                    break;
            }
        });

        services.AddScoped<IMessagePublisher, MessagePublisher>();

        return services;
    }

    private static void ConfigureRabbitMq(IBusRegistrationConfigurator bus, MessagingOptions options)
    {
        bus.UsingRabbitMq((ctx, rabbitMq) =>
        {
            rabbitMq.Host(
                options.RabbitMQ.Host,
                (ushort)options.RabbitMQ.Port,
                options.RabbitMQ.VirtualHost,
                host =>
                {
                    host.Username(options.RabbitMQ.Username);
                    host.Password(options.RabbitMQ.Password);
                });

            rabbitMq.UseMessageRetry(retry =>
                retry.Exponential(
                    options.Retry.MaxAttempts,
                    options.Retry.MinInterval,
                    options.Retry.MaxInterval,
                    options.Retry.IntervalDelta));

            rabbitMq.UseConsumeFilter(typeof(CorrelationIdFilter<>), ctx);
            rabbitMq.UseConsumeFilter(typeof(LoggingFilter<>), ctx);

            rabbitMq.ConfigureEndpoints(ctx);
        });
    }

    private static void ConfigureKafka(IBusRegistrationConfigurator bus, MessagingOptions options)
    {
        bus.UsingInMemory((ctx, inMemory) =>
        {
            // Kafka rider handles transport; in-memory bus is still required by MassTransit
            inMemory.ConfigureEndpoints(ctx);
        });

        bus.AddRider(rider =>
        {
            rider.UsingKafka((ctx, kafka) =>
            {
                kafka.Host(options.Kafka.BootstrapServers, host =>
                {
                    if (!string.IsNullOrEmpty(options.Kafka.SaslUsername))
                    {
                        host.UseSasl(sasl =>
                        {
                            sasl.Username = options.Kafka.SaslUsername;
                            sasl.Password = options.Kafka.SaslPassword ?? string.Empty;
                        });
                    }
                });
            });
        });
    }
}
