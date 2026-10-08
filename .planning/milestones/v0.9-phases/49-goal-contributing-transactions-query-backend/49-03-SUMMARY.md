---
phase: 49-goal-contributing-transactions-query-backend
plan: 03
subsystem: api
tags: [cqrs, goals, litedb, dotnet, query, sats]

requires:
  - phase: 49-01
    provides: GetGoalContributingTransactionsQuery plumbing, GoalContributionRow, GoalContributingTransactionsService, reconciling test harness
  - phase: 49-02
    provides: reader-backed override pattern, 5 of 9 transaction-based calculators exposing rows

provides:
  - GetContributingTransactionsAsync overrides on the four direct-database calculators — StackBitcoin, IncomeBtc, Dca, BitcoinHodl (9 of 10 calculators supported; NetWorthBtc alone keeps the NotSupported default)
  - StackBitcoin four-bucket signed-sats rows (+purchase, +income, −sale, −expense) with cumulative net-sats RunningTotal reconciling with CalculatedSats
  - IncomeBtc native-sats rows reconciling with CalculatedSats
  - Dca count-unit rows (Contribution 1, RunningTotal = cumulative purchase count) reconciling with CalculatedPurchaseCount
  - BitcoinHodl sold-sats rows reconciling with CalculatedSoldSats
  - GoalContributingTransactionsCurrency helper — main-fiat-currency resolution from the persisted settings collection for ILocalDatabase-only calculators (Q3 main-currency rule)
  - 7 new fixtures (25 total in the handler test file); full suite 1803/1803

affects: [50-goal-contributing-transactions-ui, 51-goal-mcp-localization]

actuals:
  tokens: 7400
  tasks: 3
  commits: 5

tech-stack:
  added: []
  patterns:
    - "Direct-DB overrides mirror their CalculateProgressAsync range scan and bucket predicates verbatim — selection stays beside the progress math (no reader dependency)"
    - "BtcValue is a magnitude type: SatsAmount carries Math.Abs(signed sats); the natural sign lives on Contribution/RunningTotal"
    - "Main fiat currency read straight from the settings collection (CurrencySettings.MainFiatCurrency key, USD default) when only ILocalDatabase is available"

key-files:
  created:
    - src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsCurrency.cs
  modified:
    - src/Valt.Infra/Modules/Goals/Services/StackBitcoinProgressCalculator.cs
    - src/Valt.Infra/Modules/Goals/Services/IncomeBtcProgressCalculator.cs
    - src/Valt.Infra/Modules/Goals/Services/DcaProgressCalculator.cs
    - src/Valt.Infra/Modules/Goals/Services/BitcoinHodlProgressCalculator.cs
    - tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs

key-decisions:
  - "BtcValue cannot represent negative sats (ctor throws InvalidBtcValueException), so sale/expense rows carry SatsAmount as the absolute magnitude — the sign is expressed by Contribution/RunningTotal; this matches the reader's existing expense-row shape (ParseSats(Math.Abs(...)))"
  - "Main currency resolved via the persisted settings collection rather than a new CurrencySettings constructor dependency, keeping the four calculators ILocalDatabase-only exactly as wired in 49-01 (DatabaseTest unchanged)"
  - "RED/GREEN TDD honored per task: failing reconciliation tests committed before each implementation pair (unlike 49-02's plan-order note)"

requirements-completed: [GOL-05, GOL-06, GOL-07]

