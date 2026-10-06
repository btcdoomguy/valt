---
phase: 39-spending-analytics-reports-ui
plan: 02
subsystem: api
tags: [avalonia, dotnet, litedb, cqrs, spending-analytics, fixed-expenses]

requires:
  - phase: 39-01
    provides: SpendingAnalytics App-layer module skeleton (SavingsRate/BurnRate DTOs, query/handler/contract shape, DI registration area)

provides:
  - GetFixedVsVariableQuery + Handler + IFixedVsVariableQueries contract
  - FixedVsVariableDataDto with HasNoFixedExpenses flag and FixedVsVariableMonthDto
  - FixedVsVariableQueries LiteDB implementation joining budget_fixedexpenserecords on Paid state
  - tests/Valt.Tests/Reports/FixedVsVariableQueriesTests.cs with 7 behavior tests
  - DI registration for IFixedVsVariableQueries in Extensions.cs

affects:
  - 39-03 (burn rate dashboard card UI reuses the same ReportsViewModel fetch pattern)
  - 39-04 (savings rate + fixed/variable chart sections consume these queries/DTOs)
  - 43 (MCP/documentation/localization will expose the new query)

tech-stack:
  added: []
  patterns:
    - "App-layer CQRS: query/handler/contract/DTO in Valt.App, implementation in Valt.Infra"
    - "Direct LiteDB aggregation mirroring SpendingEvolutionQueries"
    - "Paid-only fixed-expense record join via budget_fixedexpenserecords"
    - "Currency conversion to main fiat via ICurrencyConversionService"

key-files:
  created:
    - src/Valt.App/Modules/SpendingAnalytics/Queries/GetFixedVsVariableQuery.cs
    - src/Valt.App/Modules/SpendingAnalytics/Queries/GetFixedVsVariableHandler.cs
    - src/Valt.App/Modules/SpendingAnalytics/Contracts/IFixedVsVariableQueries.cs
    - src/Valt.App/Modules/SpendingAnalytics/DTOs/FixedVsVariableDataDto.cs
    - src/Valt.Infra/Modules/SpendingAnalytics/Queries/FixedVsVariableQueries.cs
    - tests/Valt.Tests/Reports/FixedVsVariableQueriesTests.cs
  modified:
    - src/Valt.Infra/Extensions.cs

key-decisions:
  - "Mirrored SpendingEvolutionQueries constructor and direct-LiteDB aggregation for the genuinely new fixed/variable computation"
  - "Used the transaction's actual converted amount (never the FixedExpenseRange planned amount) to satisfy D-05"
  - "Filtered FixedExpenseRecordStateId == Paid && Transaction != null to satisfy D-06"

requirements-completed: [SPA-03]

duration: 9min
completed: 2026-08-04
status: complete
---

# Phase 39 Plan 02: Fixed vs Variable Query Backend Summary

**Paid-only fixed/variable expense split backend with LiteDB record join and main-fiat conversion, dispatchable via IQueryDispatcher**

## Performance

- **Duration:** 9 min
- **Started:** 2026-08-04T22:29:01Z
- **Completed:** 2026-08-04T22:38:52Z
- **Tasks:** 3
- **Files modified:** 7

## Accomplishments

- Added App-layer CQRS types for the fixed vs variable metric (query, handler, contract, DTO)
- Implemented `FixedVsVariableQueries` following the direct-LiteDB pattern used by `SpendingEvolutionQueries`
- Joined `budget_fixedexpenserecords` on `Paid` state + bound transaction to identify fixed expenses
- Converted all expense amounts to the main fiat currency via `ICurrencyConversionService`
- Added the `HasNoFixedExpenses` empty-state flag required by D-10
- Covered all 7 required behaviors with NUnit tests using direct `FixedExpenseRecordEntity` seeding

## Task Commits

Each task was committed atomically:

1. **Task 1: App layer — fixed/variable query, handler, contract, DTO** - `2bb2f24` (feat)
2. **Task 2: RED — failing behavior tests for fixed vs variable query** - `315ea43` (test)
3. **Task 3: GREEN — FixedVsVariableQueries implementation + DI registration** - `c4710ca` (feat)

**Plan metadata:** (pending final docs commit)

## Files Created/Modified

- `src/Valt.App/Modules/SpendingAnalytics/Queries/GetFixedVsVariableQuery.cs` - Query record with date/category/account filters
- `src/Valt.App/Modules/SpendingAnalytics/Queries/GetFixedVsVariableHandler.cs` - Pass-through handler
- `src/Valt.App/Modules/SpendingAnalytics/Contracts/IFixedVsVariableQueries.cs` - Infra contract
- `src/Valt.App/Modules/SpendingAnalytics/DTOs/FixedVsVariableDataDto.cs` - Result DTO + month DTO with HasNoFixedExpenses
- `src/Valt.Infra/Modules/SpendingAnalytics/Queries/FixedVsVariableQueries.cs` - LiteDB implementation
- `src/Valt.Infra/Extensions.cs` - Singleton DI registration
- `tests/Valt.Tests/Reports/FixedVsVariableQueriesTests.cs` - 7 behavior tests

## Decisions Made

- Mirrored `SpendingEvolutionQueries` constructor and direct-LiteDB aggregation for the genuinely new fixed/variable computation
- Used the transaction's actual converted amount (never the `FixedExpenseRange` planned amount) to satisfy D-05
- Filtered `FixedExpenseRecordStateId == Paid && Transaction != null` to satisfy D-06

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Added missing FixedExpenseRange to test fixture**
- **Found during:** Task 3 (GREEN test run)
- **Issue:** `FixedExpenseBuilder.Build()` threw `ArgumentException: At least one range must be provided` in every test because `AddFixedExpense()` created a fixed expense without ranges
- **Fix:** Added `.WithFixedAmountRange(100m, FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 1)` to the helper
- **Files modified:** `tests/Valt.Tests/Reports/FixedVsVariableQueriesTests.cs`
- **Verification:** `dotnet test --filter "FullyQualifiedName~FixedVsVariableQueriesTests"` → 7 passed
- **Committed in:** `c4710ca` (Task 3 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Minor test-fixture correction; no scope creep or design change

## Issues Encountered

- `dotnet test` full suite reports 2 failures in `CoinGeckoProviderTests.Should_Get_Prices_With_Usd_And_Up_To_Date`. These are live network/API tests hitting CoinGecko; they fail with HTTP 403 and are unrelated to the SpendingAnalytics code (same pre-existing failures documented in 39-01-SUMMARY.md). All 7 fixed/variable tests, all 20 spending-analytics-filtered tests, and `dotnet build Valt.sln` pass.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- SPA-03 backend is complete and dispatchable via `IQueryDispatcher`
- Ready for 39-03 (burn rate dashboard card UI) and 39-04 (savings rate + fixed/variable chart sections)
- No blockers

---
*Phase: 39-spending-analytics-reports-ui*
*Completed: 2026-08-04*

## Self-Check: PASSED

- [x] All created files exist on disk
- [x] Task commits found in git history: `2bb2f24`, `315ea43`, `c4710ca`
- [x] `dotnet test --filter "FullyQualifiedName~FixedVsVariable"`: 7 passed, 0 failed
- [x] `dotnet build Valt.sln`: 0 errors
- [x] Pre-existing full-suite CoinGeKo 403 failures documented as out-of-scope
