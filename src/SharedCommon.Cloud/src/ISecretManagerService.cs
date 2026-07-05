namespace SharedCommon.Cloud;

/// <summary>
/// Abstraction over cloud secret stores (Azure Key Vault, AWS Secrets Manager).
/// Use this to retrieve runtime secrets instead of reading from configuration directly.
/// </summary>
public interface ISecretManagerService
{
    /// <summary>
    /// Retrieves the value of a named secret.
    /// </summary>
    /// <param name="secretName">Provider-specific secret name or ARN.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The secret value, or <c>null</c> if not found.</returns>
    Task<string?> GetSecretAsync(string secretName, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a secret and deserializes it from JSON into <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Target type.</typeparam>
    /// <param name="secretName">Provider-specific secret name or ARN.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The deserialized secret, or <c>null</c> if not found.</returns>
    Task<T?> GetSecretAsync<T>(string secretName, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Sets or updates a secret value.
    /// </summary>
    /// <param name="secretName">Provider-specific secret name.</param>
    /// <param name="value">The secret value to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetSecretAsync(string secretName, string value, CancellationToken ct = default);
}
