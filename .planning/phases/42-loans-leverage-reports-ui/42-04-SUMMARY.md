---
phase: 42-loans-leverage-reports-ui
plan: 04
subsystem: reports
tags: [loan-reports, currency-conversion, regression-test, avalonia, dotnet]

requires:
  - phase: 42-loans-leverage-reports-ui
    provides: LoanReportsQueries and test suite baseline from plans 42-01..42-03

provides:
  - Fixed monthly cost compounding bug in LoanReportsQueries.GetLoanReportsAsync
  - Regression test covering mixed-currency multi-loan monthly cost

affects:
  - 42-loans-leverage-reports-ui

actuals:
  tokens: 1600
  tasks: 2
  commits: 2

tech-stack:
  added: []
  patterns: []

key-files:
  created: []
  modified:
    - src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs
    - tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs

key-decisions:
  - "Computed per-loan raw interest and fees in the loan's currency, converted each value to the main currency exactly once, and then added to running monthly totals to avoid re-conversion compounding."
  - "Kept the conversion try/catch scoped to the per-loan conversion so a missing fiat rate zeros only that loan's contribution, not the entire month."

patterns-established: []

requirements-completed:
  - LON-01
  - LON-02

coverage:
  - id: D1
    description: "LoanReportsQueries.GetLoanReportsAsync accumulates monthly interest and fees in the main currency without compounding across mixed-currency loans."
    requirement: LON-01
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs#Should_Not_Compound_Monthly_Cost_For_Multiple_Mixed_Currency_Loans"
        status: pass
      - kind: unit
        ref: "dotnet build Valt.sln"
        status: pass
    human_judgment: false
  - id: D2
    description: "Regression test fails if the previous compounding conversion of running totals is reintroduced."
    requirement: LON-02
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs#Should_Not_Compound_Monthly_Cost_For_Multiple_Mixed_Currency_Loans"
        status: pass
    human_judgment: false

duration: 5min
completed: 2026-08-12
status: complete
---

# Phase 42 Plan 04: Mixed-currency loan cost compounding fix and regression test

**Fixed the Loans & Leverage Reports monthly cost compounding bug by converting each loan's interest and fees individually before summing, and added a regression test that fails if the old re-conversion logic returns.**

## Performance

- **Duration:** 5 min
- **Started:** 2026-08-12T14:54:00Z
- **Completed:** 2026-08-12T14:59:03Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Refactored `LoanReportsQueries.GetLoanReportsAsync` to compute monthly interest and fees strictly in the main currency by converting each loan's raw values once before adding them to the monthly totals.
- Preserved per-loan exception handling so a missing fiat rate zeroes only that loan's contribution, not the entire month.
- Added `Should_Not_Compound_Monthly_Cost_For_Multiple_Mixed_Currency_Loans`, a regression test with one USD and one BRL BTC-backed loan that asserts the correct January 2025 combined cost and rejects the old compounding value.

## Task Commits

Each task was committed atomically:

1. **Task 1: Refactor LoanReportsQueries monthly cost loop to convert per loan** - `8e7436e` (fix)
2. **Task 2: Add regression test for mixed-currency multi-loan monthly cost** - `06813e8` (test)

**Plan metadata:** to be committed with SUMMARY.md

## Files Created/Modified

- `src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs` - Per-loan raw interest and fees are now converted once to the main currency before being accumulated; missing rates zero only the offending loan's contribution.
- `tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs` - Added `Should_Not_Compound_Monthly_Cost_For_Multiple_Mixed_Currency_Loans` regression test.

## Decisions Made

- Followed the dashboard pattern in `GetBtcLoansDashboardHandler` where each loan's accrued values are converted individually before summing.
- Did not change the liquidation distance calculation or the output DTO shape.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 42 now has 4/4 plans complete.
- The Loans & Leverage Reports cost chart should now align with the dashboard's accumulated interest figure.
- Phase 43 (MCP, localization, documentation and verification) can start once STATE.md/ROADMAP.md are updated.

---
*Phase: 42-loans-leverage-reports-ui*
*Completed: 2026-08-12*
