---
phase: 49-goal-contributing-transactions-query-backend
plan: 01
subsystem: api
tags: [cqrs, goals, litedb, avalonia, dotnet, query]

requires:
  - phase: 48-goal-progress-updater (and prior goals phases)
    provides: IGoalProgressCalculator hierarchy, GoalTransactionReader, GoalProgressInput, GoalQueries

provides:
  - App-layer GetGoalContributingTransactionsQuery + handler returning GoalContributingTransactionsResult union (Supported rows / typed NotSupported)
  - ContributingTransactionRow DTO with all eight GOL-05 fields and Q2/Q3-documented RunningTotal semantics
  - IGoalProgressCalculator.GetContributingTransactionsAsync default NotSupported fallback (NetWorthBtc keeps it)
  - GoalTransactionReader.GetExpenseRows as the single expense-selection path (CalculateTotalExpenses now sums rows)
  - GoalContributingTransactionsService resolving calculator per goal and mapping rows with account/category names
  - GoalPeriodRangeHelper extracted as the single period-range source
  - Reconciling test harness in GetGoalContributingTransactionsHandlerTests (11 tests)

affects: [50-goal-contributing-transactions-ui, 51-goal-mcp-localization]

actuals:
  tokens: 21000
  tasks: 3
  commits: 3

tech-stack:
  added: []
  patterns:
    - "Calculator-exposure: per-strategy rows via IGoalProgressCalculator default-interface fallback (null = not transaction-based)"
    - "Single selection path: aggregates re-implemented as Sum() over row methods so summary cannot drift from progress math"
    - "App handler stays thin: only IGoalQueries dependency, zero transaction-selection knowledge"

key-files:
  created:
    - src/Valt.App/Modules/Goals/DTOs/ContributingTransactionRow.cs
    - src/Valt.App/Modules/Goals/DTOs/GoalContributingTransactionsResult.cs
    - src/Valt.App/Modules/Goals/Queries/GetGoalContributingTransactions/GetGoalContributingTransactionsQuery.cs
    - src/Valt.App/Modules/Goals/Queries/GetGoalContributingTransactions/GetGoalContributingTransactionsHandler.cs
    - src/Valt.Infra/Modules/Goals/Services/GoalContributionRow.cs
    - src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs
    - src/Valt.Infra/Modules/Goals/GoalPeriodRangeHelper.cs
    - tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs
  modified:
    - src/Valt.App/Modules/Goals/Contracts/IGoalQueries.cs
    - src/Valt.Infra/Modules/Goals/Services/IGoalProgressCalculator.cs
    - src/Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs
    - src/Valt.Infra/Modules/Goals/Services/SpendingLimitProgressCalculator.cs
    - src/Valt.Infra/Modules/Goals/Queries/GoalQueries.cs
    - src/Valt.Infra/Extensions.cs
    - tests/Valt.Tests/DatabaseTest.cs

key-decisions:
  - "GoalContributionRow is public (not internal as inventory suggested) because IGoalProgressCalculator is a public interface and C# forbids less-accessible return types (CS0050)"
  - "LiteDB 5.0.21 ObjectId has no TryParse — service uses a private 24-char hex defensive parser (mirrors the plan's T-49-01 mitigation intent)"
  - "RunningTotal accumulates in decimal in (Date, Id) sorted order inside the reader; the service re-asserts the same ordering after mapping"

patterns-established:
  - "Row selection lives in exactly one place per strategy (GetExpenseRows); CalculateTotalExpenses delegates to it"
  - "Sats conversions only via closest-date price lookups with 7-day buffer — never live rates"
  - "Test fixtures seed prices with PriceDataBuilder.SeedRange across period ± 7-day buffer before any conversion-touching assertion"

requirements-completed: [GOL-05, GOL-06, GOL-07]

