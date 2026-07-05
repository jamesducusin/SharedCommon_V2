# Architecture Review Skill

Evaluate package structure, layering, and dependency flow.

## When to Use This Skill

Triggers:
- Evaluating package boundaries
- Checking for layering violations
- Assessing dependency direction
- Planning a refactoring
- Introducing a new cross-cutting concern

Ask Claude explicitly: "Use architecture-review skill"

## Input (What You Provide)

- Package or module name
- Proposed change or concern

## Output (What You Get)

- Assessment report with findings
- Specific recommendations and trade-offs

## Checklist

**Layering:**
- [ ] Controllers → Application → Domain (never reversed)
- [ ] Infrastructure depends on abstractions, not vice versa
- [ ] No cross-layer direct references
- [ ] Shared types live in Core, not scattered

**Dependency Flow:**
- [ ] Dependencies flow inward only
- [ ] No circular references
- [ ] No package references across domain boundaries without abstraction
- [ ] Optional packages marked optional

**Package Cohesion:**
- [ ] Single clear responsibility per package
- [ ] Public API is minimal and stable
- [ ] Internal implementation hidden
- [ ] Extension points explicit

**Testability:**
- [ ] All dependencies injectable
- [ ] No static state
- [ ] Deterministic behavior
- [ ] No hidden global state

## Decision Tree

```
Does the change introduce a new dependency direction?
  Yes → Check dependency-rules.md before proceeding
  No  → Continue

Does it add shared types?
  Yes → Do they belong in SharedCommon.Core?
  No  → Continue

Does it create a new package?
  Yes → Follow package-design/SKILL.md
  No  → Continue

Does it touch multiple packages?
  Yes → Run architecture tests before committing
  No  → Local review sufficient
```

## Common Mistakes

❌ Placing domain types in infrastructure packages
- Why: Forces consumers to take on infrastructure deps
- Fix: Move types to SharedCommon.Core

❌ Packages referencing siblings they should not know about
- Why: Creates hidden coupling
- Fix: Introduce abstraction or merge packages

❌ Fat packages with multiple responsibilities
- Why: Changes ripple across unrelated consumers
- Fix: Split on responsibility boundary

## References

See: docs/architecture/overview.md
See: docs/architecture/dependency-rules.md
See: docs/architecture/package-boundaries.md
See: docs/adr/ (for prior decisions)
