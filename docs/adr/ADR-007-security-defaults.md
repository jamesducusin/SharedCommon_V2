# ADR-007: Security Defaults

**Status:** Accepted
**Date:** 2026-01-01

## Context

Security vulnerabilities in shared infrastructure packages affect every consuming service. Secure defaults eliminate entire classes of vulnerabilities without requiring consumers to make correct choices.

## Decision

### Secrets Management

- **Never** hardcode secrets in source code or configuration files checked into version control
- Development: `dotnet user-secrets`
- CI/CD: environment variables or secret manager references
- Production: Azure Key Vault, AWS Secrets Manager, or Kubernetes Secrets

### Cryptography

- Password hashing: **BCrypt** (cost factor ≥ 12) or **Argon2id**
- Symmetric encryption: **AES-256-GCM**
- Asymmetric: **RSA-2048** minimum, **EC P-256** preferred
- Random generation: `RandomNumberGenerator` (never `Random`)
- Forbidden: MD5, SHA1 for security, DES, 3DES, RC4

### JWT Tokens

- Algorithm: **RS256** or **ES256** (never HS256 with shared secrets)
- Expiry: access tokens max 15 minutes, refresh tokens max 24 hours
- Claims: minimal (sub, iat, exp, jti) — no sensitive data in payload
- Validation: issuer, audience, expiry, signing key — all validated

### Input Validation

- All external input validated at system boundary
- Max payload sizes configured on HTTP endpoints
- Parameterized queries only (never string-concatenated SQL)
- File upload: type, size, and content validation

### HTTP Security Headers (via Middleware)

- `Content-Security-Policy`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy`

### Logging Security

- Never log: passwords, tokens, full card numbers, SSNs, full API keys
- Log partial identifiers only: last 4 digits, first/last chars of tokens
- PII log filtering configured by default in SharedCommon.Logging

## Consequences

- Consuming services inherit secure defaults without configuration
- Security audit scope reduced to service-specific logic
- Consistent cryptographic choices across all services

See: docs/standards/security-guidelines.md