coverage:
  - id: D1
    description: "SpendingLimit contributing-transactions slice end-to-end: one fiat expense returns Supported with a full row whose final RunningTotal reconciles with CalculateProgressAsync CalculatedSpending and sats derive from the seeded tx-date price"
    requirement: GOL-05
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#SpendingLimit_Reconciles"
        status: pass
    human_judgment: false
  - id: D2
    description: "Union plumbing: NetWorthBtc returns typed NotSupported via interface default; missing/malformed ids return GOAL_NOT_FOUND; empty set returns Supported with empty list"
    requirement: GOL-06
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#NetWorthBtc_ReturnsNotSupported, MissingGoalId_ReturnsGoalNotFound, MalformedGoalId_ReturnsGoalNotFound, NoTransactions_ReturnsSupportedEmptyList"
        status: pass
    human_judgment: false
  - id: D3
    description: "Set behavior responds to add/remove, transfers are structurally excluded, rows order by (Date, Id) with deterministic same-day running totals"
    requirement: GOL-06
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#AddRemove_ChangesSetAndFinalTotal, Transfers_NeverAppear, Rows_Ascending_WithStableSameDayOrder"
        status: pass
    human_judgment: false
  - id: D4
    description: "GOL-05 edge probes: period boundary dates included / one-day-outside excluded; foreign-currency row converts at tx-date rate into main-currency RunningTotal; per-row sats at each day's seeded price"
    requirement: GOL-05
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#BoundaryDates_Included_OneDayOutside_Excluded, ForeignCurrencyAccount_Converts_AtTxDateRate, Sats_Converted_AtTransactionDatePrice_NotLive"
        status: pass
    human_judgment: false
  - id: D5
    description: "Selection cannot drift from progress math: only SpendingLimit overrides GetContributingTransactionsAsync; NetWorthBtc retains the default fallback"
    requirement: GOL-07
    verification:
      - kind: unit
        ref: "grep gate: exactly 1 concrete calculator overrides (SpendingLimitProgressCalculator); NetWorthBtcProgressCalculator override count 0"
        status: pass
    human_judgment: false

duration: 45min
completed: 2026-10-06
status: complete
---

# Phase 49 Plan 01: Goal Contributing-Transactions Query Backend (Tracer Slice) Summary

**CQRS query exposing SpendingLimit contributing transactions with running totals that reconcile exactly with CalculateProgressAsync, via a calculator-level row-exposure mechanism all nine other transaction-based strategies will plug into**

## Performance

- **Duration:** ~45 min
- **Started:** 2026-10-06 (session)
- **Completed:** 2026-10-06
- **Tasks:** 3
- **Files modified:** 15

## Accomplishments
- End-to-end tracer slice: App query → IGoalQueries → GoalContributingTransactionsService → SpendingLimitProgressCalculator → GoalTransactionReader.GetExpenseRows, returning one reconciled row per expense.
- `IGoalProgressCalculator.GetContributingTransactionsAsync` default-interface fallback delivers typed `NotSupported` for NetWorthBtc with zero NetWorthBtc code; exactly one calculator overrides it.
- `CalculateTotalExpenses` re-implemented as `GetExpenseRows(...).Sum(r => r.Contribution)` — selection exists in exactly one place (GOL-07 drift-proofing by construction).
- 11-test fixture proving: reconciliation with progress math, row shape (8 GOL-05 fields), boundary dates, foreign-currency conversion at tx-date rates, per-day sats conversion, add/remove, transfer exclusion, deterministic same-day ordering, empty-set semantics, and both GOAL_NOT_FOUND paths.
- Full suite regression: 1789/1789 tests pass.

## Task Commits

Each task was committed atomically:

1. **Task 1 (tracer): SpendingLimit contributing-transactions slice end-to-end** - `4e5c503` (feat)
2. **Task 2: Plumbing tests — NotSupported, GOAL_NOT_FOUND, empty set, add/remove, exclusion, ordering** - `d744764` (test)
3. **Task 3: SpendingLimit boundary and multi-currency fixtures (GOL-05 edge probes)** - `5a9dbaf` (test)

**Plan metadata:** pending final docs commit

## Files Created/Modified
- `src/Valt.App/Modules/Goals/DTOs/ContributingTransactionRow.cs` — 8-field row DTO (created)
- `src/Valt.App/Modules/Goals/DTOs/GoalContributingTransactionsResult.cs` — Supported/NotSupported union (created)
- `src/Valt.App/Modules/Goals/Queries/GetGoalContributingTransactions/` — query + thin handler (created)
- `src/Valt.App/Modules/Goals/Contracts/IGoalQueries.cs` — +GetContributingTransactionsAsync
- `src/Valt.Infra/Modules/Goals/Services/GoalContributionRow.cs` — infra row with Contribution/RunningTotal (created)
- `src/Valt.Infra/Modules/Goals/Services/IGoalProgressCalculator.cs` — default GetContributingTransactionsAsync fallback
- `src/Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs` — GetExpenseRows + ConvertFiatToBtc; aggregates as row sums
- `src/Valt.Infra/Modules/Goals/Services/SpendingLimitProgressCalculator.cs` — rows override
- `src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs` — goal load, period, calculator resolution, DTO mapping (created)
- `src/Valt.Infra/Modules/Goals/GoalPeriodRangeHelper.cs` — single period-range source (created)
- `src/Valt.Infra/Modules/Goals/Queries/GoalQueries.cs` — delegates to service; private GetPeriodRange deleted
- `src/Valt.Infra/Extensions.cs` — DI registration
- `tests/Valt.Tests/DatabaseTest.cs` — full calculator-stack builder for _goalQueries
- `tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs` — 11-test fixture (created)

