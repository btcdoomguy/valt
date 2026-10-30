---
phase: 54-documentation-site-sync-features-since-july-2026
plan: 04
subsystem: docs
tags: [mkdocs, mkdocs-static-i18n, documentation, btc-loan-simulator, pt-BR, en-US]

requires:
  - phase: 54-documentation-site-sync-features-since-july-2026/54-01
    provides: Reports page rewrite (relatorios.md/.en.md) that this plan cross-links from
provides:
  - New bilingual BTC Loan Simulator docs page (pt-BR + en-US) per locked decision D-02
  - mkdocs.yml 3-point nav registration (nav + pt/en nav_translations) — language toggle works
  - Reports page Próximos Passos / Next Steps cross-link to the simulator (both languages)
affects: [54-05, 54-06]

actuals:
  tokens: 2130
  tasks: 3
  commits: 2
  plan_head_before: 8f837db
  plan_head_after: b59e746

tech-stack:
  added: []
  patterns: [bilingual pt/en page authoring with header-count parity, mkdocs-static-i18n triple nav registration]

key-files:
  created:
    - docs/funcionalidades/simulador-emprestimo-btc.md
    - docs/funcionalidades/simulador-emprestimo-btc.en.md
  modified:
    - mkdocs.yml
    - docs/funcionalidades/relatorios.md
    - docs/funcionalidades/relatorios.en.md

key-decisions:
  - "Used verbatim resx label 'Calculadora de Conversão' / 'Conversion Calculator' for the Tools-menu mention instead of the plan's shorthand 'Conversor' — code/resx is spec"
  - "Placed the simulator nav entry after MCP Server at the end of the Features list (planner's pick per plan)"

patterns-established:
  - "New-page triple registration: nav entry + pt nav_translations (en-title→pt-title) + en nav_translations (pt-title→en-title), verified by count-based greps"

requirements-completed: []  # SC-4/SC-5 are phase-level criteria shared with 54-05/54-06 — not flippable by this plan alone

coverage:
  - id: D1
    description: "BTC Loan Simulator page exists in pt-BR and en-US with verbatim resx labels, live-recalc and sats-hidden admonitions, and Próximos Passos trailer"
    requirement: SC-4
    verification:
      - kind: other
        ref: "cd ../valt-docs && .venv/bin/mkdocs build --strict + content-anchor greps (Task 1/2/3 verify chains)"
        status: pass
    human_judgment: false
  - id: D2
    description: "mkdocs.yml nav triple-registration makes the language toggle work on the new page"
    requirement: SC-5
    verification:
      - kind: other
        ref: "grep -c counts (=1 nav path, =2 title translations) + strict build green (i18n wiring validated)"
        status: pass
    human_judgment: true
    rationale: "54-06 performs the human mkdocs-serve language-toggle visual check per plan success criteria"
  - id: D3
    description: "Reports Próximos Passos / Next Steps links the new simulator page in both languages"
    requirement: SC-4
    verification:
      - kind: other
        ref: "grep -q simulador-emprestimo-btc.md on relatorios.md and relatorios.en.md (Task 2 verify)"
        status: pass
    human_judgment: false

duration: 6min
completed: 2026-10-30
status: complete
---

# Phase 54 Plan 04: BTC Loan Simulator Documentation Page Summary

**Dedicated bilingual BTC Loan Simulator docs page (pt-BR + en-US) with verbatim resx labels, registered in mkdocs.yml at all three i18n points and cross-linked from the Reports page**

## Performance

- **Duration:** 6 min
- **Started:** 2026-10-30T17:33:26Z
- **Completed:** 2026-10-30T17:39:30Z
- **Tasks:** 3
- **Files modified:** 5 (docs repo)

## Accomplishments

- Created `docs/funcionalidades/simulador-emprestimo-btc.md` (pt-BR) and `.en.md` (en-US) — per locked decision D-02, a dedicated page, not a general Tools page — with all 9 verbatim input labels, all 6 verbatim result labels, live-recalculation/default-Compound and sats-hidden admonitions, Tools-menu mention, and the mandatory Próximos Passos trailer
- Registered the page in `mkdocs.yml` at all three required points: `nav:` entry, pt `nav_translations`, en `nav_translations` — count-based greps confirm exactly 1 nav path and exactly 2 title-translation entries
- Added the simulator cross-link to the Reports page Próximos Passos / Next Steps in both languages
- All labels spot-verified against `language.resx` / `language.pt-BR.resx` (`BtcLoanSimulator_*`, `Menu_Tools`, menu-item keys) before authoring — zero invented terminology

## Task Commits

All task commits land in the **docs repo** (`/home/vmabellini/RiderProjects/valt-docs`):

