# Code Review Skill

Review code for correctness, standards, security.

## When to Use This Skill

Triggers:
- Pull request review
- Code validation before merge
- Pattern enforcement check
- Standard adherence audit

Ask Claude explicitly: "Use code-review skill"

## Input (What You Provide)

- Code diff or files to review
- Context of what changed and why

## Output (What You Get)

- Approval with notes, OR
- Rejection with specific category, issue, fix, and reference

## Approval Criteria

Code passes all checks below before approval.

### Architecture ✓

**Must:**
- [ ] Follows Clean Architecture layering
- [ ] No infrastructure leakage
- [ ] Dependency direction correct
- [ ] No circular dependencies
- [ ] Package boundaries respected

**Reject if:**
- Direct database calls from controllers
- Business logic in middleware
- Infrastructure in abstractions
- Circular dependencies detected

---

### Standards ✓

**Must:**
- [ ] Nullable reference types enabled
- [ ] Warnings treated as errors
- [ ] Async/await + CancellationToken
- [ ] ConfigureAwait(false) in libraries
- [ ] XML docs on public APIs
- [ ] Immutable models preferred

**Reject if:**
- Methods > 30 lines without reason
- Console.WriteLine present
- Magic strings/numbers
- Static mutable state
- Reflection without justification

---

### Security ✓

**Must:**
- [ ] No hardcoded secrets
- [ ] Input validation present
- [ ] Authentication checked
- [ ] No overly permissive access

**Reject if:**
- Hardcoded API keys, passwords, tokens
- SQL injection risk
- Missing authorization checks
- Sensitive data logged
- Deserialization vulnerability

---

### Observability ✓

**Must:**
- [ ] ILogger injected
- [ ] Key operations logged
- [ ] Correlation ID included
- [ ] Errors properly logged
- [ ] Performance logged (if slow)

**Reject if:**
- No logging in services
- Correlation ID missing
- Exception swallowed silently
- Sensitive data logged

---

### Testing ✓

**Must:**
- [ ] Unit tests for public API
- [ ] Happy path + error cases
- [ ] Mocks injected via DI
- [ ] No database in unit tests

**Reject if:**
- No tests for new code
- Tests coupled to implementation
- Tests hit real database
- Test coverage < 70%

---

## Rejection Message Template

```
REJECTED: [Category]

Issue: [Specific problem]

Why: [Impact on codebase]

Fix: [Specific guidance]

See: [docs/standards/...md or .claude/skills/...]
```

## Example Rejections

**Hardcoded Secret:**
```
REJECTED: Security

Issue: API key hardcoded in appsettings.json

Why: Exposes credentials to version control

Fix: Use User Secrets in development, environment variables in production
See: docs/standards/security-guidelines.md
```

**Missing Logging:**
```
REJECTED: Observability

Issue: Service methods lack logging

Why: Production issues cannot be traced

Fix: Inject ILogger, log method entry/exit for critical paths
See: docs/standards/logging-guidelines.md
```

**Circular Dependency:**
```
REJECTED: Architecture

Issue: SharedCommon.Auth → SharedCommon.Core → SharedCommon.Auth

Why: Creates build/runtime issues, prevents independent use

Fix: Extract shared types to SharedCommon.Core.Models
See: docs/architecture/dependency-rules.md
```

## References

See: docs/standards/coding-standards.md
See: docs/architecture/dependency-rules.md
See: .claude/skills/security-review/SKILL.md
