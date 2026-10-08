---
phase: 39-spending-analytics-reports-ui
plan: 03
subsystem: ui

tags: [avalonia, mvvm, reports, burn-rate, dashboard, localization, nunit]

requires:
  - phase: 39-01
    provides: GetBurnRateQuery, BurnRateDataDto, and SpendingAnalytics DI registrations

provides:
  - RowItem.RightTextForeground optional brush parameter for green/red dashboard rows
  - DashboardDataUserControl binding for RightTextForeground with Text100Brush fallback
  - BurnRatePanelViewModel (DashboardPanelViewModel subclass) dispatching GetBurnRateQuery
  - ReportsViewModel observables/handler/wiring for BurnRateData/IsBurnRateLoading/IsBurnRateVisible
  - Burn rate dashboard card in ReportsView.axaml DashboardGridPanel after Statistics
  - English resx strings for the burn rate card (D-21)

affects:
  - 39-04 (savings-rate chart sections may reuse the same RowItem foreground pattern)
  - 43 (MCP/localization/documentation will reference the new burn-rate panel and strings)

tech-stack:
  added: []
  patterns:
    - "Optional positional record parameter to extend dashboard row styling without breaking call sites"
    - "DashboardPanelViewModel subclass mirroring BtcLoansPanelViewModel refresh/loading/visibility pattern"
    - "ReportsViewModel panel wiring via PropertyChanged mirroring and shared RefreshAsync triggers"

key-files:
  created:
    - src/Valt.UI/Views/Main/Tabs/Reports/Panels/BurnRatePanelViewModel.cs
  modified:
    - src/Valt.UI/Views/Main/Tabs/Reports/RowItem.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/DashboardDataUserControl.axaml
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.DesignTime.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml
    - src/Valt.UI/Extensions.cs
    - src/Valt.UI/Lang/language.resx
    - src/Valt.UI/Lang/language.Designer.cs
    - tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs

key-decisions:
  - "Followed the existing BtcLoans/Leverage panel wiring pattern exactly for ReportsViewModel observables, RefreshAsync triggers, and disposal (Q1 from 39-CONTEXT)."
  - "Placed the burn rate card after the Statistics card in DashboardGridPanel per UI-SPEC default ordering."
  - "Used English-only resx strings for this plan (D-21); pt-BR/es files left untouched for Phase 43 localization pass."
  - "Used TransactionGridResources.Credit for projection <= median and TransactionGridResources.Debt for projection > median, matching the MonthlyReportItemViewModel green/red convention."
  - "Kept the burn-rate card always visible (IsVisible = true) even on empty/error states, unlike the conditional BtcLoans/Leverage panels."

patterns-established:
  - "RowItem accepts an optional IBrush? RightTextForeground positional parameter so existing call sites compile unchanged."
  - "DashboardDataUserControl binds RightTextBlock.Foreground to RightTextForeground with TargetNullValue/FallbackValue to Text100Brush, preserving the .stale style selector precedence."
  - "New dashboard panels are registered as singletons in Extensions.cs and injected into ReportsViewModel, then mirrored into view-bound observables."

requirements-completed: [SPA-02]

# Metrics
duration: 10min
completed: 2026-08-04
status: complete
---

# Phase 39 Plan 03: Burn Rate Dashboard Card Summary

**Burn-rate dashboard card in the Reports tab with day-aware projection, 12-month median comparison, green/red vs-median coloring, and shared refresh/loading/visibility plumbing.**

## Performance

- **Duration:** 10 min
- **Started:** 2026-08-04T22:56:11Z
- **Completed:** 2026-08-04T23:05:25Z
- **Tasks:** 3
- **Files modified:** 9

## Accomplishments

- Extended `RowItem` with an optional `IBrush? RightTextForeground` and updated `DashboardDataUserControl.axaml` to bind it while preserving the existing `Text100Brush` default and `.stale` style behavior.
- Created `BurnRatePanelViewModel` dispatching `GetBurnRateQuery`, formatting rows for spend-so-far, avg daily, projected month-end, median month, and vs-median with Credit/Debt coloring.
- Registered the panel in `Extensions.cs` and added nine English resx strings per UI-SPEC copywriting.
- Wired `BurnRateData`/`IsBurnRateLoading`/`IsBurnRateVisible` into `ReportsViewModel` and added the card after Statistics in `ReportsView.axaml`.
- Updated `ReportsViewModelTests` to cover the new panel wiring and visibility propagation.

