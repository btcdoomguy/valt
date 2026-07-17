---
phase: 38-navigation-new-pages-and-quality-assurance
plan: 02
type: execute
subsystem: docs
tags: [mcp, documentation, pt-BR, en-US, drift-fix, mkdocs]

# Dependency graph
requires:
  - phase: 37-mcp-server-page-update
    provides: AssetTools/IndicatorTools sections and Source-comment convention on MCP page
provides:
  - 7 corrected MCP category tables in both pt-BR and en-US
  - Code-verified tool-name list (61 real names across 8 pre-existing categories)
  - Drift regression gate scripts and green build evidence
affects:
  - 38-03-QA-CHECKLIST
  - valt-docs/docs/funcionalidades/mcp-server.md
  - valt-docs/docs/funcionalidades/mcp-server.en.md

# Tech tracking
tech-stack:
  added: []
  patterns:
    - verbatim-from-code MCP tool names and descriptions
    - per-category <!-- Source: --> evidence comments
    - bilingual pt-BR/en-US mirror discipline

key-files:
  created: []
  modified:
    - ../valt-docs/docs/funcionalidades/mcp-server.md
    - ../valt-docs/docs/funcionalidades/mcp-server.en.md

key-decisions:
  - "Replaced all 7 drifted category tables as whole units rather than minimal renames, following RESEARCH Example 1 verbatim for PT and tracking code [Description] attributes for EN."
  - "Committed both language files in a single docs(38-02) commit per QA-01 bilingual-mirror requirement."
  - "Left the 'mais de 80 ferramentas' / '80+ tools' intro untouched because post-fix documented count (90) still satisfies the claim."
  - "Preserved the Categorias/Categories section byte-identical because it was the only clean pre-existing category."

patterns-established:
  - "Drift regression gate: 28 phantom names × 2 files must be absent, 61 real names × 2 files must be present, parity equal, strict build green."

requirements-completed: [QA-01, QA-02]

# Metrics
duration: 18 min
completed: 2026-07-17
status: complete
---

# Phase 38 Plan 02: MCP Tool-Name Drift Fix Summary

**Replaced 7 drifted MCP tool category tables in both pt-BR and en-US docs with 61 code-verified tool names, eliminating 28 phantom names and restoring the documented count to 90 tools.**

## Performance

- **Duration:** 18 min
- **Started:** 2026-07-17T14:09:56Z
- **Completed:** 2026-07-17T14:27:56Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments

- Replaced drifted tables in the Portuguese `mcp-server.md` for Contas, Transações, Despesas Fixas, Metas, Preço Médio, Relatórios, and Moedas.
- Mirrored the corrected tables in the English `mcp-server.en.md` with headings and descriptions translated while keeping tool names verbatim.
- Added per-category `<!-- Source: src/Valt.Infra/Mcp/Tools/...cs -->` evidence comments to all 7 rewritten sections (9 total per file, including the 2 phase-37 comments for Ativos and Indicadores).
- Verified that all 28 phantom tool names are absent from both files and all 61 real names across the 8 pre-existing categories are present in both files.
- Confirmed bilingual heading parity (`grep -c '^#'`) and table-row parity (`grep -c '^|'`).
- Ran `mkdocs build --strict` successfully (exit 0, INFO-only output).

## Task Commits

1. **Task 1: Replace drifted tables in Portuguese page** + **Task 2: Mirror corrected tables to English page** — `d760fd5` `docs(38-02): replace drifted MCP tool tables with code-verified names in 7 categories (pt-BR + en-US)`

Task 3 (full drift regression gate + strict build) did not introduce new file changes; it verified the previous commit.

## Files Created/Modified

- `../valt-docs/docs/funcionalidades/mcp-server.md` — Replaced 7 drifted category tool tables (Contas, Transações, Despesas Fixas, Metas, Preço Médio, Relatórios, Moedas) with 61 code-verified names and PT descriptions; added 7 Source evidence comments.
- `../valt-docs/docs/funcionalidades/mcp-server.en.md` — Mirrored the 7 corrected tables with English headings and descriptions; added 7 Source evidence comments.

## Decisions Made

- **Full table replacement vs. minimal renames:** Replaced each category table entirely from RESEARCH Example 1 to remove stale descriptions and add the 32 previously missing real tools, landing the documented count at 90 = code truth.
- **Single bilingual commit:** Committed both `mcp-server.md` and `mcp-server.en.md` together under one `docs(38-02):` message per QA-01 mirror requirement.
- **No intro count edit:** Left the existing "mais de 80 ferramentas" / "80+ tools" intro text untouched because the post-fix count (90) still satisfies the claim.
- **Preserve clean Categorias/Categories section:** Left the only drift-free category untouched to avoid unnecessary churn.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. The build environment in `valt-docs/.venv` produced a green `mkdocs build --strict` on the first run; all presence, phantom, and parity greps returned empty on the first run.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 38-02 is complete. The corrected MCP tool tables can be cited as evidence in the wave-2 QA checklist (38-03).
- No blockers. The valt-docs tree is clean and the site builds strictly.

## Self-Check: PASSED

- [x] Modified files exist on disk: `../valt-docs/docs/funcionalidades/mcp-server.md`, `../valt-docs/docs/funcionalidades/mcp-server.en.md`
- [x] Commit `d760fd5` exists in `valt-docs` and contains both files
- [x] 28 phantom tool names absent from both files (phantom loop returned empty)
- [x] 61 real tool names present in both files (MISSING loop returned empty)
- [x] PT/EN heading parity equal (`grep -c '^#'` matched)
- [x] PT/EN table-row parity equal (`grep -c '^|'` matched)
- [x] `mkdocs build --strict` exited 0 with INFO-only output
- [x] `git -C /home/vmabellini/RiderProjects/valt-docs status --short` shows clean tree

---
*Phase: 38-navigation-new-pages-and-quality-assurance*
*Completed: 2026-07-17*
