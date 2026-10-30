---
phase: 54-documentation-site-sync-features-since-july-2026
plan: 02
subsystem: docs
tags: [mkdocs, documentation, transactions, goals, i18n, pt-BR, en-US]

requires:
  - phase: 53-transaction-exclusion-flag
    provides: Exclusion-flag behavior and scope (balances unaffected, reports/goals excluded) documented here
  - phase: 49-goal-contributing-transactions
    provides: Goal contributing-transactions query powering the documented Goal summary modal
  - phase: 50-goal-summary-modal
    provides: Goal summary modal UI (columns, running total, empty state, NetWorthBtc hidden) documented here
  - plan: 54-01
    provides: Bilingual pt→en mirror workflow and parity-gate conventions reused here
provides:
  - Transactions docs page (pt-BR + en-US) documenting the exclusion flag and asterisk rendering
  - Goals docs page (pt-BR + en-US) documenting the View summary modal and exclusion note
affects: [54-06]

actuals:
  tokens: 2723
  tasks: 3
  commits: 4
  plan_head_before: da9dd027bda11d7dda924c3b2a32246cfd465d75
  plan_head_after: 156d53a8a49cafcc856b47cc01ee4554fc9cd81d

tech-stack:
  added: []
  patterns:
    - "Bilingual pt→en mirror commit pairs with header-count parity gate per task (inherited from 54-01)"

key-files:
  created: []
  modified:
    - "valt-docs/docs/funcionalidades/transacoes.md (pt-BR, exclusion-flag section)"
    - "valt-docs/docs/funcionalidades/transacoes.en.md (en-US mirror)"
    - "valt-docs/docs/funcionalidades/metas.md (pt-BR, View summary subsection + exclusion note)"
    - "valt-docs/docs/funcionalidades/metas.en.md (en-US mirror)"

key-decisions:
  - "Corrected the plan's Source-comment path for the goals panel (Transactions/Panels/GoalsPanelView.axaml does not exist; actual path is Transactions/GoalsPanelView.axaml) — code is spec (Rule 1 deviation)"
  - "New transacoes section titled Excluindo Transações de Relatórios e Metas, placed after Excluindo Transações to keep the Edit/Delete adjacency and avoid title collision with the deletion section"
  - "Exclusion checkbox row added only to the Fiat editor field table (the plan's designated table); the dedicated section states the flag applies to any transaction"

patterns-established: []

requirements-completed: [SC-3, SC-5]

coverage:
  - id: D1
    description: "Transactions page (pt + en) documents the exclusion checkbox with verbatim labels, the balances-vs-reports/goals scope distinction, the wealth-transfer use case, and the asterisk suffix on excluded amounts"
    requirement: SC-3
    verification:
      - kind: other
        ref: "presence greps: 'Excluído de relatórios e metas', 'Excluded from reports and goals', literal '123.00 *' in both variants; header parity 31/31"
        status: pass
    human_judgment: false
  - id: D2
    description: "Goals page (pt + en) documents the Ver resumo / View summary flow, Resumo da meta / Goal summary modal columns with reconciling running total, empty state, Patrimônio em BTC unsupported note, and exclusion sentence in Cálculo Automático"
    requirement: SC-3
    verification:
      - kind: other
        ref: "presence greps: 'Ver resumo', 'Resumo da meta', 'Total acumulado', 'Ainda sem transações', 'View summary', 'Goal summary', 'Running total', 'No transactions yet'; header parity 33/33"
        status: pass
    human_judgment: false
  - id: D3
    description: "Strict build green and pt/en structural parity after every commit; both variants updated together"
    requirement: SC-5
    verification:
      - kind: other
        ref: ".venv/bin/mkdocs build --strict (4 runs, all green) + header-count diffs empty for both page pairs"
        status: pass
    human_judgment: false

duration: 4min
completed: 2026-10-30
status: complete
---

