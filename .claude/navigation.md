# Context Loading Strategy

Load only what you need. Omit everything else.

## Task: Add New Package

**Time:** 2 minutes | **Tokens:** ~1.5KB

1. Read: `.claude/skills/package-design/SKILL.md` (checklist)
2. Reference: `src/SharedCommon.Logging/CLAUDE.md` (template)
3. Use template: `tools/templates/package-template/`

**Skip:**
- Architecture docs
- Other package CLAUDEs
- Performance guides

---

## Task: Implement Package Feature

**Time:** 3 minutes | **Tokens:** ~2KB

1. Read: `src/[PackageName]/CLAUDE.md` (rules)
2. Reference: `docs/standards/coding-standards.md` (if new style question)
3. Check: `docs/adr/` (if architectural question)

**Skip:**
- Other packages
- Infra setup
- Deployment docs

---

## Task: Code Review (PR)

**Time:** 2 minutes | **Tokens:** ~1KB

1. Load: `.claude/skills/code-review/SKILL.md` (rejection checklist)
2. Reference: `docs/standards/` (as needed)

**Skip:**
- Everything else

---

## Task: Optimize Performance

**Time:** 3 minutes | **Tokens:** ~2KB

1. Read: `.claude/skills/performance-review/SKILL.md`
2. Reference: `docs/standards/performance-guidelines.md`
3. Check: `tests/SharedCommon.PerformanceTests/` (benchmarks)

**Skip:**
- Package docs
- Architecture overview

---

## Task: Security Audit

**Time:** 3 minutes | **Tokens:** ~2KB

1. Load: `.claude/skills/security-review/SKILL.md` (checklist)
2. Reference: `docs/standards/security-guidelines.md`
3. Check: `docs/adr/ADR-007-security-defaults.md`

**Skip:**
- Implementation details
- Other packages

---

## Task: Architecture Review

**Time:** 5 minutes | **Tokens:** ~3KB

1. Read: `.claude/skills/architecture-review/SKILL.md`
2. Reference: `docs/architecture/overview.md`
3. Check: `docs/architecture/dependency-rules.md`
4. Reference: Relevant `src/[Package]/CLAUDE.md` files

**Skip:**
- Implementation details
- Standards (unless needed)

---

## Task: Understand Full System

**Time:** 15 minutes | **Tokens:** ~10KB

1. CLAUDE.md (root)
2. docs/architecture/overview.md
3. docs/architecture/layering.md
4. Skim: src/*/CLAUDE.md (read headings only)
5. Reference: docs/adr/ (as needed)

**This is rare. Most tasks don't need it.**

---

## Rule: Stop When Context Stabilizes

Stop reading when you can confidently answer the question.

Example:
- "Add middleware" → Just read middleware CLAUDE.md + skill
- "Refactor auth layer" → Read auth CLAUDE.md + architecture guide
- "Is this design good?" → Read architecture skill + ADRs

**Don't:**
- Read every file in docs/
- Load all package CLAUDEs
- Dump entire repo context

**Do:**
- Load minimum needed
- Reference files when stuck
- Ask for clarification if uncertain
