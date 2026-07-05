using Polly;

namespace SharedCommon.Resiliency;

/// <summary>Provides named Polly v8 resilience pipelines for use outside the HTTP client infrastructure.</summary>
public interface IResiliencyPolicyProvider
{
    /// <summary>Returns a pre-configured resilience pipeline by <paramref name="name"/>.</summary>
    ResiliencePipeline GetPipeline(string name);

    /// <summary>Returns a pre-configured typed resilience pipeline by <paramref name="name"/>.</summary>
    ResiliencePipeline<T> GetPipeline<T>(string name);
}