# Phase 54 Plan 02: Transactions + Goals Pages Sync (exclusion flag + Goal summary modal) Summary

**Transactions and Goals docs pages brought to current-app state in pt-BR and en-US: the v0.10 transaction exclusion flag (checkbox, balance-vs-reports scope, asterisk rendering) and the v0.9 Goal summary modal (View summary flow, reconciling running total, empty state, Patrimônio em BTC limitation) documented with verbatim resx labels.**

## Performance

- **Duration:** 4 min
- **Started:** 2026-10-30T17:14:21Z
- **Completed:** 2026-10-30T17:18:30Z
- **Tasks:** 3
- **Files modified:** 4 (in the separate `valt-docs` repo)

## Accomplishments

- **Transactions page (pt + en):** added the **Excluído de relatórios e metas** / **Excluded from reports and goals** checkbox row to the editor field table and a new `## Excluindo Transações de Relatórios e Metas` / `## Excluding Transactions from Reports and Goals` section covering: what the flag removes (all Reports panels — Monthly Totals income/expense, Statistics, By categories, Spending Evolution, Fixed vs variable, Sats earned & spent, Stack velocity — and all goal progress), the mandatory scope wording (balances still affected), the wealth-transfer use case, and a `!!! tip` admonition documenting the trailing-asterisk rendering (`$ 123.00 *`) with cross-links to Relatórios and Metas
- **Goals page (pt + en):** added the `### Ver resumo` / `### View summary` subsection under Gerenciando Metas with the right-click flow, the read-only **Resumo da meta** / **Goal summary** modal header strip and `| Coluna | Descrição |` grid table (**Conta**, **Categoria**, **Fiat**, **Sats**, **Total acumulado** / **Account**, **Category**, **Fiat**, **Sats**, **Running total**) stating the final running-total row reconciles exactly with displayed goal progress, an empty-state `!!! note` (**Ainda sem transações** / **No transactions yet**), a `!!! info` that the menu entry is hidden for **Patrimônio em BTC** / **Net Worth in BTC** goals, and the exclusion sentence in `## Cálculo Automático` / `## Automatic Calculation` cross-linking Transações
- All labels copied verbatim from `language.resx` / `language.pt-BR.resx` (verified in-session against the resx values, `TransactionEditorView.axaml:200-206`, `GoalsPanelView.axaml:151`, `GoalSummaryView.axaml` grid columns, `GoalEntryViewModel.CanViewSummary`, `TransactionViewModel.cs:124`)
- Header parity held throughout: transacoes 30/30 → 31/31, metas 32/32 → 33/33; `.venv/bin/mkdocs build --strict` green after every commit
- D-01 honored: only shipped features documented; no savings-rate-panel or other unshipped content added

## Task Commits

Each task was committed atomically in the docs repo (`/home/vmabellini/RiderProjects/valt-docs`):

1. **Task 1: Transactions exclusion-flag section (pt-BR)** — `53a112b` (docs)
2. **Task 1: English mirror** — `bf385f5` (docs)
3. **Task 2: Goals View summary + exclusion note (pt-BR)** — `0580ab7` (docs)
4. **Task 3: English mirror + combined pair gates** — `156d53a` (docs)

**Plan metadata:** committed in the app repo with this SUMMARY (see final commit)

## Files Created/Modified

- `valt-docs/docs/funcionalidades/transacoes.md` — pt-BR Transactions page: checkbox field-table row + new exclusion-flag section (+23 lines)
- `valt-docs/docs/funcionalidades/transacoes.en.md` — en-US mirror with identical structure (+23 lines)
- `valt-docs/docs/funcionalidades/metas.md` — pt-BR Goals page: Ver resumo subsection + Cálculo Automático exclusion sentence (+36 lines)
- `valt-docs/docs/funcionalidades/metas.en.md` — en-US mirror with identical structure (+36 lines)

## Decisions Made

