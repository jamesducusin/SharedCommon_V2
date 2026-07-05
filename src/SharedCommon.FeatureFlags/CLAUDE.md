# SharedCommon.FeatureFlags

Feature flag evaluation backed by Microsoft.FeatureManagement.

## API Surface

- `IFeatureFlagService` — `IsEnabledAsync`, `IsEnabledForAsync<TContext>`, `GetEnabledFeaturesAsync`
- `FeatureFlagContext` — user/tenant/group context for targeting filters
- `FeatureFlagOptions` — `CacheTtlSeconds`, `LogEvaluations`
- `AddSharedFeatureFlags(IConfiguration)` — registers `IFeatureManager` and `IFeatureFlagService`
- Feature definitions live under the standard `FeatureManagement` configuration section

## Rules

**Must:**
- Feature flag names are PascalCase constants (e.g., `"NewCheckoutFlow"`)
- All feature-gated code paths have a fallback to the stable path
- Use `[FeatureGate("FlagName")]` on controller actions where appropriate
- Never gate on a flag that doesn't exist in configuration (startup validation)

**Forbidden:**
- Hardcoding flag evaluations (always use `IFeatureFlagService`)
- Storing flag state in static or singleton fields
- Feature flags that control security or auth behavior
- Long-lived flags with no removal plan (flags are temporary by design)

## Built-in Filters

| Filter | Use case |
|--------|----------|
| `AlwaysOn` | Always enabled |
| `Percentage` | Gradual rollout by request |
| `Targeting` | Specific users or groups |
| `TimeWindow` | Seasonal features |

## Design Decisions

Microsoft.FeatureManagement is the backing library — no external service required.
External providers (LaunchDarkly, Azure App Config) can be added by replacing the
`IFeatureManager` registration without changing `IFeatureFlagService` consumers.

## Test Strategy

- Unit test `FeatureFlagService` with a mock `IFeatureManager`
- Integration tests use `AddFeatureManagement` with in-memory config overrides
- Test `IsEnabledForAsync` with `FeatureFlagContext` targeting

## Extension Points

- Swap in LaunchDarkly or Azure App Config by replacing `IFeatureManager` registration
- Custom `IContextualFeatureFilter<TContext>` for domain-specific targeting logic