1. **Task 1: Create simulador-emprestimo-btc.md (pt-BR) from the despesas-fixas skeleton** - `5c930ba` (docs)
2. **Task 2: English page + mkdocs.yml 3-point nav registration + Reports cross-link** - `b59e746` (docs)
3. **Task 3: New-page gate — nav, parity, content anchors** - verification-only, no file changes required (all gates green on first run)

**Plan metadata:** committed in the app repo (see final completion notes)

## Files Created/Modified

Docs repo (`/home/vmabellini/RiderProjects/valt-docs`):

- `docs/funcionalidades/simulador-emprestimo-btc.md` — NEW pt-BR page (5 headers: H1 + O Que É / Como Usar / Resultados / Próximos Passos)
- `docs/funcionalidades/simulador-emprestimo-btc.en.md` — NEW en-US mirror (identical header count, parity diff green)
- `mkdocs.yml` — 3 registration points (nav + 2 nav_translations)
- `docs/funcionalidades/relatorios.md` — Próximos Passos cross-link bullet
- `docs/funcionalidades/relatorios.en.md` — Next Steps cross-link bullet

## Decisions Made

- **Verbatim resx label for Tools-menu mention:** the plan's prose shorthand said "Conversor"; the actual resx label is `Calculadora de Conversão` (pt) / `Conversion Calculator` (en) (`Menu_ConversionCalculator`). Used the verbatim resx label per the project rule "code/resx is spec" and RESEARCH Pitfall 3 (never invent terminology).
- **Nav position:** placed `BTC Loan Simulator` after `MCP Server` at the end of the Features nav list — plan explicitly left position to the executor ("adjacent to Assets/MCP Server, planner's pick").

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Corrected plan's Tools-menu label shorthand to verbatim resx label**
- **Found during:** Task 1 (pt page authoring, step 6 Tools-menu sentence)
- **Issue:** Plan step 6 named the menu item "Conversor"; the shipped pt-BR label is `Calculadora de Conversão` (resx `Menu_ConversionCalculator`)
- **Fix:** Wrote the sentence with verbatim labels — pt: Calculadora de Conversão, Simulador de Alavancagem, Histórico de Preços, Evolução de Gastos; en: Conversion Calculator, Leverage Simulator, Price History, Spending Evolution
- **Files modified:** docs/funcionalidades/simulador-emprestimo-btc.md, docs/funcionalidades/simulador-emprestimo-btc.en.md
- **Verification:** Labels grep-verified against `language.resx` / `language.pt-BR.resx` in-session
- **Committed in:** `5c930ba` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 Rule 1)
**Impact on plan:** Accuracy fix only — no scope creep; every UI label on the new page is resx-verbatim.

## Issues Encountered

None — strict build green on first run for all three tasks; Task 3 gate passed without corrections.

## Threat Model Handling

| Threat | Disposition | Outcome |
|--------|-------------|---------|
| T-54-08 (misinformation in simulator page) | mitigate | All input/result labels copied verbatim from the RESEARCH resx-verified table and re-spot-checked against `language.resx`/`language.pt-BR.resx` this session; prefill-from-existing-loan feature (removed pre-ship, quick 260821-k09) never documented — Task 3 guard grep confirms zero prefill descriptions |
| T-54-09 (mkdocs.yml misregistration breaks i18n toggle) | mitigate | All three registration points landed in one commit (`b59e746`); count-based greps green (=1 nav path, =2 title translations); `mkdocs build --strict` validates the i18n wiring; 54-06 re-checks |

## Known Stubs

None — the page is fully authored content; no placeholders, no TODOs.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- 54-05 (MCP Server tool-catalog sync) is unblocked — independent files
- 54-06 QA gate inherits a green strict build with the simulator page registered; manual follow-ups remain: factual spot-check of inputs/results against resx `BtcLoanSimulator_*` keys and `mkdocs serve` language-toggle check on the new page

## Self-Check: PASSED

- FOUND: `/home/vmabellini/RiderProjects/valt-docs/docs/funcionalidades/simulador-emprestimo-btc.md`
- FOUND: `/home/vmabellini/RiderProjects/valt-docs/docs/funcionalidades/simulador-emprestimo-btc.en.md`
- FOUND commit (docs repo): `5c930ba` — `git merge-base --is-ancestor 5c930ba HEAD` OK
- FOUND commit (docs repo): `b59e746` — `git merge-base --is-ancestor b59e746 HEAD` OK
- Strict build green with page in nav; parity diff clean; prefill guard grep zero-hit

---
*Phase: 54-documentation-site-sync-features-since-july-2026*
*Completed: 2026-10-30*
