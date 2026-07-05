# Security Review Skill

Identify security issues before deployment.

## When to Use This Skill

Triggers:
- Auth/identity changes
- Handling secrets or tokens
- Input validation logic
- Encryption or hashing
- Any public-facing endpoint change

Ask Claude explicitly: "Use security-review skill"

## Input (What You Provide)

- Changed files or feature description
- Threat model context (if available)

## Output (What You Get)

- Security assessment report
- Prioritized issues with fixes

## Checklist

**Secrets & Credentials:**
- [ ] No hardcoded API keys, passwords, tokens
- [ ] No connection strings in code
- [ ] Secrets use User Secrets (dev) or environment variables
- [ ] No sensitive data in logs
- [ ] Secrets not in version control

**Authentication & Authorization:**
- [ ] Auth middleware present
- [ ] Claims validated correctly
- [ ] Token expiration enforced
- [ ] Permission checks on protected endpoints
- [ ] Service-to-service auth configured

**Input Validation:**
- [ ] All user input validated
- [ ] File uploads validated
- [ ] JSON payload size limits
- [ ] No buffer overflows
- [ ] Regex safe from ReDoS

**Data Protection:**
- [ ] Encryption at rest (if applicable)
- [ ] Encryption in transit (HTTPS)
- [ ] Hash algorithms appropriate (bcrypt, Argon2)
- [ ] Database encryption enabled
- [ ] No sensitive data in URLs

**Injection Prevention:**
- [ ] SQL injection impossible (parameterized queries)
- [ ] No command injection
- [ ] No template injection
- [ ] No LDAP injection
- [ ] Input sanitized for output context

**Access Control:**
- [ ] CORS properly scoped
- [ ] Open redirects blocked
- [ ] CSRF protection (if applicable)
- [ ] No privilege escalation vectors
- [ ] Rate limiting configured

**Error Handling:**
- [ ] Generic error messages to clients
- [ ] Detailed errors in logs only
- [ ] Stack traces never exposed
- [ ] Security errors handled specially

**Dependencies:**
- [ ] No known vulnerabilities (check NuGet)
- [ ] Dependencies up to date
- [ ] Supply chain attacks mitigated
- [ ] Transitive dependencies reviewed

## Report Format

```
Security Assessment: [Package Name]

**Critical Issues:** [count]
**High Issues:** [count]
**Medium Issues:** [count]
**Low Issues:** [count]

[Issue 1]
Location: [file:line]
Severity: [Critical/High/Medium/Low]
Description: [What's wrong]
Impact: [Business/data risk]
Fix: [Specific remediation]

See: docs/standards/security-guidelines.md
```

## References

See: docs/standards/security-guidelines.md
See: docs/adr/ADR-007-security-defaults.md
