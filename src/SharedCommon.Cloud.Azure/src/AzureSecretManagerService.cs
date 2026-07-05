using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace SharedCommon.Cloud.Azure;

/// <summary>
/// Azure Key Vault implementation of <see cref="ISecretManagerService"/>.
/// Always uses <see cref="DefaultAzureCredential"/> (Managed Identity in production,
/// Azure CLI / environment variables in development).
/// </summary>
internal sealed class AzureSecretManagerService : ISecretManagerService
{
    private readonly SecretClient _client;
    private readonly ILogger<AzureSecretManagerService> _logger;

    public AzureSecretManagerService(IOptions<CloudOptions> options, ILogger<AzureSecretManagerService> logger)
    {
        _logger = logger;
        var keyVaultUri = options.Value.Azure.KeyVaultUri
            ?? throw new InvalidOperationException(
                "CloudOptions:Azure:KeyVaultUri is required for ISecretManagerService.");

        _client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
    }

    /// <inheritdoc/>
    public async Task<string?> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetSecretAsync(secretName, cancellationToken: ct);
            return response.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret {SecretName} not found in Key Vault", secretName);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetSecretAsync<T>(string secretName, CancellationToken ct = default)
        where T : class
    {
        var json = await GetSecretAsync(secretName, ct);
        return json is null ? null : JsonSerializer.Deserialize<T>(json);
    }

    /// <inheritdoc/>
    public async Task SetSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        await _client.SetSecretAsync(secretName, value, ct);
        _logger.LogDebug("Set secret {SecretName} in Key Vault", secretName);
    }
}
