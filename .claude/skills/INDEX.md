# Skill Selection Guide

Use this to pick the right skill for your task.

## When Starting a New Package

→ Use: **package-design/SKILL.md**

Triggers:
- Creating new package
- Defining public API
- Setting up dependency structure
- Designing extension points

Input: Package name, purpose, consumers
Output: Folder structure, CLAUDE.md, IServiceCollection extension, test scaffold

---

## When Reviewing Architecture

→ Use: **architecture-review/SKILL.md**

Triggers:
- Evaluating package boundaries
- Checking layering violations
- Assessing dependency flow
- Planning refactoring

Input: Package or module name
Output: Assessment report with recommendations

---

## When Working with Security

→ Use: **security-review/SKILL.md**

Triggers:
- Auth/identity changes
- Handling secrets/tokens
- Input validation
- Encryption/hashing

Input: Changed files or feature description
Output: Security assessment with fixes

---

## When Optimizing Performance

→ Use: **performance-review/SKILL.md**

Triggers:
- Allocations on hot paths
- Async/await patterns
- Caching strategies
- Database queries

Input: Code or performance concern
Output: Optimization report with benchmarks

---

## When Reviewing Code

→ Use: **code-review/SKILL.md**

Triggers:
- Pull request review
- Code validation
- Pattern enforcement
- Standard adherence

Input: Code to review
Output: Approval or specific rejection reasons

---

## When Adding Tests

→ Use: **testing/SKILL.md**

Triggers:
- Unit test design
- Integration test setup
- Architecture test constraints
- Test data fixtures

Input: Feature or module to test
Output: Test structure, fixtures, edge cases

---

## When Adding Logging/Tracing

→ Use: **observability/SKILL.md**

Triggers:
- Structured logging setup
- Correlation ID propagation
- Metrics instrumentation
- Distributed tracing

Input: Module name
Output: Logging patterns, trace setup

---

## When Working with APIs

→ Use: **clean-architecture/SKILL.md**

Triggers:
- API layer design
- Request/response models
- Dependency injection
- Service boundaries

Input: API design or refactoring
Output: Validated architecture

---

## Default: Ask Claude Explicitly

If none match, ask: "Use the appropriate .claude/skill for this task."

Claude will select based on task context.
