---
phase: 49-goal-contributing-transactions-query-backend
plan: 02
subsystem: api
tags: [cqrs, goals, litedb, dotnet, query]

requires:
  - phase: 49-01
    provides: IGoalProgressCalculator.GetContributingTransactionsAsync fallback, GoalTransactionReader.GetExpenseRows, GoalContributionRow, GoalContributingTransactionsService, reconciling test harness

provides:
  - IGoalTransactionReader.GetIncomeRows — single fiat-income selection path (Fiat type, positive FromFiatAmount, BitcoinToFiat excluded)
  - CalculateTotalIncome re-implemented as Sum over GetIncomeRows (selection in exactly one place)
  - GetContributingTransactionsAsync overrides on IncomeFiat, SaveFiat, ReduceExpenseCategory, SavingsRate (5 of 9 transaction-based calculators now expose rows)
  - SaveFiat merged mixed-sign RunningTotal (cumulative income minus cumulative expenses)
  - SavingsRate incremental percentage RunningTotal (unrounded accumulators, ±100 clamp, 0 when cumulative income <= 0)
  - 7 new reconciling fixtures in GetGoalContributingTransactionsHandlerTests (18 total)

affects: [50-goal-contributing-transactions-ui, 51-goal-mcp-localization]

actuals:
  tokens: 6000
  tasks: 3
  commits: 3

tech-stack:
  added: []
  patterns:
    - "Origin-flag merge: mixed-sign calculators concat reader row sets tagged with isExpense instead of re-testing transaction predicates — zero TransactionEntityType references in calculators"
    - "with-expression row rebuild: merged accumulators replace RunningTotal via `row with { RunningTotal = ... }` keeping reader-produced Contribution untouched"
    - "Aggregate-as-row-sum: CalculateTotalIncome delegates to GetIncomeRows, mirroring 49-01 CalculateTotalExpenses → GetExpenseRows"

key-files:
  created: []
  modified:
    - src/Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs
    - src/Valt.Infra/Modules/Goals/Services/IncomeFiatProgressCalculator.cs
    - src/Valt.Infra/Modules/Goals/Services/SaveFiatProgressCalculator.cs
    - src/Valt.Infra/Modules/Goals/Services/ReduceExpenseCategoryProgressCalculator.cs
    - src/Valt.Infra/Modules/Goals/Services/SavingsRateProgressCalculator.cs
    - tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs

key-decisions:
  - "SaveFiat/SavingsRate merge tags each row with its reader-set origin (isExpense) at concat time — running-total signs and accumulators come from the flag, never from re-evaluating transaction predicates (plan prohibition against copied Where predicates)"
  - "Plan-02 Task 2 tdd flag honored in plan order: implementation preceded fixtures because the plan itself sequenced the test matrix as Task 3; all fixtures reconcile against real-calculator CalculateProgressAsync output"
  - "DatabaseTest static helper untouched: all four calculators already shared the single GoalTransactionReader instance from 49-01, so the plan's 'extend only if needed' clause required no change"

requirements-completed: [GOL-05, GOL-06, GOL-07]

