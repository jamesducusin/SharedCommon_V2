# Prompt: Architecture Review

Use this prompt for architectural assessment of a package or module.

---

## Prompt

```
Review the architecture of [package or module name].

Use the architecture-review skill (.claude/skills/architecture-review/SKILL.md).

Read:
1. src/[PackageName]/CLAUDE.md
2. docs/architecture/dependency-rules.md
3. docs/architecture/package-boundaries.md

Evaluate:
- Layering correctness (no violations)
- Dependency flow (correct direction)
- Package cohesion (single responsibility)
- Testability (injectable, no hidden state)
- Extension points (explicit and documented)

Report:
- Current state assessment
- Violations found (with file:line)
- Recommended changes
- Priority order for fixes

Reference docs/adr/ for prior decisions that constrain the design.
```

---

## Expected Output

Architecture assessment with prioritized findings and recommendations.
