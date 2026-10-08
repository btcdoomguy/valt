---
phase: 40-btc-denominated-metrics-reports-ui
plan: 01
subsystem: ui
tags: [avalonia, cqrs, litedb, btc-denominated, reports, livecharts]

requires:
  - phase: 39-spending-analytics-reports-ui
    provides: Reports tab architecture, IReportDataProvider, FilterRange, centralized analytics category filter, SavingsRate/FixedVsVariable chart patterns.

provides:
  - BtcDenominatedMetrics App-layer query module (contract, query, handler, DTOs).
  - Infra-layer BTC-denominated metrics aggregation backed by IReportDataProvider and exact-date price lookup.
  - Two disposable chart-data classes (BtcDenominatedMetricsChartData, StackVelocityChartData).
  - ReportsViewModel wiring for loading, empty, and error states.
  - Two new XAML Expander sections in ReportsView.axaml (Sats earned & spent, Stack velocity).
  - English localization keys for titles, series labels, empty states, and error copy.
  - Passing tracer test proving BRL income/expense → sats conversion and velocity computation.

affects:
  - 40-btc-denominated-metrics-reports-ui (Plan 40-02 will extend the same DTO and UI)
  - 43-mcp-localization-documentation-and-verification (needs pt-BR/es strings, MCP tool exposure, docs)

actuals:
  tokens: 20000
  tasks: 3
  commits: 3

tech-stack:
  added: []
  patterns:
    - CQRS query module mirroring SpendingEvolution/SavingsRate modules
    - Disposable chart-data classes with LiveChartsCore StackedColumnSeries and LineSeries
    - ReportsViewModel async fetch pattern with loading/empty/error observables

key-files:
  created:
    - src/Valt.App/Modules/BtcDenominatedMetrics/Contracts/IBtcDenominatedMetricsQueries.cs
    - src/Valt.App/Modules/BtcDenominatedMetrics/Queries/GetBtcDenominatedMetricsQuery.cs
    - src/Valt.App/Modules/BtcDenominatedMetrics/Queries/GetBtcDenominatedMetricsHandler.cs
    - src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/BtcDenominatedMetricsDataDto.cs
    - src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/BtcDenominatedMetricsMonthDto.cs
    - src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/SatsSpentByCategoryDto.cs
    - src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/BtcDenominatedMetricsChartData.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/StackVelocityChartData.cs
    - tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs
  modified:
    - src/Valt.Infra/Extensions.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml
    - src/Valt.UI/Lang/language.resx
    - src/Valt.UI/Lang/language.Designer.cs

key-decisions:
  - "Mirrored the SpendingEvolution/SavingsRate backend structure for the new BtcDenominatedMetrics module to keep the App/Infra split consistent."
  - "Added the Earned/Spent/Velocity series-name language keys in Task 2 so the chart-data classes could compile before the full localization pass in Task 3."
  - "Added error-state observables (IsBtcMetricsError, IsStackVelocityError) in Task 3 rather than Task 2 to keep the ViewModel wiring task focused on data flow."
  - "Kept SpentByCategory empty in the query DTO; the per-category breakdown is intentionally deferred to Plan 40-02 to avoid overloading this tracer slice."

patterns-established:
  - "New reports module: contract interface in Valt.App, handler pass-through, implementation in Valt.Infra, registration in Extensions.AddQueries()."
  - "Reports chart data: disposable class owns Series/XAxes/YAxes, recreates series on each RefreshChart to avoid LiveCharts memory leaks."
  - "ReportsViewModel fetch pattern: DispatchAsync query, update chart data on UI thread, set loading/empty/error flags in both success and exception paths."

requirements-completed:
  - BTC-01
  - BTC-02
  - BTC-03

coverage:
  - id: D1
    description: "Backend BtcDenominatedMetrics query returns monthly sats earned, sats spent, and stack velocity from fiat/BTC transactions."
    requirement: BTC-01
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs#BtcDenominatedMetricsQueriesTests"
        status: pass
    human_judgment: false
  - id: D2
    description: "ReportsViewModel exposes disposable chart-data classes and fetches the BTC metrics query in parallel with the other report sections."
    requirement: BTC-03
    verification:
      - kind: other
        ref: "dotnet build Valt.sln (ReportsViewModel, BtcDenominatedMetricsChartData, StackVelocityChartData compile)"
        status: pass
    human_judgment: false
  - id: D3
    description: "ReportsView.axaml contains two new expanders (Sats earned & spent, Stack velocity) with correct localization keys, bindings, and loading/empty/error states."
    requirement: BTC-03
    verification:
      - kind: other
        ref: "dotnet build Valt.sln and grep-verification of language keys in src/Valt.UI/Lang/language.resx"
        status: pass
    human_judgment: false

duration: 15min
completed: 2026-08-06
status: complete
---

