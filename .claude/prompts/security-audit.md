# Prompt: Security Audit

Use this prompt to run a security review on changed code.

---

## Prompt

```
Perform a security audit on the following changed files:
[paste file list or diff]

Use the security-review skill (.claude/skills/security-review/SKILL.md).

Go through every checklist item and report:
- Critical issues (block merge)
- High issues (fix before next release)
- Medium issues (fix in follow-up)
- Low issues (track as tech debt)

For each issue provide:
- Location (file:line)
- Severity
- Description
- Impact
- Specific fix

Reference docs/standards/security-guidelines.md for remediation guidance.
```

---

## Expected Output

Security Assessment report with prioritized issue list and fixes.
