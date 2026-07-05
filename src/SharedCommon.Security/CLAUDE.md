# SharedCommon.Security

Cryptographic utilities, secret management abstractions, data protection.
Does NOT include authentication (that's SharedCommon.Auth).

## API Surface

- `IEncryptionService` — AES-256-GCM encrypt/decrypt
- `IHashingService` — BCrypt/Argon2 password hashing and verification
- `ISecretProvider` — abstract secret retrieval (User Secrets, Key Vault, env vars)
- `DataProtectionExtensions` — ASP.NET Data Protection setup
- `SecurityOptions` — algorithm configuration

## Rules

**Must:**
- Use `RandomNumberGenerator` for all random values (never `System.Random`)
- Default to AES-256-GCM for symmetric encryption
- Default to Argon2id for password hashing (BCrypt as fallback)
- Log security events (but never log the secret itself)
- Dispose of `ICryptoTransform` and key material properly

**Forbidden:**
- MD5, SHA1 for security purposes
- DES, 3DES, RC2
- Hardcoded encryption keys or IVs
- Symmetric encryption without authenticated encryption (AES-CBC without MAC)
- Logging plaintext secrets, passwords, or decrypted values

## Design Decisions

See: docs/adr/ADR-007-security-defaults.md

## Test Strategy

- Test encrypt/decrypt round trips
- Test hashing produces different hashes for same input (salt)
- Test verification succeeds for correct password, fails for wrong
- Do NOT store real test secrets in test code — use constants for test values only

## Extension Points

- Custom `ISecretProvider` for additional secret stores
- Custom key derivation via `IKeyDerivationService`
