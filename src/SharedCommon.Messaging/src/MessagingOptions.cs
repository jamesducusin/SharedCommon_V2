using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Messaging;

/// <summary>Messaging transport backend selection.</summary>
public enum MessagingTransport
{
    /// <summary>RabbitMQ via AMQP (default). Best for general-purpose work queues.</summary>
    RabbitMQ,

    /// <summary>Apache Kafka. Best for high-throughput event streaming and log-based messaging.</summary>
    Kafka
}

/// <summary>Configuration for the SharedCommon messaging infrastructure.</summary>
public sealed class MessagingOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:Messaging";

    /// <summary>
    /// Transport backend to use. Defaults to <see cref="MessagingTransport.RabbitMQ"/>.
    /// Set to <c>Kafka</c> to switch to Apache Kafka.
    /// </summary>
    public MessagingTransport Transport { get; init; } = MessagingTransport.RabbitMQ;

    /// <summary>RabbitMQ configuration. Used when <see cref="Transport"/> is <see cref="MessagingTransport.RabbitMQ"/>.</summary>
    public RabbitMqOptions RabbitMQ { get; init; } = new();

    /// <summary>Kafka configuration. Used when <see cref="Transport"/> is <see cref="MessagingTransport.Kafka"/>.</summary>
    public KafkaOptions Kafka { get; init; } = new();

    /// <summary>Retry configuration for failed message processing. Applies to all transports.</summary>
    public RetryOptions Retry { get; init; } = new();
}

/// <summary>RabbitMQ connection settings.</summary>
public sealed class RabbitMqOptions
{
    /// <summary>RabbitMQ host name or IP. Defaults to "localhost".</summary>
    [Required]
    public string Host { get; init; } = "localhost";

    /// <summary>RabbitMQ AMQP port. Defaults to 5672.</summary>
    [Range(1, 65535)]
    public int Port { get; init; } = 5672;

    /// <summary>RabbitMQ virtual host. Defaults to "/".</summary>
    public string VirtualHost { get; init; } = "/";

    /// <summary>RabbitMQ username. Defaults to "guest". Use a non-guest user in production.</summary>
    public string Username { get; init; } = "guest";

    /// <summary>
    /// RabbitMQ password. Defaults to "guest".
    /// Never place this in appsettings.json. Use User Secrets or a secrets manager.
    /// </summary>
    public string Password { get; init; } = "guest";
}

/// <summary>Apache Kafka connection settings.</summary>
public sealed class KafkaOptions
{
    /// <summary>
    /// Comma-separated list of Kafka broker addresses (host:port).
    /// Example: "broker1:9092,broker2:9092".
    /// </summary>
    [Required]
    public string BootstrapServers { get; init; } = "localhost:9092";

    /// <summary>Consumer group ID. All instances of the same service share one group.</summary>
    [Required]
    public string ConsumerGroupId { get; init; } = "shared-common";

    /// <summary>
    /// SASL username for authenticated brokers. Leave empty for unauthenticated (dev only).
    /// Never place credentials in appsettings.json — use User Secrets or a secrets manager.
    /// </summary>
    public string? SaslUsername { get; init; }

    /// <summary>SASL password. See <see cref="SaslUsername"/>.</summary>
    public string? SaslPassword { get; init; }

    /// <summary>
    /// Security protocol. Options: Plaintext, Ssl, SaslPlaintext, SaslSsl.
    /// Defaults to "Plaintext" for local development.
    /// </summary>
    public string SecurityProtocol { get; init; } = "Plaintext";

    /// <summary>
    /// Number of partitions for auto-created topics.
    /// Only relevant when topic auto-creation is enabled on the broker.
    /// </summary>
    [Range(1, 1000)]
    public int DefaultTopicPartitions { get; init; } = 1;

    /// <summary>
    /// Topic replication factor for auto-created topics.
    /// Must be &lt;= the number of Kafka brokers.
    /// </summary>
    [Range(1, 32767)]
    public short DefaultTopicReplicationFactor { get; init; } = 1;
}

/// <summary>Retry configuration for message consumers. Applies to all transports.</summary>
public sealed class RetryOptions
{
    /// <summary>Maximum number of delivery attempts before routing to DLQ. Defaults to 3.</summary>
    [Range(1, 10)]
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Minimum back-off interval for retry. Defaults to 1 second.</summary>
    public TimeSpan MinInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Maximum back-off interval for retry. Defaults to 30 seconds.</summary>
    public TimeSpan MaxInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Interval delta for exponential back-off. Defaults to 2 seconds.</summary>
    public TimeSpan IntervalDelta { get; init; } = TimeSpan.FromSeconds(2);
}