coverage:
  - id: D1
    description: "StackBitcoin rows expose the four verbatim buckets as signed rows (+sats purchases, +sats income, −sats sales, −sats expenses) with cumulative net-sats RunningTotal whose final row equals StackBitcoinGoalType.CalculatedSats"
    requirement: GOL-06
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#StackBitcoin_FourBuckets_NetSats_Reconcile"
        status: pass
    human_judgment: false
  - id: D2
    description: "IncomeBtc rows contain only Bitcoin-type positive-FromSatAmount transactions with native sats; final RunningTotal equals IncomeBtcGoalType.CalculatedSats"
    requirement: GOL-07
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#IncomeBtc_NativeSats_Reconciles"
        status: pass
    human_judgment: false
  - id: D3
    description: "Dca rows contain one row per FiatToBitcoin purchase in range with Contribution 1 and RunningTotal as cumulative purchase count; final equals DcaGoalType.CalculatedPurchaseCount (Q2 count unit)"
    requirement: GOL-06
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#Dca_CountRunningTotal_Reconciles"
        status: pass
    human_judgment: false
  - id: D4
    description: "BitcoinHodl rows contain only BitcoinToFiat sales (FromSatAmount < 0) with sold-sats Contribution; result is Supported (never NotSupported); final RunningTotal equals BitcoinHodlGoalType.CalculatedSoldSats"
    requirement: GOL-07
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#BitcoinHodl_SoldSats_Reconciles_AndIsSupported"
        status: pass
    human_judgment: false
  - id: D5
    description: "Sats-only rows (direct Bitcoin income/expense) carry FiatAmount FiatValue.Zero and FiatCurrencyCode equal to the main currency (Q3) while SatsAmount carries the native sats magnitude"
    requirement: GOL-05
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#SatsOnlyRows_CarryZeroFiat_AndMainCurrency"
        status: pass
    human_judgment: false
  - id: D6
    description: "All nine transaction-based calculators override GetContributingTransactionsAsync; only NetWorthBtcProgressCalculator retains the interface-default NotSupported fallback"
    requirement: GOL-07
    verification:
      - kind: unit
        ref: "grep gates: 9 concrete calculator overrides; NetWorthBtc concrete override count 0"
        status: pass
    human_judgment: false
  - id: D7
    description: "Equal-or-touching dates merge deterministically: same-day rows order by Transaction.Id ascending in StackBitcoin and SaveFiat with matching running-total sequences across two dispatches"
    requirement: GOL-07
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#SameDayRows_StableAcrossStrategies"
        status: pass
    human_judgment: false
  - id: D8
    description: "Set behavior on the direct-DB path: inserting an extra purchase lands it in chronological position and moves the final total by exactly its sats; removal reverts"
    requirement: GOL-06
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs#AddRemove_DirectDb"
        status: pass
    human_judgment: false
  - id: D9
    description: "Phase gate: full test suite green with no regressions in any existing module (1803/1803)"
    requirement: GOL-05
    verification:
      - kind: integration
        ref: "dotnet test full suite"
        status: pass
    human_judgment: false

duration: 8min
completed: 2026-10-06
status: complete
---

# Phase 49 Plan 03: Direct-DB Calculator Overrides (StackBitcoin, IncomeBtc, Dca, BitcoinHodl) Summary

**The four direct-database calculators now expose signed, reconciling contributing-transaction rows — closing the strategy matrix at 9 of 10 supported types with NetWorthBtc the sole NotSupported fallback**

## Performance

- **Duration:** ~8 min
- **Completed:** 2026-10-06
- **Tasks:** 3
- **Files modified:** 6 (519 insertions, 0 deletions)

## Accomplishments
- All four direct-DB overrides implemented by mirroring each calculator's existing `_localDatabase.GetTransactions()` range scan and bucket predicates verbatim — selection stays beside the progress math, so rows cannot drift (GOL-07 drift-proofing by construction, no reader dependency).
- StackBitcoin emits the four buckets as signed rows: +ToSatAmount purchases, +FromSatAmount direct income, −|FromSatAmount| sales and direct expenses, with cumulative net-sats RunningTotal whose final row equals `CalculatedSats`.
- IncomeBtc emits native-sats rows (`Bitcoin && FromSatAmount > 0`) reconciling with `CalculatedSats`; Dca emits one row per `FiatToBitcoin` purchase with count-unit RunningTotal reconciling with `CalculatedPurchaseCount`; BitcoinHodl emits sold-sats rows (`BitcoinToFiat && FromSatAmount < 0`) reconciling with `CalculatedSoldSats` — Supported, never the NotSupported fallback.
- New `GoalContributingTransactionsCurrency` helper resolves the main fiat currency from the persisted settings collection (`CurrencySettings.MainFiatCurrency`, USD default) so the Q3 sats-only-row contract (zero fiat + main currency) holds without adding constructor dependencies.
- 7 new fixtures (25 total): four per-strategy reconciliation fixtures, the Q3 sats-only-row shape probe, cross-strategy same-day ordering determinism (run twice per strategy), and direct-DB add/remove.
- **Full-suite gate: 1803/1803 green** (was 1796; +7). The phase-49 backend is complete: every transaction-based strategy reconciles and the strategy matrix is closed.

## Task Commits

1. **Task 1 RED:** failing tests for StackBitcoin/IncomeBtc overrides — `b2c1b02` (test)
2. **Task 1 GREEN:** StackBitcoin + IncomeBtc overrides + currency helper — `dd566a8` (feat)
3. **Task 2 RED:** failing tests for Dca/BitcoinHodl overrides — `cd8aa70` (test)
4. **Task 2 GREEN:** Dca + BitcoinHodl overrides — `a6fa017` (feat)
5. **Task 3:** cross-cutting fixtures (Q3 row shape, same-day ordering, direct-DB add/remove) — `c50e648` (test)

