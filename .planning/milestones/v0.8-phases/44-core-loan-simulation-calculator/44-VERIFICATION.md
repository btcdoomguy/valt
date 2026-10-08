---
phase: 44-core-loan-simulation-calculator
verified: 2026-08-14T13:58:48Z
status: passed
score: 10/10 must-haves verified
behavior_unverified: 0
overrides_applied: 0
gaps: []
behavior_unverified_items: []
human_verification: []
---

# Phase 44: Core Loan Simulation Calculator Verification Report

**Phase Goal:** Deliver the pure, fully-tested Core loan-simulation calculator. This is the foundation for the entire v0.8 BTC Loan Simulator milestone; every downstream phase (UI, schedule rendering, prefill, MCP) consumes this engine without reimplementing math.

**Verified:** 2026-08-14T13:58:48Z

**Status:** passed

**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| #   | Truth   | Status     | Evidence       |
| --- | ------- | ---------- | -------------- |
| 1   | `BtcLoanSimulationCalculator` is a pure static class in `Valt.Core` with no DI, no persistence, and no UI dependency. | VERIFIED | Class is `public static class BtcLoanSimulationCalculator` in `Valt.Core.Modules.Assets.Simulation`. Only `using System; using System.Collections.Generic;`. No references to `LiteDB`, `Valt.Infra`, `Valt.UI`, `ICommandDispatcher`, `IQueryDispatcher`, `IDisposable`, `DateTime.Now`, or Avalonia namespaces. |
| 2   | Simple-interest mode produces the same 2-decimal result as `BtcLoanDetails.CalculateAccruedInterest()` for the same principal, APR, and day count (act/365). | VERIFIED | `SimpleInterest_Should_Parity_Match_BtcLoanDetails_CalculateAccruedInterest` passes. Both use `Math.Round(principal * apr / 365 * days, 2)`. The test pins a `LoanStateSnapshot` to `today.AddDays(-100)` and asserts equality. |
| 3   | Compound-interest mode uses a daily `decimal` loop over act/365 days; accrued interest is added to the principal balance for the next day, and fees never earn interest. | VERIFIED | `CalculateCompound` loops `for (var i = 1; i <= days; i++)`, computes `dailyInterest = runningPrincipal * input.Apr / 365m`, adds it to `accruedInterest` and `runningPrincipal`. `fees` are added only once to `TotalRepay`, never to `runningPrincipal`. `CompoundInterest_Should_Exceed_SimpleInterest_For_Multi_Day_Loan` passes. |
| 4   | The calculator returns a `BtcLoanSimulationResult` with total repay, principal, interest, fees, liquidation BTC price, effective fee-inclusive APR, and a monthly schedule. | VERIFIED | `BtcLoanSimulationResult` record contains all required fields; `Calculate` returns a fully populated instance in both simple and compound paths. |
| 5   | Schedule rows are one per calendar month plus a closing row on the end date, keeping row counts bounded. | VERIFIED | `CalculateSimple`/`CalculateCompound` emit rows whenever `currentDate == endDate || currentDate == GetMonthEnd(currentDate)`. Duplicate month-end/end-date rows collapse into a single closing row. A 10-year loan produces at most ~120 monthly rows + 1 closing row. |
| 6   | The final schedule row's cumulative total exactly equals the headline `TotalRepay` figure. | VERIFIED | `Final_Schedule_Row_Should_Equal_TotalRepay_For_Both_Modes` asserts `simpleResult.Schedule[^1].CumulativeTotal == simpleResult.TotalRepay` and the same for compound. Both pass. |
| 7   | Effective APR is `((totalRepay - principal) / principal) * 365 / days` rounded to 2 dp. | VERIFIED | `CalculateEffectiveApr` implements exactly `Math.Round(totalCost / principal * 365m / days, 2)` where `totalCost = totalRepay - principal`. `EffectiveApr_Should_Equal_Nominal_Apr_When_No_Fees` and `ZeroFees_Should_Make_EffectiveApr_Equal_Nominal_For_Simple_Mode_Over_365_Days` pass. |
| 8   | Liquidation BTC price is `totalDebt / (collateralBtc * liquidationLtv)` and uses 2 dp rounding. | VERIFIED | `CalculateLiquidationPrice` does `Math.Round(totalDebt / (collateralBtc * input.LiquidationLtv / 100m), 2)` and guards zero collateral/LTV. `LiquidationPrice_Should_Be_Calculated_From_Total_Debt_And_Collateral` passes with expected `31_250m`. |
| 9   | Edge cases end ≤ start date, same-day loan, and leap-year spans are covered by unit tests with no database or DI. | VERIFIED | `Should_Return_Zero_Interest_When_EndDate_Is_Before_Or_Equal_To_StartDate`, `LeapYearSpan_Should_Use_Calendar_Day_Count`, `ZeroApr_Should_Produce_Zero_Interest`, `ZeroFees_Should_Make_EffectiveApr_Equal_Nominal_For_Simple_Mode_Over_365_Days`, `Should_Throw_For_Negative_Apr`, and `Should_Throw_For_Negative_Fees` all pass. Tests inherit only from NUnit `TestFixture`; no `DatabaseTest` or `IntegrationTest` base. |
| 10   | All money math uses `decimal`; all sat amounts use `long`. No `double` or `Math.Pow` appears in money paths. | VERIFIED | `grep -RE "Math\.Pow|\\bdouble\\b" src/Valt.Core/Modules/Assets/Simulation/ tests/Valt.Tests/Domain/Assets/Simulation/` returned no matches. `CollateralSats` is `long`; all interest/principal/fee fields are `decimal`. |

