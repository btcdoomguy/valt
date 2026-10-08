---
phase: 44-core-loan-simulation-calculator
plan: 01
subsystem: core
tags: [btc-loan, simulation, decimal, act-365, compound-interest, avalancheonia-not-involved]

requires: []

provides:
  - Pure static BTC loan simulation engine in Valt.Core.Modules.Assets.Simulation
  - Simple-interest mode that parity-matches BtcLoanDetails.CalculateAccruedInterest
  - Compound-interest mode using a daily decimal loop with fees excluded from principal
  - Monthly cost-over-time schedule bounded by month-end anchors plus closing end-date row
  - Liquidation BTC price and effective fee-inclusive APR derivation
  - Unit-test suite covering parity, compound, schedule, edge cases, and input validation

affects:
  - 45-simulator-modal-ui
  - 46-cost-over-time-schedule
  - 47-prefill-existing-btc-loan

actuals:
  tokens: 5468
  tasks: 3
  commits: 3

tech-stack:
  added: []
  patterns:
    - Pure static domain calculator with no DI/persistence/UI dependencies
    - act/365 decimal money math with 2-decimal rounding for display totals
    - Daily compounding via explicit decimal loop instead of Math.Pow
    - Monthly anchor schedule generation with end-date closure

key-files:
  created:
    - src/Valt.Core/Modules/Assets/Simulation/BtcLoanInterestMode.cs
    - src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationInput.cs
    - src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationResult.cs
    - src/Valt.Core/Modules/Assets/Simulation/LoanScheduleEntry.cs
    - src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs
    - tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs
  modified: []

key-decisions:
  - "Simple-interest formula mirrors BtcLoanDetails exactly: Math.Round(principal * apr / 365 * days, 2)"
  - "Compound mode uses a daily decimal loop over act/365 days; fees are added once to total repayment and never earn interest"
  - "Effective APR is derived from actual total repayment: ((totalRepay - principal) / principal) * 365 / days, rounded to 2 dp"
  - "Liquidation price uses total debt / (collateralBtc * liquidationLtv / 100), rounded to 2 dp"
  - "Schedule rows are emitted on month-end anchors and the final end date; duplicate month-end/end-date rows collapse to one"

patterns-established:
  - "Valt.Core static calculator: no DI, no persistence, no DateTime.Now, no UI namespace references"
  - "Money math stays in decimal; sat amounts use long; no double or Math.Pow in money paths"

requirements-completed: [SIM-03, SIM-04]

coverage:
  - id: D1
    description: "BTC loan simulation engine supports both simple and compound interest modes"
    requirement: SIM-03
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#CompoundInterest_Should_Exceed_SimpleInterest_For_Multi_Day_Loan"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#ZeroApr_Should_Produce_Zero_Interest"
        status: pass
    human_judgment: false
  - id: D2
    description: "Simple-interest mode uses the app's act/365 convention and matches existing BtcLoanDetails math byte-for-byte"
    requirement: SIM-04
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#SimpleInterest_Should_Parity_Match_BtcLoanDetails_CalculateAccruedInterest"
        status: pass
    human_judgment: false
  - id: D3
    description: "Compound-interest mode accrues daily via a pure decimal loop and produces totals equal to schedule rows"
    requirement: SIM-04
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#Final_Schedule_Row_Should_Equal_TotalRepay_For_Both_Modes"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#CompoundInterest_Should_Exceed_SimpleInterest_For_Multi_Day_Loan"
        status: pass
    human_judgment: false
  - id: D4
    description: "Calculator derives liquidation BTC price and effective fee-inclusive APR from inputs"
    requirement: SIM-04
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#LiquidationPrice_Should_Be_Calculated_From_Total_Debt_And_Collateral"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#EffectiveApr_Should_Equal_Nominal_Apr_When_No_Fees"
        status: pass
    human_judgment: false
  - id: D5
    description: "Edge cases (end <= start, same-day loan, leap-year spans, zero APR/fees, negative inputs) are covered"
    requirement: SIM-04
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#Should_Return_Zero_Interest_When_EndDate_Is_Before_Or_Equal_To_StartDate"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#LeapYearSpan_Should_Use_Calendar_Day_Count"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#Should_Throw_For_Negative_Apr"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs#Should_Throw_For_Negative_Fees"
        status: pass
    human_judgment: false