## Files Created/Modified
- `src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsCurrency.cs` — main-currency + from-account currency resolution for ILocalDatabase-only calculators (created)
- `src/Valt.Infra/Modules/Goals/Services/StackBitcoinProgressCalculator.cs` — four-bucket signed-sats override with fiat-leg resolution
- `src/Valt.Infra/Modules/Goals/Services/IncomeBtcProgressCalculator.cs` — native-sats income override
- `src/Valt.Infra/Modules/Goals/Services/DcaProgressCalculator.cs` — count-unit purchase rows override
- `src/Valt.Infra/Modules/Goals/Services/BitcoinHodlProgressCalculator.cs` — sold-sats rows override (Supported, never NotSupported)
- `tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs` — +7 fixtures (25 total) and SeedBtcAccount/SeedTransaction helpers

## Decisions Made
- **BtcValue is a magnitude type** (its ctor throws on negative sats), so `SatsAmount` on sale/expense rows carries `Math.Abs(signedSats)`; the natural sign lives on `Contribution`/`RunningTotal`. This matches the reader's existing expense-row shape and keeps `BtcValue`'s invariant intact.
- **Main currency via settings collection, not a new dependency**: the four calculators stay `ILocalDatabase`-only exactly as wired in 49-01; `DatabaseTest` needed no change.
- **True RED/GREEN per task** this wave: each implementation commit is preceded by a failing-test commit, satisfying the plan's `tdd="true"` flags directly.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] BtcValue cannot carry negative sats**
- **Found during:** Task 1 (GREEN test run)
- **Issue:** The plan specified `SatsAmount = BtcValue of the same signed sats` (and, for BitcoinHodl, "native signed sats with natural sign"), but `BtcValue`'s constructor throws `InvalidBtcValueException` for any negative value — negative SatsAmount is infeasible without changing a Core value object used across the entire app.
- **Fix:** `SatsAmount` carries the absolute sats magnitude on debit rows; the sign is expressed by `Contribution`/`RunningTotal` (and visible in the UI as RunningTotal deltas). Identical to the reader's existing expense-row shape (`ParseSats(Math.Abs(...))`).
- **Files modified:** StackBitcoinProgressCalculator.cs, BitcoinHodlProgressCalculator.cs
- **Verification:** All 25 handler fixtures pass; full suite green.
- **Committed in:** dd566a8, a6fa017

### Gate-Interpretation Notes (no code impact)

**2. Override-count gate glob includes the interface file**
- The `<gate>` `grep -l ... *Calculator.cs | wc -l` also matches `IGoalProgressCalculator.cs` (it defines the method), yielding 8 after Task 1 (expected 7) and 10 after Task 2 (expected 9). Filtering the interface file: exactly 7 then 9 concrete overrides; NetWorthBtc concrete count 0. Gate intent satisfied — same note as 49-01/49-02.

**3. DatabaseTest.cs listed in frontmatter files_modified but unchanged**
- The four calculators were already wired with `ILocalDatabase` only in 49-01, and the currency helper reads the settings collection directly, so no test-harness wiring change was required (mirrors the 49-02 outcome).

---

**Total deviations:** 1 auto-fixed blocking issue + 2 documentation/gate notes
**Impact on plan:** The BtcValue fix was forced by a Core invariant; row semantics (signed contributions, reconciled running totals, Q3 zero-fiat contract) are exactly as planned. No scope creep.

## TDD Gate Compliance
- RED gate commits: `b2c1b02` (Task 1), `cd8aa70` (Task 2) — both confirmed failing before implementation.
- GREEN gate commits: `dd566a8` (Task 1), `a6fa017` (Task 2) — all tests pass after implementation.
- No REFACTOR commit needed (no cleanup beyond the deviation fix, committed within GREEN).

## Issues Encountered
- None beyond the deviations above.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness
- Phase 50 (UI) can bind to `GetGoalContributingTransactionsQuery` for all nine transaction-based goal types (rows carry per-strategy RunningTotal units: fiat, percentage, sats, count) and show a "not available" state only for NetWorthBtc.
- Phase 51 (MCP + localization) consumes the same query; no backend changes anticipated.

## Self-Check: PASSED

- All 6 created/modified source artifacts verified on disk (build succeeded, 0 errors).
- Commits b2c1b02, dd566a8, cd8aa70, a6fa017, c50e648 verified via git log.
- Wave-3 gates: 25/25 handler fixtures; 9 concrete calculator overrides; NetWorthBtc override count 0; full suite 1803/1803.

---
*Phase: 49-goal-contributing-transactions-query-backend*
*Completed: 2026-10-06*
