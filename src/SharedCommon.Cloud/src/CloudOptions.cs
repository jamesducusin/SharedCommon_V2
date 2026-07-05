using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Cloud;

/// <summary>Cloud provider selection.</summary>
public enum CloudProvider
{
    /// <summary>Microsoft Azure (Blob Storage, Key Vault, Service Bus).</summary>
    Azure,

    /// <summary>Amazon Web Services (S3, Secrets Manager, SQS).</summary>
    AWS
}

/// <summary>Top-level configuration for the SharedCommon cloud abstraction layer.</summary>
public sealed class CloudOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:Cloud";

    /// <summary>
    /// Active cloud provider. Controls which SDK implementations are registered.
    /// </summary>
    [Required]
    public CloudProvider Provider { get; init; } = CloudProvider.Azure;

    /// <summary>Azure-specific settings. Required when <see cref="Provider"/> is <see cref="CloudProvider.Azure"/>.</summary>
    public AzureCloudOptions Azure { get; init; } = new();

    /// <summary>AWS-specific settings. Required when <see cref="Provider"/> is <see cref="CloudProvider.AWS"/>.</summary>
    public AwsCloudOptions Aws { get; init; } = new();
}

/// <summary>Azure SDK configuration.</summary>
public sealed class AzureCloudOptions
{
    /// <summary>
    /// Azure Storage account name or connection string.
    /// Prefer Managed Identity — if using a connection string, store it in Key Vault, never in appsettings.json.
    /// </summary>
    public string? StorageAccountName { get; init; }

    /// <summary>
    /// Azure Key Vault URI (e.g., https://my-vault.vault.azure.net/).
    /// Required when using <see cref="ISecretManagerService"/>.
    /// </summary>
    public string? KeyVaultUri { get; init; }

    /// <summary>
    /// Azure Service Bus namespace FQDN (e.g., my-namespace.servicebus.windows.net).
    /// Required when using <see cref="ICloudQueueService"/>.
    /// </summary>
    public string? ServiceBusNamespace { get; init; }

    /// <summary>
    /// Use Managed Identity for authentication. Defaults to <c>true</c> for production security.
    /// When <c>false</c>, connection strings must be supplied.
    /// </summary>
    public bool UseManagedIdentity { get; init; } = true;
}

/// <summary>AWS SDK configuration.</summary>
public sealed class AwsCloudOptions
{
    /// <summary>AWS region (e.g., "us-east-1"). Required.</summary>
    public string Region { get; init; } = "us-east-1";

    /// <summary>
    /// AWS access key ID. Use IAM roles in production — this field is for local/dev only.
    /// Never store in appsettings.json. Use User Secrets or environment variables.
    /// </summary>
    public string? AccessKeyId { get; init; }

    /// <summary>
    /// AWS secret access key. See <see cref="AccessKeyId"/>. Never hardcode.
    /// </summary>
    public string? SecretAccessKey { get; init; }

    /// <summary>
    /// Override endpoint URL for local testing (LocalStack, Azurite).
    /// Example: "http://localhost:4566".
    /// Leave null in production.
    /// </summary>
    public string? ServiceUrl { get; init; }
}