**Score:** 10/10 truths verified (0 present-but-behavior-unverified)

### Required Artifacts

| Artifact | Expected    | Status | Details |
| -------- | ----------- | ------ | ------- |
| `src/Valt.Core/Modules/Assets/Simulation/BtcLoanInterestMode.cs` | Simple/Compound enum used by the calculator and downstream UI. | VERIFIED | File exists; enum has `Simple` and `Compound` values. |
| `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationInput.cs` | Immutable input record carrying collateral, principal, APR, liquidation LTV, fees, dates, and mode. | VERIFIED | File exists; sealed record with all required init-only properties. |
| `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationResult.cs` | Result record with total repay, breakdown, liquidation price, effective APR, and schedule. | VERIFIED | File exists; sealed record with all required init-only properties. |
| `src/Valt.Core/Modules/Assets/Simulation/LoanScheduleEntry.cs` | Monthly schedule row with date, accrued interest, and cumulative total. | VERIFIED | File exists; positional record with `DateOnly Date`, `decimal AccruedInterest`, `decimal CumulativeTotal`. |
| `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs` | Pure static calculation engine. | VERIFIED | File exists; public static class with single `Calculate` entry point. |
| `tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs` | Parity, compound, edge-case, and liquidation/APR unit tests. | VERIFIED | File exists; 15 tests all pass. |

### Key Link Verification

| From | To  | Via | Status | Details |
| ---- | --- | --- | ------ | ------- |
| `tests/Valt.Tests/Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs` | `BtcLoanDetails.CalculateAccruedInterest` | Parity test constructs a `LoanStateSnapshot` pinned to `today.AddDays(-100)` and compares `details.CalculateAccruedInterest()` with `BtcLoanSimulationCalculator.Calculate(...).Interest`. | WIRED | `SimpleInterest_Should_Parity_Match_BtcLoanDetails_CalculateAccruedInterest` invokes `details.CalculateAccruedInterest()` at line 58 and asserts equality with the calculator result. |
| `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs` | `BtcLoanSimulationResult.Schedule` | Monthly-anchor + end-date row generation inside the same calculation pass. | WIRED | `CalculateSimple` and `CalculateCompound` build `List<LoanScheduleEntry>` locally and assign it to `BtcLoanSimulationResult.Schedule` before returning. |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
| -------- | ------------- | ------ | ------------------ | ------ |
| `BtcLoanSimulationCalculator` | `BtcLoanSimulationResult` fields | Caller-supplied `BtcLoanSimulationInput` plus pure arithmetic | Yes — output is computed directly from input values; no static/hardcoded fallback | FLOWING |
| `BtcLoanSimulationCalculator` | `Schedule` list | Generated from `StartDate`/`EndDate` and month-end anchors | Yes — rows are produced by the daily/monthly loop | FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Loan simulation test suite passes | `dotnet test --filter "FullyQualifiedName~BtcLoanSimulationCalculatorTests"` | `Passed!  - Failed: 0, Passed: 15, Skipped: 0, Total: 15` | PASS |
| Solution builds with no warnings | `dotnet build Valt.sln` | `Build succeeded. 0 Warning(s), 0 Error(s)` | PASS |
| No `double` or `Math.Pow` in simulation code/tests | `grep -RE "Math\.Pow|\\bdouble\\b" src/Valt.Core/Modules/Assets/Simulation/ tests/Valt.Tests/Domain/Assets/Simulation/` | No matches | PASS |

### Probe Execution

No probes declared for this phase.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ---------- | ----------- | ------ | -------- |
| SIM-03 | 44-01-PLAN.md | User can choose simple or compound interest mode | Satisfied | `BtcLoanInterestMode` enum; `CompoundInterest_Should_Exceed_SimpleInterest_For_Multi_Day_Loan` and `ZeroApr_Should_Produce_Zero_Interest` pass for both modes. |
| SIM-04 | 44-01-PLAN.md | Simple interest mode uses the app's existing act/365 convention (parity with current loan math); compound mode uses daily accrual | Satisfied | Parity test with `BtcLoanDetails.CalculateAccruedInterest` passes; compound path uses daily `decimal` loop. |

### Anti-Patterns Found

No debt markers (`TODO`, `FIXME`, `XXX`, `HACK`, `placeholder`, etc.) found in the new files. No `return null`/`return {}`/`return []` stubs, no hardcoded empty collections, and no console-log-only implementations.

### Human Verification Required

None. The phase is a pure Core engine with automated unit-test coverage and a passing solution build.

### Gaps Summary

No gaps found. All must-have truths are verified, all artifacts are present and substantive, all key links are wired, and the solution builds cleanly.

---
_Verified: 2026-08-14T13:58:48Z_
_Verifier: the agent (gsd-verifier)_
