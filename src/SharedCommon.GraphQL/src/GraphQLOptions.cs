using System.ComponentModel.DataAnnotations;

namespace SharedCommon.GraphQL;

/// <summary>Configuration for the SharedCommon GraphQL infrastructure.</summary>
public sealed class GraphQLOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:GraphQL";

    /// <summary>
    /// Maximum allowed query complexity score.
    /// Queries exceeding this value are rejected before execution.
    /// Defaults to 1000.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxAllowedComplexity { get; init; } = 1000;

    /// <summary>
    /// Maximum query depth. Prevents deeply nested queries.
    /// Defaults to 15.
    /// </summary>
    [Range(1, 100)]
    public int MaxAllowedExecutionDepth { get; init; } = 15;

    /// <summary>
    /// Whether to enable introspection. Disable in production.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableIntrospection { get; init; } = false;

    /// <summary>
    /// GraphQL endpoint path. Defaults to "/graphql".
    /// </summary>
    public string Path { get; init; } = "/graphql";

    /// <summary>
    /// Whether to enable the Banana Cake Pop IDE. Should only be true in Development.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableBananaCakePop { get; init; } = false;
}