coverage:
  - id: D1
    description: "IncomeFiat rows contain only Fiat-type positive-FromFiatAmount transactions; BitcoinToFiat transfer row never appears; ascending order; final RunningTotal == IncomeFiatGoalType.CalculatedIncome (GOL-07 adjacency/exclusion)"
    requirement: GOL-07
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#IncomeFiat_Reconciles_AndExcludesBitcoinToFiat"
        status: pass
    human_judgment: false
  - id: D2
    description: "SaveFiat rows merge income and expense sets in one ascending order with per-row RunningTotal as cumulative income-minus-expenses (-300/200/100); final == SaveFiatGoalType.CalculatedSavings; add/remove re-dispatch lands a new earlier income in chronological position and moves the final total (GOL-06)"
    requirement: GOL-06
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#SaveFiat_MixedSigns_Ordered_Reconciles, SaveFiat_AddRemove_ChangesSetPositionAndFinalTotal"
        status: pass
    human_judgment: false
  - id: D3
    description: "ReduceExpenseCategory rows limited to expenses whose CategoryId equals the configured category; equally-sized expense in another category absent; final == CalculatedSpending (GOL-07 category parity probe)"
    requirement: GOL-07
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#ReduceExpenseCategory_CategoryParity"
        status: pass
    human_judgment: false
  - id: D4
    description: "SavingsRate per-row RunningTotal is the incremental percentage — 100.00 after income, 60.00 after expense, 0.00 when cumulative income <= 0 — using unrounded accumulators identical to CalculateProgressAsync; final == CalculatedPercentage (GOL-06 percentage unit)"
    requirement: GOL-06
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#SavingsRate_PercentageRunningTotal_Reconciles, SavingsRate_ExpenseBeforeIncome_FirstRowIsZero"
        status: pass
    human_judgment: false
  - id: D5
    description: "Fiat-income rows carry SatsAmount converted at the transaction-date closest BTC price via the USD-hop helper — 40k-day row 250_000 sats vs 60k-day row 166_666 sats, never a live price (GOL-05 precision probe)"
    requirement: GOL-05
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#FiatIncome_Sats_AtTransactionDatePrice"
        status: pass
    human_judgment: false
  - id: D6
    description: "Selection cannot drift from progress math: no second income-selection predicate — CalculateTotalIncome is literally GetIncomeRows(...).Sum(r => r.Contribution); IncomeFiat/SaveFiat/SavingsRate contain zero TransactionEntityType references"
    requirement: GOL-07
    verification:
      - kind: unit
        ref: "grep gates: CalculateTotalIncome single delegation body; TransactionEntityType count 0 in IncomeFiat/SaveFiat/SavingsRate calculators; 5 concrete calculator overrides"
        status: pass
    human_judgment: false

duration: 25min
completed: 2026-10-06
status: complete
---

# Phase 49 Plan 02: Reader-Backed Calculator Rows (IncomeFiat, SaveFiat, ReduceExpenseCategory, SavingsRate) Summary

**GetIncomeRows as the single fiat-income selection path plus four calculator overrides with mixed-sign merge, category filtering, and incremental percentage running totals — 5 of 9 transaction-based calculators now expose reconciling rows**

## Performance

- **Duration:** ~25 min
- **Completed:** 2026-10-06
- **Tasks:** 3
- **Files modified:** 6 (368 insertions, 6 deletions)

## Accomplishments
- `IGoalTransactionReader.GetIncomeRows(from, to)` selects Fiat-type positive-FromFiatAmount transactions ordered (Date, Id), per-row Contribution identical to the old aggregate, sats via the existing USD-hop `ConvertFiatToBtc` at tx-date closest price; `CalculateTotalIncome` is now literally a Sum over the rows — income selection exists in exactly one place.
- Four calculator overrides: IncomeFiat (rows direct from reader), ReduceExpenseCategory (reader category filter from config ObjectId), SaveFiat (merged mixed-sign re-accumulation), SavingsRate (two unrounded accumulators with the exact CalculateProgressAsync percentage formula clamped to ±100).
- Origin-flag merge pattern keeps every transaction predicate inside the reader — zero `TransactionEntityType` references in any reader-backed calculator.
- 7 new fixtures (18 total in the file): reconciliation against real-calculator `CalculateProgressAsync` output, transfer exclusion, category parity, mixed-sign ordering, per-row percentage including the cumulative-income-≤0 zero branch, tx-date sats proof at two seeded prices, and add/remove with chronological repositioning.
- Full regression: 1796/1796 tests pass (was 1789; +7 new).

## Task Commits

1. **Task 1: GoalTransactionReader.GetIncomeRows with CalculateTotalIncome as row sum** — `cec5b3e` (feat)
2. **Task 2: Reader-backed calculator overrides on the four strategies** — `8667e9c` (feat)
3. **Task 3: Per-strategy fixtures (reconciliation, exclusion, mixed-sign, parity, percentage, sats, add/remove)** — `c3e6ed8` (test)

