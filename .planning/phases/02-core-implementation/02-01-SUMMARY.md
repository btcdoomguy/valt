---
phase: 02-core-implementation
plan: 01
subsystem: api
tags: [cqrs, litedb, aggregation, spending-evolution, dotnet]

# Dependency graph
requires:
  - phase: 01-foundation
    provides: "Project structure, DI container, database access patterns"
provides:
  - "CQRS query pipeline for spending evolution data aggregation"
  - "Monthly aggregated spending data with fiat and sats totals"
  - "Currency conversion to user's primary fiat currency"
  - "Missing price data detection for sats amounts"
affects:
  - "02-02-chart-ui"
  - "02-03-viewmodel"

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "CQRS Query + Handler + Interface pattern"
    - "LiteDB-side filtering with in-memory aggregation"
    - "Currency conversion using IPriceDatabase local rates"

key-files:
  created:
    - src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionQuery.cs
    - src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionHandler.cs
    - src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionMonthDto.cs
    - src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionDataDto.cs
    - src/Valt.App/Modules/SpendingEvolution/Contracts/ISpendingEvolutionQueries.cs
    - src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs
  modified:
    - src/Valt.App/Extensions.cs
    - src/Valt.Infra/Extensions.cs

key-decisions:
  - "Placed ISpendingEvolutionQueries interface in App layer (Contracts/) following existing architecture pattern (ITransactionQueries, etc.) instead of Infra layer as originally planned"
  - "Used IPriceDatabase local rates for currency conversion to avoid network calls and meet <500ms performance target"
  - "HasMissingPriceInSats tracks Bitcoin-type transactions without FromSatAmount, not all null sat amounts"

patterns-established:
  - "SpendingEvolution module follows same CQRS pattern as Budget module: Query in App, Implementation in Infra, Interface in App.Contracts"
  - "LiteDB query chaining for date range, category, account visibility, and debit-type filters before ToList()"

requirements-completed: [DATA-01, DATA-02, DATA-03, DATA-07, INT-01, INT-02]

# Metrics
duration: 7min
completed: 2026-05-27
---

# Phase 02 Plan 01: CQRS Query Layer for Spending Evolution Data Aggregation

**CQRS query pipeline that aggregates debit transactions by month with fiat conversion to primary currency and sats totals, using LiteDB-side filtering for <500ms performance.**

## Performance

- **Duration:** 7 min
- **Started:** 2026-05-27T18:56:12Z
- **Completed:** 2026-05-27T19:03:29Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- Created GetSpendingEvolutionQuery with date range, category filter, and hidden account toggle
- Created SpendingEvolutionMonthDto and SpendingEvolutionDataDto records with required init properties
- Created GetSpendingEvolutionHandler that auto-registers via assembly scanning
- Created ISpendingEvolutionQueries contract interface in App layer
- Implemented SpendingEvolutionQueries with LiteDB query-side filtering and in-memory aggregation
- Added currency conversion from account currency to primary currency using local price database rates
- Registered all components in DI container

## Task Commits

Each task was committed atomically:

1. **Task 1: Create App layer Query, DTOs, and Handler** - `8ec2b48` (feat)
2. **Task 2: Create Infra layer Query Implementation and Register in DI** - `cce5522` (feat)

**Plan metadata:** (pending final commit)

## Files Created/Modified
- `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionQuery.cs` - Query definition with date range, category IDs, and visibility filters
- `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionHandler.cs` - Query handler delegating to ISpendingEvolutionQueries
- `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionMonthDto.cs` - Monthly aggregate DTO with fiat, sats, and count
- `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionDataDto.cs` - Response wrapper with months list and metadata
- `src/Valt.App/Modules/SpendingEvolution/Contracts/ISpendingEvolutionQueries.cs` - Query contract interface
- `src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs` - LiteDB aggregation implementation with currency conversion
- `src/Valt.App/Extensions.cs` - Added comment noting SpendingEvolution handlers auto-register
- `src/Valt.Infra/Extensions.cs` - Registered ISpendingEvolutionQueries as singleton

## Decisions Made
- Placed ISpendingEvolutionQueries in App layer Contracts/ folder following the existing pattern used by ITransactionQueries, IAccountQueries, etc. The plan originally specified Infra layer but this would violate dependency direction (App cannot reference Infra).
- Used IPriceDatabase (local cached rates) instead of live price providers for currency conversion to guarantee query performance stays under 500ms without network dependency.
- For HasMissingPriceInSats detection, only flag Bitcoin-typed transactions that lack FromSatAmount, rather than all transactions with null sat amounts (fiat transactions legitimately have null sats).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Moved ISpendingEvolutionQueries from Infra to App layer**
- **Found during:** Task 1 (Create App layer Query, DTOs, and Handler)
- **Issue:** The plan specified placing ISpendingEvolutionQueries in `src/Valt.Infra/Modules/SpendingEvolution/Queries/`, but the GetSpendingEvolutionHandler in the App layer needs to reference this interface. Valt.App does not reference Valt.Infra (correct dependency direction: App -> Core, Infra -> App + Core).
- **Fix:** Created `ISpendingEvolutionQueries` in `src/Valt.App/Modules/SpendingEvolution/Contracts/` following the existing codebase pattern where all query interfaces (ITransactionQueries, IAccountQueries, etc.) live in the App layer.
- **Files modified:** Added `src/Valt.App/Modules/SpendingEvolution/Contracts/ISpendingEvolutionQueries.cs`; updated handler usings
- **Verification:** Build succeeds, handler correctly references interface
- **Committed in:** `8ec2b48` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 missing critical)
**Impact on plan:** Auto-fix necessary for architectural correctness. No scope creep.

## Issues Encountered
- Initial build failure due to missing `using Valt.Infra.Modules.Budget.Transactions;` for `TransactionEntityType` enum - fixed by adding the using directive.
- Initial build failure in Extensions.cs due to missing `using Valt.App.Modules.SpendingEvolution.Contracts;` - fixed by adding the using directive.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Query pipeline complete and ready for UI consumption
- Next phase (02-02) can use `IQueryDispatcher.DispatchAsync(new GetSpendingEvolutionQuery { ... })` to fetch chart data
- No blockers

---
*Phase: 02-core-implementation*
*Completed: 2026-05-27*
