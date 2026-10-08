---
phase: 39-spending-analytics-reports-ui
plan: 01
subsystem: reports

tags: [cqrs, reports, spending-analytics, savings-rate, burn-rate, nunit, litedb]

requires:
  - phase: 29-31
    provides: Core domain, accounts, transactions, categories, fixed expenses
  - phase: 34
    provides: MonthlyTotalsReport, StatisticsReport, IReportDataProvider
  - phase: 37
    provides: SpendingEvolution CQRS pattern reference

provides:
  - Valt.App.Modules.SpendingAnalytics query/handler/contract/DTO layer
  - Valt.Infra.Modules.SpendingAnalytics query implementations reusing IMonthlyTotalsReport and IStatisticsReport
  - DI registrations for ISavingsRateQueries and IBurnRateQueries
  - NUnit behavior tests for savings rate and burn rate math

affects:
  - 39-02 (fixed vs variable query backend can follow the same module pattern)
  - 39-03 (burn-rate dashboard card consumes GetBurnRateQuery)
  - 39-04 (savings-rate chart consumes GetSavingsRateQuery)
  - 43 (MCP/localization/docs will reference these queries)

tech-stack:
  added: []
  patterns:
    - App-layer CQRS with IQuery<T>, IQueryHandler<T,Q>, I*Queries contract, DTO records
    - Infra query implementation reusing IReportDataProviderFactory + existing reports

key-files:
  created:
    - src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateQuery.cs
    - src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateHandler.cs
    - src/Valt.App/Modules/SpendingAnalytics/Queries/GetBurnRateQuery.cs
    - src/Valt.App/Modules/SpendingAnalytics/Queries/GetBurnRateHandler.cs
    - src/Valt.App/Modules/SpendingAnalytics/Contracts/ISavingsRateQueries.cs
    - src/Valt.App/Modules/SpendingAnalytics/Contracts/IBurnRateQueries.cs
    - src/Valt.App/Modules/SpendingAnalytics/DTOs/SavingsRateDataDto.cs
    - src/Valt.App/Modules/SpendingAnalytics/DTOs/BurnRateDataDto.cs
    - src/Valt.Infra/Modules/SpendingAnalytics/Queries/SavingsRateQueries.cs
    - src/Valt.Infra/Modules/SpendingAnalytics/Queries/BurnRateQueries.cs
    - tests/Valt.Tests/Reports/SavingsRateQueriesTests.cs
    - tests/Valt.Tests/Reports/BurnRateQueriesTests.cs
  modified:
    - src/Valt.Infra/Extensions.cs

key-decisions:
  - "Savings rate uses AllIncomeInFiat/AllExpensesInFiat from IMonthlyTotalsReport so numbers match the existing Monthly totals panel (D-01)."
  - "Burn rate median is read from IStatisticsReport.MedianMonthlyExpenses, not recomputed (D-13)."
  - "Current incomplete month is excluded in the query implementation, not the UI (D-04)."
  - "Zero-income months emit a null Rate for LiveCharts EnableNullSplitting gaps (D-02)."
  - "Burn rate projection is null before day 5 of the month (D-15)."

requirements-completed: [SPA-01, SPA-02]

duration: 11 min
completed: 2026-08-04
status: complete
---

# Phase 39 Plan 01: Spending Analytics Query Backend (Savings Rate + Burn Rate) Summary

**SpendingAnalytics App-layer CQRS module with savings-rate and burn-rate queries that reuse IMonthlyTotalsReport and IStatisticsReport for byte-identical numbers, plus NUnit behavior tests.**

## Performance

- **Duration:** 11 min
- **Started:** 2026-08-04T22:09:54Z
- **Completed:** 2026-08-04T22:21:04Z
- **Tasks:** 3
- **Files modified:** 13

## Accomplishments

- Created the `Valt.App.Modules.SpendingAnalytics` query/handler/contract/DTO layer for savings rate and burn rate.
- Implemented `SavingsRateQueries` and `BurnRateQueries` in `Valt.Infra` reusing `IMonthlyTotalsReport` and `IStatisticsReport` via `IReportDataProviderFactory`.
- Registered `ISavingsRateQueries` and `IBurnRateQueries` in DI alongside `ISpendingEvolutionQueries`.
- Wrote 10 NUnit behavior tests covering positive/null/negative savings rates, current-month exclusion, burn-rate projection day-5 gate, median reuse, and empty states.

## Task Commits

Each task was committed atomically:

1. **Task 1: App layer queries, handlers, contracts, DTOs** - `4b0eed7` (feat)
2. **Task 2: RED behavior tests** - `a2594bd` (test)
3. **Task 3: Infra implementations + DI registration** - `58b3794` (feat)

**Plan metadata:** docs(39-01) commit in git log (39-01-SUMMARY.md + STATE.md + ROADMAP.md + REQUIREMENTS.md)