# Phase 40 Plan 01: BTC-Denominated Metrics Reports & UI (Tracer Slice) Summary

**Backend CQRS query + Reports tab UI sections for sats earned, sats spent, and stack velocity, with a passing end-to-end tracer test.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-08-06T14:04:00Z
- **Completed:** 2026-08-06T14:19:00Z
- **Tasks:** 3
- **Files modified:** 13

## Accomplishments

- Created the BtcDenominatedMetrics App-layer module (contract, query, handler, DTOs) following the SpendingEvolution pattern.
- Implemented the Infra query that converts fiat income/expense at exact transaction-date BTC rates, excludes internal transfers and BTC buy/sell from earned/spent, and computes stack velocity per month.
- Added two disposable chart-data classes for the monthly grouped-bar chart and the velocity line chart.
- Wired `ReportsViewModel` with loading, empty, and error observables for the two new panels.
- Inserted the two new XAML expanders in the correct order between Fixed vs variable and Categories.
- Added English-only localization keys (pt-BR/es deferred to Phase 43).
- Wrote and greened a tracer test proving a BRL income/expense pair is converted to sats and velocity is computed correctly.

## Task Commits

Each task was committed atomically:

1. **Task 1: BTC-denominated metrics backend + tracer test** - `9ed87fe` (feat)
2. **Task 2: Chart-data classes and ReportsViewModel wiring** - `dfd2040` (feat)
3. **Task 3: Reports XAML expanders and English localization** - `d4f499b` (feat)

## Files Created/Modified

- `src/Valt.App/Modules/BtcDenominatedMetrics/Contracts/IBtcDenominatedMetricsQueries.cs` - Query contract.
- `src/Valt.App/Modules/BtcDenominatedMetrics/Queries/GetBtcDenominatedMetricsQuery.cs` - Query record with filter ranges.
- `src/Valt.App/Modules/BtcDenominatedMetrics/Queries/GetBtcDenominatedMetricsHandler.cs` - Thin handler forwarding to the infra query.
- `src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/BtcDenominatedMetricsDataDto.cs` - Aggregate DTO.
- `src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/BtcDenominatedMetricsMonthDto.cs` - Per-month metrics DTO.
- `src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/SatsSpentByCategoryDto.cs` - Per-category breakdown DTO.
- `src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs` - Infra aggregation and rate conversion.
- `src/Valt.Infra/Extensions.cs` - Registered `IBtcDenominatedMetricsQueries` in `AddQueries()`.
- `src/Valt.UI/Views/Main/Tabs/Reports/BtcDenominatedMetricsChartData.cs` - Monthly grouped-bar chart data.
- `src/Valt.UI/Views/Main/Tabs/Reports/StackVelocityChartData.cs` - Monthly line chart data.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` - New observables, fetch methods, and dispose calls.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` - New expanders and bindings.
- `src/Valt.UI/Lang/language.resx` - English strings.
- `src/Valt.UI/Lang/language.Designer.cs` - Generated static accessors.
- `tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs` - Tracer test.

## Decisions Made

- Mirrored the SpendingEvolution/SavingsRate module layout to keep the App/Infra split and DI registration consistent.
- Added the Earned/Spent/Velocity series-name keys in Task 2 so the chart-data classes could compile before the full localization pass in Task 3.
- Added error-state observables (`IsBtcMetricsError`, `IsStackVelocityError`) in Task 3 rather than Task 2 to keep the ViewModel wiring task focused on data flow.
- Left `SpentByCategory` empty in the query DTO; the per-category breakdown is intentionally deferred to Plan 40-02 to avoid overloading this tracer slice.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## Known Stubs

| File | Line | Description |
|------|------|-------------|
| `src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs` | ~130 | `SpentByCategory` is returned as an empty list; the per-category breakdown is planned for Plan 40-02. |
| `src/Valt.UI/Lang/language.resx` | `Reports_BtcMetrics_CategoryLabel` | The "By category" label is added but not yet consumed in the UI; the category toggle/view is deferred to Plan 40-02. |

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 40-02 can extend the same `BtcDenominatedMetricsDataDto` and `BtcDenominatedMetricsQueries` to populate `SpentByCategory` and add the category-breakdown UI toggle.
- Phase 43 will add pt-BR/es translations, expose the query through MCP report tools, and update `.claude/docs/reports.md`.

---
*Phase: 40-btc-denominated-metrics-reports-ui*
*Completed: 2026-08-06*

## Self-Check: PASSED

- `40-01-SUMMARY.md` exists on disk.
- Task commits `9ed87fe`, `dfd2040`, and `d4f499b` exist in git history.
- Final plan-metadata commit for `.planning/` files was intentionally skipped by `gsd-tools` because `.planning/phases/.../40-01-SUMMARY.md` matches `.gitignore`. STATE.md, ROADMAP.md, and REQUIREMENTS.md updates remain in the working tree.