- **[Rule 1 deviation] Source-comment path corrected:** the plan's prescribed comment cited `src/Valt.UI/Views/Main/Tabs/Transactions/Panels/GoalsPanelView.axaml`, which does not exist (the file lives directly under `Transactions/`); all new Source comments use the real path — citing a non-existent path would be misinformation (T-54-04)
- **Section title and placement:** `## Excluindo Transações de Relatórios e Metas` placed after `## Excluindo Transações` (deletion) so the Edit/Delete adjacency is preserved and the two "Excluindo" titles remain clearly distinguishable
- **Field-table row scope:** the checkbox row was added to the Fiat editor table only, exactly as the plan designates; the dedicated section states the flag is available for any transaction, so no per-type table duplication was needed
- **Grid table completeness note:** the column table documents the five plan-designated columns; prose adds that each row also shows the transaction's date and description (verified against `GoalSummaryView.axaml` DataGrid columns)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Corrected non-existent path in the prescribed Source comment**
- **Found during:** Task 2 (Goals page pt-BR)
- **Issue:** Plan task 2 mandates a Source comment citing `src/Valt.UI/Views/Main/Tabs/Transactions/Panels/GoalsPanelView.axaml line 151`; the `Panels/` subdirectory does not exist — the file is `src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelView.axaml` (line 151 confirmed correct: `Goals_ViewSummary` menu item)
- **Fix:** Used the verified real path in the Source comments of both metas.md and metas.en.md
- **Files modified:** `valt-docs/docs/funcionalidades/metas.md`, `valt-docs/docs/funcionalidades/metas.en.md`
- **Verification:** strict build green; path verified via `find src -name GoalsPanelView.axaml`
- **Committed in:** `0580ab7` (pt), `156d53a` (en)

---

**Total deviations:** 1 auto-fixed (Rule 1)
**Impact on plan:** Factual accuracy of source attribution; zero scope change.

## Threat Model Handling

| Threat | Disposition | How handled |
|--------|-------------|-------------|
| T-54-04 (misinformation — exclusion scope) | mitigate | Mandatory scope wording included verbatim in both languages: excluded transactions "continua afetando o saldo das contas" / "keeps affecting account balances" while being removed from all Reports and Goals calculations — verified against 53-01/53-02/53-06 SUMMARYs and the shipped predicates; factual spot-check deferred to 54-06 per plan |
| T-54-05 (pt/en drift) | mitigate | pt→en mirror commit pairs per task; header-count parity diff in Task 1 and combined Task 3 gates (transacoes 31/31, metas 33/33, both diffs empty); site-wide re-check left to 54-06 |

No new threat surface introduced (Markdown-only changes in a public docs repo — no endpoints, auth paths, file access, or schema changes).

## Issues Encountered

None beyond the corrected path above. Every resx label and axaml line reference cited in the plan was spot-verified against the app repo before authoring (ManageTransactions.ExcludedFromReports, Goals_ViewSummary, GoalSummary_* keys, checkbox placement below Notes, CanViewSummary allow-list hiding NetWorthBtc, asterisk formatting at TransactionViewModel.cs:124).

## Known Stubs

None — no placeholders, TODOs, or unwired content. All cross-links (`relatorios.md`, `metas.md`, `transacoes.md`) target existing pages; relatorios.md's exclusion admonition (54-01) already points readers to the transacoes.md section authored here.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plans 54-03/54-04/54-05 can proceed with the same proven bilingual workflow
- Manual follow-ups (tracked in 54-06, not blocking): factual spot-check of the exclusion-scope wording and modal columns against resx/app source; `mkdocs serve` visual check

---
*Phase: 54-documentation-site-sync-features-since-july-2026*
*Completed: 2026-10-30*

## Self-Check: PASSED

- FOUND: `.planning/phases/54-documentation-site-sync-features-since-july-2026/54-02-SUMMARY.md`
- FOUND: docs-repo commits `53a112b`, `bf385f5`, `0580ab7`, `156d53a` (all ancestors of valt-docs HEAD)
