---
phase: 42-loans-leverage-reports-ui
plan: 01
subsystem: ui
tags: [avalonia, livecharts, cqrs, loan-reports]

requires: []
provides:
  - App-layer LoanReports CQRS module (query, handler, contract, DTOs)
  - Infra LoanReportsQueries implementation
  - LoanCostChartData and LiquidationDistanceChartData UI wrappers
  - ReportsViewModel wiring and new Expander section
  - English localization keys for the new section
affects:
  - 42-02
  - 42-03

actuals:
  tokens: 22000
  tasks: 3
  commits: 3

tech-stack:
  added: []
  patterns:
    - "CQRS query/handler/contract/DTO module in Valt.App + Infra implementation"
    - "LiveCharts chart-data class with Series/XAxes/YAxes and RefreshChart"
    - "Reports tab panel with IsLoading/IsVisible/IsEmpty/IsError flags"

key-files:
  created:
    - src/Valt.App/Modules/LoanReports/Queries/GetLoanReportsQuery.cs
    - src/Valt.App/Modules/LoanReports/Queries/GetLoanReportsHandler.cs
    - src/Valt.App/Modules/LoanReports/Contracts/ILoanReportsQueries.cs
    - src/Valt.App/Modules/LoanReports/DTOs/LoanReportsDataDto.cs
    - src/Valt.App/Modules/LoanReports/DTOs/LoanCostMonthDto.cs
    - src/Valt.App/Modules/LoanReports/DTOs/LiquidationDistanceMonthDto.cs
    - src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/LoanCostChartData.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/LiquidationDistanceChartData.cs
  modified:
    - src/Valt.Infra/Extensions.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml
    - src/Valt.UI/Lang/language.resx
    - src/Valt.UI/Lang/language.Designer.cs

key-decisions:
  - "Distance line color is selected once per refresh from the minimum distance in the dataset using the same risk-band thresholds as DashboardDataBrushes.ForLtv, per UI-SPEC color rule."
  - "Fees are assigned to the month containing each snapshot's EffectiveDate, but only when the loan is active at the corresponding month-end."

requirements-completed:
  - LON-01
  - LON-02

coverage:
  - id: D1
    description: "App-layer LoanReports CQRS module with query, handler, contract, and DTOs"
    requirement: LON-01
    verification:
      - kind: build
        ref: "dotnet build Valt.sln"
        status: pass
    human_judgment: false
  - id: D2
    description: "Infra LoanReportsQueries computes monthly interest+fees cost and worst-case liquidation distance from loan snapshots"
    requirement: LON-01
    verification:
      - kind: build
        ref: "dotnet build Valt.sln"
        status: pass
    human_judgment: false
  - id: D3
    description: "Reports tab shows Loans & Leverage Reports Expander with cost and distance charts when active BTC-backed loans exist"
    requirement: LON-02
    verification:
      - kind: build
        ref: "dotnet build Valt.sln"
        status: pass
    human_judgment: true
    rationale: "Visual layout and chart rendering require human smoke test; automated UI tests are out of scope for this phase."

duration: 45min
completed: 2026-08-11
status: complete
---

# Phase 42 Plan 01: Loans & Leverage Reports tracer Summary

**End-to-end Loans & Leverage Reports section wired from CQRS query through ReportsViewModel to a new XAML Expander with stacked-bar cost and line distance charts.**

## Performance

- **Duration:** 45 min
- **Started:** 2026-08-11T21:00:00Z
- **Completed:** 2026-08-11T21:45:00Z
- **Tasks:** 3
- **Files modified:** 14

## Accomplishments
- Created `Valt.App.Modules.LoanReports` with `GetLoanReportsQuery`, handler, contract, and DTOs.
- Implemented `LoanReportsQueries` in `Valt.Infra` to project snapshot-based monthly cost and liquidation distance.
- Registered `ILoanReportsQueries` in the DI container.
- Added `LoanCostChartData` and `LiquidationDistanceChartData` LiveCharts wrappers.
- Wired `FetchLoanReportsAsync` into `ReportsViewModel` and the Reports tab date-range refresh pipeline.
- Added the "Loans & Leverage Reports" Expander to `ReportsView.axaml` after Stack velocity.
- Added English-only localization keys and generated `language.Designer.cs` properties.

## Task Commits

1. **Task 1: Create LoanReports App-layer contracts and DTOs** - `719eb38` (feat)
2. **Task 2: Implement LoanReportsQueries, register DI, and create chart data classes** - `ba8cb26` (feat)
3. **Task 3: Wire ViewModel, XAML Expander, and English-only strings** - `295b60d` (feat)

## Files Created/Modified
- `src/Valt.App/Modules/LoanReports/` - New CQRS module
- `src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs` - Snapshot-based projection
- `src/Valt.Infra/Extensions.cs` - DI registration
- `src/Valt.UI/Views/Main/Tabs/Reports/LoanCostChartData.cs` - Stacked-bar chart wrapper
- `src/Valt.UI/Views/Main/Tabs/Reports/LiquidationDistanceChartData.cs` - Line chart wrapper
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` - New fetch method and flags
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` - New Expander section
- `src/Valt.UI/Lang/language.resx` - English strings
- `src/Valt.UI/Lang/language.Designer.cs` - Generated properties

## Decisions Made
- Distance line color is selected from the minimum distance in the dataset using the same risk-band thresholds as `DashboardDataBrushes.ForLtv`, per the UI-SPEC color rule.
- Fees are assigned to the month containing each snapshot's `EffectiveDate`, contingent on the loan being active at month-end.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None.

## Next Phase Readiness
- Ready for Plan 42-02 (query tests and edge-case hardening).
- Ready for Plan 42-03 (ViewModel tests and full suite verification).

---
*Phase: 42-loans-leverage-reports-ui*
*Completed: 2026-08-11*