## Decisions Made
- GoalContributionRow made public: IGoalProgressCalculator is public and C# requires the return type to be at least as accessible (CS0050). The plan's "internal" inventory entry was infeasible without making the interface internal (which would break existing public consumers).
- Defensive id parsing: LiteDB 5.0.21's ObjectId has no TryParse, so the service carries a private 24-hex-char validator — same T-49-01 mitigation, no exception surface.
- RunningTotal accumulates in (Date, Id) order inside the reader and the service re-asserts that order after name mapping, so the final row is stable regardless of enumeration order.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] GoalContributionRow accessibility (CS0050)**
- **Found during:** Task 1 (build)
- **Issue:** Plan inventory declared GoalContributionRow internal, but the public interface IGoalProgressCalculator cannot expose a less-accessible return type.
- **Fix:** Made GoalContributionRow public (record with init-only positional properties, no serialization attributes).
- **Files modified:** src/Valt.Infra/Modules/Goals/Services/GoalContributionRow.cs
- **Verification:** Build succeeds; gate grep shows exactly one concrete calculator override.
- **Committed in:** 4e5c503 (Task 1)

**2. [Rule 3 - Blocking] LiteDB ObjectId has no TryParse**
- **Found during:** Task 1 (build)
- **Issue:** The plan's defensive-parse step assumed ObjectId.TryParse exists; LiteDB 5.0.21 does not provide it.
- **Fix:** Private TryParseObjectId helper (length-24 hex check) in GoalContributingTransactionsService; malformed ids return null → GOAL_NOT_FOUND, verified by MalformedGoalId_ReturnsGoalNotFound.
- **Files modified:** src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs
- **Verification:** MalformedGoalId test passes; no exception surface.
- **Committed in:** 4e5c503 (Task 1)

**3. [Rule 1 - Bug] Same-day ordering test data contradicted the plan's stated running totals**
- **Found during:** Task 2 (test run)
- **Issue:** With expenses 300/500/700/900/1100 the cumulative running totals are 300/800/1500/2400/3500, not the plan's stated 300/500/700/900/1100.
- **Fix:** Used expenses 300/200/200/200/200 so cumulative totals are exactly 300/500/700/900/1100 per the plan's deterministic assertion; added distinct same-day names to prove Id-order stability.
- **Files modified:** tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs
- **Verification:** 8/8 then 11/11 green across two consecutive runs.
- **Committed in:** d744764 (Task 2)

**4. [Documentation note] Calculator gate grep counts the interface file**
- The `<gate>` `grep -l ... *Calculator.cs` matches IGoalProgressCalculator.cs (it defines the method) yielding 2, not 1. Filtering the interface definition file, exactly 1 concrete calculator (SpendingLimitProgressCalculator) overrides; NetWorthBtc override count is 0. Gate intent satisfied.

---

**Total deviations:** 3 auto-fixed (2 blocking, 1 bug) + 1 gate-interpretation note
**Impact on plan:** All fixes were necessary for compilation/correctness; no scope creep. Public-vs-internal on an infra record is a minor surface change with no serialization attributes, consistent with AGENTS.md DTO constraints.

## Issues Encountered
- None beyond the deviations above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Plans 02/03 extend exactly one place per concern: add GetIncomeRows for income-backed strategies, override GetContributingTransactionsAsync on the remaining 8 transaction-based calculators, and add fixtures — the plumbing (query, union, service, reader, DI, test harness) is proven.
- Phase 50 (UI) can bind to GetGoalContributingTransactionsQuery and hide NotSupported goal types; Phase 51 (MCP) consumes the same query.

## Self-Check: PASSED

- All 8 created/modified source artifacts verified on disk during build (Build succeeded, 0 errors).
- Commits 4e5c503, d744764, 5a9dbaf verified via git log.
- Full test suite: 1789 passed, 0 failed.

---
*Phase: 49-goal-contributing-transactions-query-backend*
*Completed: 2026-10-06*
