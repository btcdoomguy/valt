---
phase: 50-goal-summary-modal-ui
plan: 01
subsystem: ui
tags: [goals, modal, datagrid, localization, context-menu]
requires:
  - Phase 49 GetGoalContributingTransactionsQuery / GoalContributingTransactionsResult / ContributingTransactionRow contract
provides:
  - GoalSummary chromeless modal (View + ViewModel + code-behind)
  - GoalsPanelViewModel.ViewSummaryCommand end-to-end path
  - GoalEntryViewModel.CanViewSummary / SummaryStrategyUnit gating
  - ApplicationModalNames.GoalSummary = 43 + DI registration
  - 12 neutral (en) localization keys (pt-BR/es deferred to Phase 51 GOL-09)
affects:
  - src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelView.axaml (context menu)
  - src/Valt.UI/Extensions.cs (composition root)
tech-stack:
  added: []
  patterns:
    - Chromeless modal (SystemDecorations None + CustomTitleBar) copied from LoanStateHistory
    - Pre-fetched Result projection (query awaited before IModalFactory.CreateAsync — no async load in modal)
    - RunningTotal-delta sign derivation with lazy TryGetResource semantic brushes
key-files:
  created:
    - src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml
    - src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml.cs
    - src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryViewModel.cs
  modified:
    - src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs
    - src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelViewModel.cs
    - src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelView.axaml
    - src/Valt.UI/Views/ApplicationModalNames.cs
    - src/Valt.UI/Extensions.cs
    - src/Valt.UI/Lang/language.resx
    - src/Valt.UI/Lang/language.Designer.cs
decisions:
  - "Single parameterless GoalSummaryViewModel constructor serves both DI and design mode (VM needs no injected services — the query is dispatched in the panel VM and the result arrives in Request); deviates from PATTERNS dual-ctor sketch but matches the plan's 'one public parameterless constructor' instruction"
  - "GoalSummaryStrategyUnit enum referenced as GoalSummaryViewModel.GoalSummaryStrategyUnit from GoalEntryViewModel (nested type needs qualification)"
  - "Empty-state panel wrapped in a Border (Padding 16) because Avalonia StackPanel has no Padding property (AVLN2000 build error)"
metrics:
  duration: 25min
  completed: 2026-10-06
  tasks: 3
  commits: 2
actuals:
  tokens: 7800
  tasks: 3
  commits: 2
status: complete
---

# Phase 50 Plan 01: Goal Summary Modal — "View summary" Path Summary

End-to-end "View summary" path: right-click any transaction-based goal entry in
the Goals section → dispatch `GetGoalContributingTransactionsQuery` → open the
new chromeless Goal Summary modal with a read-only 7-column grid (date,
description, account, category, fiat, sats, running total), per-unit running
total formatting, RunningTotal-delta sign semantics, and localized empty state.

## Tasks Completed

| # | Task | Type | Commit | Result |
|---|------|------|--------|--------|
| 1 | End-to-end "View summary" — context menu to populated modal | tracer | 661ff6d | New modal trio, gating, wiring, enum/DI, 12 en resx keys; build + 15 existing GoalsPanelViewModelTests green |
| 2 | Projection semantics — per-unit totals, sign derivation, empty cells | auto | c13e864 | Design-time rows resolve semantic brushes for preview; all sign/format/empty-cell grep gates pass |
| 3 | Grid fidelity pass — templates, fonts, empty-state verification | auto | (no changes) | Audit-only: zero drift vs UI-SPEC; all 7 acceptance grep gates pass |

## Verification

- `dotnet build Valt.sln` — 0 errors.
- `dotnet test --filter "FullyQualifiedName~GoalsPanelViewModelTests"` — 15/15 passed.
- Menu item: exactly 1 `ViewSummaryCommand` binding in `GoalsPanelView.axaml`, carrying `IsVisible="{Binding CanViewSummary}"` and the inverse secure-mode `IsEnabled` binding.
- `GoalSummary = 43` in `ApplicationModalNames.cs`; `AddTransient<GoalSummaryViewModel>()` and `ApplicationModalNames.GoalSummary =>` factory case in `Extensions.cs`.
- resx/Designer: 11 `GoalSummary_*` keys + 1 `Goals_ViewSummary` key, all mirrored as static properties.
- XAML: 3 `ContributionForeground` bindings, 4 `CharacterEllipsis`, 6 `DataGridTemplateColumn` (12 element/close tag pairs = 24 grep hits), 3 `GeistMono` amount columns, no window-level `MaxWidth/MaxHeight`, `MinWidth="720" MinHeight="480"`, root content `Margin="12"`.

## Truths Coverage (GOL-03 / GOL-04)

- NetWorthBtc entries: `CanViewSummary` false → item hidden (not disabled), no explanation modal.
- Secure mode: item disabled via XAML binding, same as Edit.
- Query failure: error box via `MessageBoxHelper.ShowErrorAsync(language.Error, ...)`; modal never opens.
- Modal opens fully populated — query awaited before `IModalFactory.CreateAsync`; no loading chrome.
- Empty contributing set: localized empty state replaces the grid (`HasRows` toggle).
- Row order: query-returned order rendered verbatim, no UI re-sorting.
- Final total: last row's `RunningTotal` via `FormatRunningTotal`, never recomputed.
- Empty counterpart cells: `string.Empty` for zero fiat/sats — no fabricated zeros.
- Sign: derived only from RunningTotal deltas (first row = own sign); drives `ContributionForeground` on Fiat/Sats/Running-total cells; zero deltas render default foreground.
- Per-unit running-total format: fiat 2dp+currency / grouped sats / integer count / 1dp percentage, all four branches present.

## Deviations from Plan

None requiring rule invocation beyond the documented adjustments:

1. **Plan-action delta (not a rule violation):** Task 1 acceptance criteria stated `grep -c 'name="GoalSummary_'` returns 10, but the task action lists 12 keys total — 11 with the `GoalSummary_` prefix plus `Goals_ViewSummary`. Implemented per the action (authoritative copy contract); grep returns 11, not 10. All 12 keys exist and are used (or reserved for plan 02: `GoalSummary_PeriodLabel`, `GoalSummary_TotalLabel`).
2. **Avalonia constraint fix (Rule 1 — bug):** Empty-state `StackPanel Padding="16"` failed the build (AVLN2000 — StackPanel has no Padding). Wrapped in a `Border Padding="16"` preserving the UI-SPEC spacing token.
3. **Type qualification (Rule 3 — blocking):** `GoalSummaryStrategyUnit` is nested in `GoalSummaryViewModel`; `GoalEntryViewModel` references it fully qualified.
4. **Task 3 was audit-only:** the Task 1 XAML already matched the UI-SPEC typography/color/spacing contracts, so no edits were needed and no commit was created for it.

## Known Stubs

None. The header strip is intentionally minimal (goal name + period) per the
plan; the full label/total layout is plan 02 scope. `GoalSummary_PeriodLabel`
and `GoalSummary_TotalLabel` keys are added now and consumed by plan 02.

## Auth Gates

None.

## Self-Check: PASSED

- FOUND: src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml
- FOUND: src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml.cs
- FOUND: src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryViewModel.cs
- FOUND: commit 661ff6d (tracer) and c13e864 (projection hardening) in `git log`
- Build 0 errors; 15/15 GoalsPanelViewModelTests pass
