---
phase: 45-simulator-modal-ui-inputs-results-panel
plan: 02
subsystem: testing
tags: [nunit, nsubstitute, viewmodel-tests, btc-loan-simulator]

requires:
  - phase: 45-simulator-modal-ui-inputs-results-panel
    plan: 01
    provides: BtcLoanSimulatorViewModel and modal implementation under test.

provides:
  - Automated coverage for SIM-08 (effective fee-inclusive APR) and SIM-09 (distance to liquidation + color).
  - Edge-case tests for currency changes, interest-mode toggles, and invalid input clearing.
  - Human UAT checkpoint prompt for visual/interactive sign-off.

affects:
  - 45-01 (verification closure)
  - verify-work (human sign-off required)

actuals:
  tokens: 9000
  tasks: 2
  commits: 2

tech-stack:
  added: []
  patterns:
    - "NUnit parameterized test for invalid-input scenarios using a private enum."

key-files:
  created: []
  modified:
    - tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs

key-decisions:
  - "Effective APR test uses a 365-day zero-fee simple loan so the fee-inclusive effective APR equals the nominal APR."
  - "Distance-to-liquidation tests set RatesState.BitcoinPrice before inputs so the VM recalculates with the intended price."

patterns-established:
  - "Assert formatted string behavior for color strings and distance percentages rather than raw VM decimals, matching the UI contract."

requirements-completed:
  - SIM-08
  - SIM-09

coverage:
  - id: D1
    description: "Effective APR displays the fee-inclusive rate formatted to 2 decimals."
    requirement: SIM-08
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#EffectiveApr_WithZeroFeesAnd365DayTerm_IsNominalApr"
        status: pass
    human_judgment: false
  - id: D2
    description: "Distance to liquidation shown as a signed percentage."
    requirement: SIM-09
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#DistanceToLiquidation_WhenPriceAboveLiquidation_ShowsPositivePercentage"
        status: pass
    human_judgment: false
  - id: D3
    description: "Distance-to-liquidation color is red when current BTC price is at or below liquidation price and green otherwise."
    requirement: SIM-09
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#DistanceToLiquidationColor_IsRed_WhenPriceAtOrBelowLiquidation"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#DistanceToLiquidationColor_IsGreen_WhenPriceAboveLiquidation"
        status: pass
    human_judgment: false
  - id: D4
    description: "Currency and interest-mode changes trigger fresh recalculation; invalid inputs clear results."
    requirement: SIM-02
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#CurrencyChange_TriggersRecalculate_AndUpdatesCurrencyCode"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#InterestModeToggle_CompoundProducesHigherTotalThanSimple"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#InvalidInputs_ClearResults"
        status: pass
    human_judgment: false
  - id: D5
    description: "Human visual sign-off on the BTC Loan Simulator modal."
    verification: []
    human_judgment: true
    rationale: "Layout, color rendering, and interactive behavior can only be verified by running the app and viewing the modal."

duration: 20min
completed: 2026-08-20
status: complete
---

# Phase 45 Plan 02 Summary

**SIM-08/SIM-09 automated coverage plus live-recalc edge-case tests, pending human visual sign-off**

## Performance

- **Duration:** 20 min
- **Started:** 2026-08-20T00:45:00Z
- **Completed:** 2026-08-20T01:05:00Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Added unit test proving `EffectiveAprText` formats to the nominal APR for a zero-fee 365-day simple loan.
- Added unit tests for distance-to-liquidation signed percentage and red/green color switching.
- Added edge-case tests covering currency dropdown recalculation, simple-vs-compound interest toggle, and invalid-input clearing (empty collateral, empty amount, end date ≤ start date, LTV > 100).
- All 15 `BtcLoanSimulatorViewModelTests` pass.

## Task Commits

1. **Task 1: SIM-08/SIM-09 and edge-case VM tests** - `ddb14a7` (test)
2. **Summary** - current commit (docs)

## Files Created/Modified
- `tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs` - Extended with SIM-08, SIM-09, and live-recalc edge-case tests.

## Decisions Made
- Kept all new assertions on the formatted VM strings exposed to XAML, matching the UI contract.
- Used an enum-driven parameterized test for invalid-input scenarios to keep the test file maintainable.

## Deviations from Plan

None - all test behaviors described in the plan were implemented and pass.

## Issues Encountered
- None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 45 automated verification is complete.
- Awaiting human visual sign-off on the modal before marking Phase 45 complete.

---
*Phase: 45-simulator-modal-ui-inputs-results-panel*
*Completed: 2026-08-20*