## Files Created/Modified
- `src/Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs` — GetIncomeRows + interface member; CalculateTotalIncome delegates to row sum
- `src/Valt.Infra/Modules/Goals/Services/IncomeFiatProgressCalculator.cs` — GetContributingTransactionsAsync → GetIncomeRows
- `src/Valt.Infra/Modules/Goals/Services/SaveFiatProgressCalculator.cs` — merged mixed-sign RunningTotal override
- `src/Valt.Infra/Modules/Goals/Services/ReduceExpenseCategoryProgressCalculator.cs` — category-filtered GetExpenseRows override
- `src/Valt.Infra/Modules/Goals/Services/SavingsRateProgressCalculator.cs` — incremental percentage RunningTotal override
- `tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs` — +7 fixtures and SeedGoal/SeedIncome/NewReader/NewInput helpers

## Decisions Made
- **Origin-flag merge (SaveFiat/SavingsRate):** rows are tagged `(Row, IsExpense)` at concat time so running-total signs and accumulators never re-evaluate transaction predicates — satisfies the plan prohibition and keeps selection in the reader.
- **Task-order note on the Task 2 `tdd="true"` flag:** the plan itself sequenced the test matrix as Task 3, so implementation preceded fixtures; every fixture reconciles against `CalculateProgressAsync` output rather than hand-computed constants alone, preserving the TDD intent (tests prove behavior, not just compilation).
- **DatabaseTest untouched:** all four calculators already shared the 49-01 `GoalTransactionReader` instance; the plan's conditional clause required no wiring change.

## Deviations from Plan

### Gate-Interpretation Notes (no code impact)

**1. Calculator override gate glob includes the interface file**
- The `<gate>` `grep -l ... *Calculator.cs | wc -l` yields 6 (IGoalProgressCalculator.cs defines the method + 5 concrete calculators), matching the 49-01 deviation note. Filtering the interface file: exactly 5 concrete overrides — gate intent satisfied.

**2. CalculateTotalIncome grep gate floor is 3, not 2**
- The gate expects ≤2 lines matching "CalculateTotalIncome", but a working delegation has a 3-reference floor: interface declaration + implementation signature + the single Sum call. The old foreach body is gone (verified by inspection — one income predicate remains, inside GetIncomeRows). Gate intent satisfied.

**3. SaveFiat mixed-sign running totals asserted as -300/200/100**
- Task 3's prose mentioned "300/200/100 semantics," but Task 2's normative behavior spec defines `RunningTotal_i = cumulative income − cumulative expenses`, which yields −300 for the first (expense) row. The normative spec was implemented and asserted; the final row (100) reconciles with CalculatedSavings either way.

---

**Total deviations:** 3 gate-interpretation/documentation notes, 0 code deviations from plan behavior.

## Issues Encountered
- None blocking. One compile iteration on a missing `Valt.Core.Modules.Budget.Categories` using for `CategoryId` in the test file (fixed before commit).

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness
- Plan 03 (direct-DB strategies: StackBitcoin, Dca, IncomeBtc, BitcoinHodl) extends the same override point; the reader-backed machinery is fully proven and no reader changes are anticipated for those.
- Phase 50 (UI) can rely on 5 supported goal types returning rows and NetWorthBtc plus the four direct-DB types returning typed NotSupported.

## Self-Check: PASSED

- All 6 modified files verified on disk (build succeeded, 0 errors).
- Commits cec5b3e, 8667e9c, c3e6ed8 verified via git log.
- Wave-2 gate: build green; GetGoalContributingTransactionsHandlerTests 18/18; ProgressCalculatorTests 68/68; full suite 1796/1796.

---
*Phase: 49-goal-contributing-transactions-query-backend*
*Completed: 2026-10-06*