duration: 28min
completed: 2026-08-14
status: complete
---

# Phase 44: Core Loan Simulation Calculator Summary

**Pure static BTC loan simulation engine in Valt.Core with act/365 parity, daily-decimal compounding, liquidation price, effective APR, and a bounded monthly schedule.**

## Performance

- **Duration:** 28 min
- **Started:** 2026-08-14T13:26:17Z
- **Completed:** 2026-08-14T13:54:17Z
- **Tasks:** 3
- **Files modified:** 6

## Accomplishments

- Created the `Valt.Core.Modules.Assets.Simulation` namespace with the `BtcLoanInterestMode` enum, input/result records, and `LoanScheduleEntry`.
- Implemented `BtcLoanSimulationCalculator.Calculate` as a pure static method with no DI, persistence, or UI dependencies.
- Simple-interest path matches `BtcLoanDetails.CalculateAccruedInterest()` byte-for-byte, proven by a parity unit test.
- Compound-interest path uses a daily `decimal` loop with fees excluded from the compounding principal.
- Added liquidation price and effective fee-inclusive APR derivation using act/365 annualization.
- Generated a monthly schedule anchored on month-ends plus a closing end-date row; the final row's cumulative total equals the headline `TotalRepay`.
- Covered edge cases including end-date ≤ start-date, same-day loans, leap-year spans, zero APR, zero fees, and negative input validation.
- Verified the full solution builds and contains no `double` or `Math.Pow` in the new files.

## Task Commits

Each task was committed atomically:

1. **Task 1: Create domain types and simple-interest engine with parity test** - `7323413` (feat)
2. **Task 2: Add compound mode, effective APR, liquidation price, and monthly schedule** - `3fbe994` (feat)
3. **Task 3: Edge-case tests and full build verification** - `8d58376` (test)

## Files Created/Modified

- `src/Valt.Core/Modules/Assets/Simulation/BtcLoanInterestMode.cs` - Simple/Compound enum
- `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationInput.cs` - Immutable input record
- `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationResult.cs` - Result record with totals, liquidation price, effective APR, and schedule
- `src/Valt.Core/Modules/Assets/Simulation/LoanScheduleEntry.cs` - Monthly schedule row
- `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs` - Pure static calculation engine
- `tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs` - Parity, compound, schedule, APR, liquidation-price, and edge-case tests

## Decisions Made

- Followed the existing `BtcLoanDetails` act/365 convention for simple interest to keep parity byte-for-byte.
- Chose daily compounding via an explicit `decimal` loop instead of `Math.Pow` to avoid precision drift and keep totals reconcilable with schedule rows.
- Treated fees as a flat one-time cost that does not earn interest, matching `BtcLoanDetails.Fees` semantics.
- Derived effective APR from the actual total repayment rather than the nominal APR so the displayed rate reflects true cost.
- Bounded the schedule to month-end anchors plus a closing end-date row to keep row counts manageable for multi-year loans.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- Initial implementation had a local variable name collision (`totalRepay` declared in both the zero-day branch and the main path), causing a compiler error. Renamed the zero-day variable to `zeroDayTotalRepay` and committed the fix within Task 1.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 45 (Simulator Modal UI) can consume `BtcLoanSimulationCalculator.Calculate` through `BtcLoanSimulationInput`/`BtcLoanSimulationResult` without reimplementing any math.
- Phase 46 (Cost-Over-Time Schedule) can render the `Schedule` list directly.
- Phase 47 (Prefill from Existing BTC Loan) can map `AssetDTO` fields into `BtcLoanSimulationInput`.
- No blockers.

## Self-Check: PASSED

- All created files exist on disk.
- All three task commits exist in git history.
- `dotnet test --filter "FullyQualifiedName~BtcLoanSimulationCalculatorTests"` passes 15 tests.
- `dotnet build Valt.sln` succeeds.
- No `double` or `Math.Pow` found in `src/Valt.Core/Modules/Assets/Simulation/` or `tests/Valt.Tests/Domain/Assets/Simulation/`.

---
*Phase: 44-core-loan-simulation-calculator*
*Completed: 2026-08-14*
