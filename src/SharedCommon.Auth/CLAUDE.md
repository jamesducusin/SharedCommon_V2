# SharedCommon.Auth

Authentication and authorization abstractions.
JWT validation, API key auth, current user context.

## API Surface

- `ICurrentUser` — userId, claims, roles, tenant
- `ICurrentUserAccessor` — resolves ICurrentUser from HttpContext
- `ITokenService` — issue and validate JWT tokens
- `IApiKeyValidator` — validate API key credentials
- `AddSharedAuth(IConfiguration)` — DI registration
- `AuthOptions` — JWT settings, token expiry, allowed algorithms

## Rules

**Must:**
- Use RS256 or ES256 for JWT (never HS256 with shared secrets)
- Validate: issuer, audience, expiry, signing key — all four
- Access tokens: max 15 min TTL
- Refresh tokens: max 24 hours TTL
- Log auth failures at Warning level with UserId (not credentials)

**Forbidden:**
- HS256 in production
- Storing tokens in local/session storage (document this in README)
- Logging token values or passwords
- Bypassing expiry validation
- Storing auth state in static fields

## Design Decisions

See: docs/adr/ADR-007-security-defaults.md

## Test Strategy

- Unit test token validation with known keys
- Test expiry, invalid issuer, invalid audience — all should fail validation
- Integration tests use TestServer with real JWT middleware
- Provide `FakeCurrentUser` for testing downstream services

## Extension Points

- Custom `ICurrentUser` enrichment via `ICurrentUserEnricher`
- Custom claim mapping via `IClaimsMappingPolicy`