## Task Commits

Each task was committed atomically:

1. **Task 1: RowItem.RightTextForeground + DashboardDataUserControl binding** - `659493b` (feat)
2. **Task 2: BurnRatePanelViewModel + DI registration + English resx keys** - `f4b52a5` (feat)
3. **Task 3: ReportsViewModel wiring + ReportsView.axaml dashboard card + tests** - `df4e951` (feat)

**Plan metadata:** SDK returned `skipped_gitignored` for the final metadata commit — `.planning/` is gitignored, so SUMMARY.md/STATE.md/ROADMAP.md/REQUIREMENTS.md updates remain uncommitted per project configuration.

## Files Created/Modified

- `src/Valt.UI/Views/Main/Tabs/Reports/Panels/BurnRatePanelViewModel.cs` - New dashboard panel VM dispatching `GetBurnRateQuery` and building D-12..D-15 row variants.
- `src/Valt.UI/Views/Main/Tabs/Reports/RowItem.cs` - Added optional `RightTextForeground` positional parameter.
- `src/Valt.UI/Views/Main/Tabs/Reports/DashboardDataUserControl.axaml` - Bound `RightTextBlock.Foreground` to `RightTextForeground` with `Text100Brush` fallback.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` - Added burn-rate panel field, observables, PropertyChanged handler, refresh triggers, and disposal.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.DesignTime.cs` - Added `_burnRatePanel = null!` for design-time compile.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` - Added Burn rate card after Statistics in `DashboardGridPanel`.
- `src/Valt.UI/Extensions.cs` - Registered `services.AddSingleton<BurnRatePanelViewModel>()`.
- `src/Valt.UI/Lang/language.resx` - Added English burn-rate strings.
- `src/Valt.UI/Lang/language.Designer.cs` - Generated static properties for the new resx keys.
- `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` - Updated constructor call sites and visibility assertions.

## Decisions Made

- Followed the existing BtcLoans/Leverage panel wiring pattern exactly for `ReportsViewModel` observables, `RefreshAsync` triggers, and disposal (Q1 from 39-CONTEXT).
- Placed the burn rate card after the Statistics card in `DashboardGridPanel` per UI-SPEC default ordering.
- Used English-only resx strings for this plan (D-21); `language.pt-BR.resx` and `language.es.resx` left untouched for Phase 43 localization pass.
- Used `TransactionGridResources.Credit` when projected month-end spend <= median and `TransactionGridResources.Debt` when above, matching the `MonthlyReportItemViewModel` green/red convention.
- Kept the burn-rate card always visible (`IsVisible = true`) even on empty/error states, unlike the conditional BtcLoans/Leverage panels.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- `ReportsViewModelTests` failed to compile after Task 3 constructor change because the test file still used the old `ReportsViewModel` signature without `BurnRatePanelViewModel`. Fixed by updating both test methods to create and pass a real `BurnRatePanelViewModel`, and added burn-rate visibility assertions. This is part of the planned wiring task and was committed with Task 3.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- SPA-02 is complete; the burn-rate backend from 39-01 is now consumed in the UI.
- The `RowItem.RightTextForeground` pattern is available for reuse by 39-04 and later chart/panel UI plans.
- `dotnet build Valt.sln` is green and the targeted UI tests pass.

## Self-Check: PASSED

- All created/modified files exist on disk.
- Task commits `659493b`, `f4b52a5`, and `df4e951` exist in git history.
- `dotnet build Valt.sln` succeeded with 0 errors.
- Targeted UI tests passed: 14/14 (`ReportsViewModelTests`, `ReportsViewModelSnapshotTests`, `LeveragePositionsPanelViewModelTests`, `BtcLoansPanelViewModelTests`).
- STATE.md, ROADMAP.md, and REQUIREMENTS.md updated on disk (metadata commit intentionally skipped because `.planning/` is gitignored).
- No new stub patterns or security-relevant surface beyond the planned UI card.

---
*Phase: 39-spending-analytics-reports-ui*
*Completed: 2026-08-04*
