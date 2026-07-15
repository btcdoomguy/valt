---
phase: 32
slug: factual-fixes-and-cross-page-accuracy
status: approved
nyquist_compliant: true
wave_0_complete: N/A
created: 2026-07-15
---

# Phase 32 — Validation Strategy

> Per-phase validation contract for a documentation-only phase. Verification is performed by file inspection and the sibling `valt-docs` MkDocs build, with no application code tests.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Shell-based documentation verification + MkDocs `build --strict` |
| **Config file** | `../valt-docs/mkdocs.yml` |
| **Quick run command** | `grep`-based factual checks per page |
| **Full suite command** | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run the relevant `grep`-based factual checks from the plan's `<verify>` block.
- **After every plan wave:** Run `mkdocs build --strict` in `../valt-docs/.venv`.
- **Before `/gsd-verify-work`:** Full suite must be green (MkDocs build exits 0 with no `ERROR` lines).
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 32-01-01 | 01 | 1 | ACC-01, ACC-02 | T-32-01 | Public docs contain only factual, source-evidence-backed claims | docs | `test -f ../valt-docs/mkdocs.yml && grep -q "LiteDB" ../valt-docs/docs/guia/instalacao.md && ! grep -q "SQLite" ../valt-docs/docs/guia/instalacao.md && grep -q "LiteDB" ../valt-docs/docs/guia/instalacao.en.md && ! grep -q "SQLite" ../valt-docs/docs/guia/instalacao.en.md && grep -q "CSV" ../valt-docs/docs/referencia/faq.md && grep -q "Importar Transações" ../valt-docs/docs/referencia/faq.md && grep -q "CSV" ../valt-docs/docs/referencia/faq.en.md && grep -q "Import Transactions" ../valt-docs/docs/referencia/faq.en.md` | ✅ | ✅ green |
| 32-01-02 | 01 | 1 | ACC-03, ACC-04 | T-32-01 | Getting Started and Assets pages match the source-of-truth code counts and labels | docs | `grep -q "quatro" ../valt-docs/docs/guia/primeiros-passos.md && grep -q "four main tabs" ../valt-docs/docs/guia/primeiros-passos.en.md && grep -q "9 tipos de ativo" ../valt-docs/docs/funcionalidades/ativos.md && grep -q "Empréstimo BTC" ../valt-docs/docs/funcionalidades/ativos.md && grep -q "Empréstimo BTC (Credor)" ../valt-docs/docs/funcionalidades/ativos.md && grep -q "9 asset types" ../valt-docs/docs/funcionalidades/ativos.en.md && grep -q "BTC Loan" ../valt-docs/docs/funcionalidades/ativos.en.md && grep -q "BTC Lending" ../valt-docs/docs/funcionalidades/ativos.en.md` | ✅ | ✅ green |
| 32-01-03 | 01 | 1 | ACC-05 | T-32-01 | Reports page removes stale "in development" note and accurately describes export behavior | docs | `! grep -q "Em desenvolvimento" ../valt-docs/docs/funcionalidades/relatorios.md && ! grep -q "In development" ../valt-docs/docs/funcionalidades/relatorios.en.md && grep -q "Exportar Transações" ../valt-docs/docs/funcionalidades/relatorios.md && grep -q "Export Transactions" ../valt-docs/docs/funcionalidades/relatorios.en.md && grep -q "dashboards" ../valt-docs/docs/funcionalidades/relatorios.en.md` | ✅ | ✅ green |
| 32-02-01 | 02 | 2 | ACC-03, ACC-04 | T-32-02 | Internal planning artifacts (REQUIREMENTS.md, ROADMAP.md, .claude/docs/assets.md) match the code-corrected facts | docs | `grep -A1 "ACC-03" .planning/REQUIREMENTS.md | grep -q "Average Prices" && grep -A1 "ACC-03" .planning/REQUIREMENTS.md | grep -q "Assets" && grep -A1 "ACC-04" .planning/REQUIREMENTS.md | grep -q "nine" && grep -A1 "ACC-04" .planning/REQUIREMENTS.md | grep -q "BtcLending" && grep -A1 "lists all four current main tabs" .planning/ROADMAP.md | grep -q "Transactions, Reports, Average Prices, and Assets" && grep -A1 "enumerates all nine current asset types" .planning/ROADMAP.md | grep -q "BtcLending" && grep -q "| 8 | BtcLending | BtcLendingDetails |" .claude/docs/assets.md` | ✅ | ✅ green |
| 32-02-02 | 02 | 2 | QA-02 | T-32-SC | Sibling documentation site builds without errors or broken links | smoke | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict 2>&1 | tee /tmp/mkdocs-build-32.log && ! grep -i "ERROR" /tmp/mkdocs-build-32.log && test -f ../valt-docs/site/index.html` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

None — existing MkDocs tooling and shell-based `grep` checks cover all documentation verification requirements for this phase.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| — | — | — | All phase behaviors are verified through automated file inspection and the MkDocs strict build. |

---

## Validation Audit 2026-07-15

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (none required)
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-07-15

## VALIDATION COMPLETE

**Verdict:** Phase 32 is **NYQUIST-COMPLIANT** for its documentation-only scope.

All five ACC requirements and the QA-02 build gate are covered by automated commands: four `grep`-based factual checks (one per task) and one `mkdocs build --strict` smoke test. No implementation code was modified, so no unit/integration tests are applicable. The 32-VERIFICATION.md report confirms 10/10 must-have truths verified, 0 gaps remaining, and the MkDocs build passes cleanly.
