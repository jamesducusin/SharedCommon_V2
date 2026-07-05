namespace SharedCommon.Grpc;

/// <summary>Configuration options for SharedCommon gRPC infrastructure.</summary>
public sealed class GrpcOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:Grpc";

    /// <summary>Enable gRPC reflection service. Should only be true in Development.</summary>
    public bool EnableReflection { get; init; } = false;

    /// <summary>Enable the gRPC health check service (grpc-health-v1).</summary>
    public bool EnableHealthCheck { get; init; } = true;

    /// <summary>Maximum inbound message size in bytes. Defaults to 4 MB.</summary>
    public int MaxReceiveMessageSizeBytes { get; init; } = 4 * 1024 * 1024;

    /// <summary>Maximum outbound message size in bytes. Defaults to 4 MB.</summary>
    public int MaxSendMessageSizeBytes { get; init; } = 4 * 1024 * 1024;

    /// <summary>Metadata key used to propagate the correlation ID. Case-insensitive.</summary>
    public string CorrelationIdHeader { get; init; } = "x-correlation-id";
}