## Files Created/Modified

- `src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateQuery.cs` - Query record with date range and filter arrays
- `src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateHandler.cs` - Pass-through query handler
- `src/Valt.App/Modules/SpendingAnalytics/Queries/GetBurnRateQuery.cs` - Query record with CurrentWealthInFiat and filter arrays
- `src/Valt.App/Modules/SpendingAnalytics/Queries/GetBurnRateHandler.cs` - Pass-through query handler
- `src/Valt.App/Modules/SpendingAnalytics/Contracts/ISavingsRateQueries.cs` - Infra contract
- `src/Valt.App/Modules/SpendingAnalytics/Contracts/IBurnRateQueries.cs` - Infra contract
- `src/Valt.App/Modules/SpendingAnalytics/DTOs/SavingsRateDataDto.cs` - Data + month DTOs with nullable Rate
- `src/Valt.App/Modules/SpendingAnalytics/DTOs/BurnRateDataDto.cs` - Burn rate gauge DTO
- `src/Valt.Infra/Modules/SpendingAnalytics/Queries/SavingsRateQueries.cs` - Reuses MonthlyTotalsReport
- `src/Valt.Infra/Modules/SpendingAnalytics/Queries/BurnRateQueries.cs` - Reuses MonthlyTotalsReport + StatisticsReport
- `src/Valt.Infra/Extensions.cs` - DI registrations
- `tests/Valt.Tests/Reports/SavingsRateQueriesTests.cs` - 5 behavior tests
- `tests/Valt.Tests/Reports/BurnRateQueriesTests.cs` - 5 behavior tests

## Decisions Made

- Followed the `SpendingEvolution` CQRS pattern exactly for the new App-layer module (D-19).
- Reused `IMonthlyTotalsReport` for income/expense values and `IStatisticsReport` for median to avoid divergent calculations (D-01/D-13).
- Excluded the current incomplete month in the query implementation using injected `IClock` (D-04).
- Used nullable `Rate` for zero-income months to produce chart gaps via `EnableNullSplitting` (D-02).
- Gated burn-rate projection to day >= 5 to avoid early-month noise (D-15).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed a pre-existing typo in TransactionsView.axaml that prevented `dotnet build Valt.sln`**
- **Found during:** Task 1 acceptance verification (`dotnet build Valt.sln`)
- **Issue:** `Margin="4, 0, -1sd, 0"` in `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsView.axaml` caused Avalonia error AVLN2005 (unparseable thickness). The build could not succeed, blocking all plan verification.
- **Fix:** Changed `-1sd` to `-1` to match the intended thickness value. The file reverted to the committed HEAD state, so no additional commit was required.
- **Files modified:** `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsView.axaml`
- **Verification:** `dotnet build Valt.sln` now succeeds with 0 errors.
- **Committed in:** No separate commit (working-tree fix only; no net change from HEAD).

### Environmental Issues Encountered

- `dotnet test` full suite reports 2 failures in `CoinGeckoProviderTests.Should_Get_Prices_With_Usd_And_Up_To_Date`. These are live network/API tests hitting CoinGecko; they fail at `EnsureSuccessStatusCode()` and are unrelated to the SpendingAnalytics code. All 20 spending-analytics-filtered tests and the full solution build pass.

---

**Total deviations:** 1 auto-fixed (1 blocking), 2 environmental test failures unrelated to plan
**Impact on plan:** No scope creep. The only code change was a one-character typo fix required to unblock the mandated full-solution build verification.

## Issues Encountered

- Initial RED tests leaked transactions across tests because `DatabaseTest` uses a single in-memory database per fixture. Added `[TearDown]` to delete all transactions after each test.
- Initial test assertions assumed the query would return only months with data; `MonthlyTotalsReport` returns every month in the display range, so tests were changed to look up the target month by date.
- Full-suite `dotnet test` has 2 pre-existing CoinGecko live-API failures (environmental/network).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `GetSavingsRateQuery` and `GetBurnRateQuery` are dispatchable via `IQueryDispatcher` and registered in DI.
- Wave 2 plans (39-02 fixed/variable, 39-03 burn-rate UI, 39-04 savings-rate chart) can consume these queries.
- `dotnet build Valt.sln` is green. Spending-analytics-filtered tests (`--filter "FullyQualifiedName~SpendingAnalytics|FullyQualifiedName~SavingsRate|FullyQualifiedName~BurnRate"`) all pass.

## Self-Check: PASSED

- All 13 key files exist on disk.
- All 3 task commits (4b0eed7, a2594bd, 58b3794) and the metadata commit exist in git history.
- `dotnet build Valt.sln` succeeded with 0 errors.
- Spending-analytics-filtered tests passed 20/20.
- STATE.md, ROADMAP.md, and REQUIREMENTS.md updated and committed.

---
*Phase: 39-spending-analytics-reports-ui*
*Completed: 2026-08-04*
