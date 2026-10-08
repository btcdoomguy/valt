---
phase: 42-loans-leverage-reports-ui
plan: 02
subsystem: infra
tags: [tests, loan-reports, queries, hardening]

requires:
  - 42-01
provides:
  - NUnit test fixture for LoanReportsQueries
  - Hardened LoanReportsQueries implementation
affects:
  - 42-03

actuals:
  tokens: 26000
  tasks: 2
  commits: 2

tech-stack:
  added: []
  patterns:
    - "DatabaseTest fixture with seeded BTC/fiat prices and a dummy transaction"
    - "Day-inclusive APR accrual capped by next snapshot and today"
    - "Loan-currency to main-currency conversion via USD bridge"
    - "Current total debt-based liquidation distance with missing-rate fallback"

key-files:
  created:
    - tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs
  modified:
    - src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs

key-decisions:
  - "Interest accrues for the inclusive number of days between max(snapshot date, month start) and min(month end, next snapshot, today)."
  - "Monthly costs are converted to the user's main currency; missing rates zero out the cost contribution for that loan/month instead of throwing."
  - "Liquidation distance uses CurrentTotalDebt (TotalBorrowed + accrued interest + fees) rather than the original principal."
  - "Tests seed a dummy transaction so ReportDataProvider loads BTC/fiat rate collections."

requirements-completed:
  - LON-01
  - LON-02

coverage:
  - id: D1
    description: "LoanReportsQueriesTests covers empty state, APR accrual, fees assignment, fixed debt, month-end boundaries, currency conversion, liquidation distance, and current incomplete month"
    requirement: LON-02
    verification:
      - kind: test
        ref: "dotnet test --filter FullyQualifiedName~LoanReportsQueriesTests"
        status: pass
    human_judgment: false
  - id: D2
    description: "LoanReportsQueries handles day-inclusive APR, fixed debt, currency conversion, and missing-rate distance fallback"
    requirement: LON-01
    verification:
      - kind: test
        ref: "dotnet test --filter FullyQualifiedName~LoanReports"
        status: pass
    human_judgment: false

duration: 55min
completed: 2026-08-11
status: complete
---

# Phase 42 Plan 02: LoanReports Query Tests & Hardening Summary

**Hardened the LoanReports query implementation with a full NUnit test fixture covering month boundaries, APR accrual, fixed debt, currency conversion, and liquidation distance.**

## Performance

- **Duration:** 55 min
- **Started:** 2026-08-11T21:45:00Z
- **Completed:** 2026-08-11T22:40:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Created `tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs` with 10 focused tests using the `DatabaseTest` base.
- Seeded daily BTC/USD/BRL prices and a dummy transaction so `ReportDataProvider` loads historical rates.
- Hardened `src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs`:
  - Switched APR accrual to an inclusive day count.
  - Added loan-currency to main-currency conversion with a USD bridge.
  - Zeroed costs when required fiat/BTC rates are missing instead of propagating exceptions.
  - Updated liquidation distance to use `CurrentTotalDebt`.
  - Preserved fee assignment to the snapshot's effective month and fixed-debt no-interest behavior.

## Task Commits

1. **Task 1: Add LoanReportsQueriesTests** - `971552d` (feat)
2. **Task 2: Harden LoanReportsQueries** - `2ad1807` (feat)

## Files Created/Modified
- `tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs` - New test fixture
- `src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs` - Hardened query implementation

## Decisions Made
- Interest is calculated with inclusive day boundaries so a full calendar month accrues the expected number of days.
- Costs are converted per-loan to the user's main currency; the test fixture uses BRL as default main currency and asserts converted values.
- Missing historical rates do not crash the report; distance and cost contributions for that loan/month fall back to zero.

## Deviations from Plan

None - all acceptance criteria met.

## Issues Encountered
- Initial tests failed because `ReportDataProvider` clears rate collections when no transactions exist. Resolved by seeding a dummy transaction.
- Loan-currency conversion tests initially used the default USD currency; fixed by explicitly setting `currencyCode: FiatCurrency.Brl.Code` on the BRL loan builder.

## User Setup Required
None.

## Next Phase Readiness
- Ready for Plan 42-03 (ViewModel tests and full suite verification).

---
*Phase: 42-loans-leverage-reports-ui*
*Completed: 2026-08-11*
