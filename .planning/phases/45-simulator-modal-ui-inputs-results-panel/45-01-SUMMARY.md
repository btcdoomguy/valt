---
phase: 45-simulator-modal-ui-inputs-results-panel
plan: 01
subsystem: ui
tags: [avalonia, mvvm, btc-loan-simulator, live-recalculation]

requires:
  - phase: 44-core-loan-simulation-calculator
    provides: BtcLoanSimulationCalculator and input/output contracts used by the ViewModel.

provides:
  - BTC Loan Simulator chromeless modal with inputs and results panels.
  - BtcLoanSimulatorViewModel with live recalculation wired to the Phase 44 calculator.
  - ApplicationModalNames enum entry, DI registration, and Tools menu command.
  - Initial VM unit tests proving live recalculation, fiat/sats formatting, and invalid-input clearing.

affects:
  - 45-02 (APR/distance tests and human sign-off)
  - 46 (schedule visualization)
  - 47 (prefill from existing BTC-backed loans)
  - 48 (localization)

actuals:
  tokens: 22000
  tasks: 3
  commits: 2

tech-stack:
  added: []
  patterns:
    - "LeverageSimulator clone pattern: chromeless modal, CustomTitleBar, live-recalc VM, fiat+sats formatting."
    - "BtcLoanSimulationItem placeholder prefill record for Phase 47 extension."

key-files:
  created:
    - src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml
    - src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml.cs
    - src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorViewModel.cs
    - src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulationItem.cs
    - tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs
  modified:
    - src/Valt.UI/Views/ApplicationModalNames.cs
    - src/Valt.UI/Extensions.cs
    - src/Valt.UI/Views/Main/MainView.axaml
    - src/Valt.UI/Views/Main/MainViewModel.cs

key-decisions:
  - "Used English literals in XAML per approved 45-UI-SPEC; Phase 48 will add resx/pt-BR/es/Designer.cs entries."
  - "Distance to liquidation rendered as a separate color-coded percentage row, matching Leverage Simulator precedent."
  - "Sats values and conversion basis hidden when RatesState.BitcoinPrice is unavailable, per Interaction Contract."

patterns-established:
  - "BtcLoanSimulatorViewModel mirrors LeverageSimulatorViewModel shape: ObservableProperty partial methods triggering a single Recalculate()."
  - "FiatInput/BtcInput bound with typed values (FiatValue/BtcValue) and currency symbol bound from VM."
  - "All result rows computed by BtcLoanSimulationCalculator; VM only formats outputs."

requirements-completed:
  - SIM-01
  - SIM-02
  - SIM-05
  - SIM-06
  - SIM-07

coverage:
  - id: D1
    description: "BTC Loan Simulator chromeless modal with input and results panels renders per 45-UI-SPEC."
    requirement: SIM-01
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#Recalculate_WithValidInputs_ShowsTotalRepay"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#Recalculate_ProvidesBreakdownAndLiquidationPrice"
        status: pass
    human_judgment: true
    rationale: "Layout/visual fidelity can only be confirmed by running the app and viewing the modal."
  - id: D2
    description: "Live recalculation from all inputs through BtcLoanSimulationCalculator."
    requirement: SIM-02
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#Recalculate_WithValidInputs_ShowsTotalRepay"
        status: pass
    human_judgment: false
  - id: D3
    description: "Total to repay, principal, interest, and fees breakdown rows."
    requirement: SIM-05
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#Recalculate_ProvidesBreakdownAndLiquidationPrice"
        status: pass
    human_judgment: false
  - id: D4
    description: "Fiat and sats values with BTC price conversion basis and fallback when unavailable."
    requirement: SIM-06
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#Recalculate_WhenBtcPriceAvailable_ShowsSatsAndConversionBasis"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#Recalculate_WhenBtcPriceUnavailable_HidesSatsAndShowsFallback"
        status: pass
    human_judgment: false
  - id: D5
    description: "Liquidation BTC price derived from LTV and total debt."
    requirement: SIM-07
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs#Recalculate_ProvidesBreakdownAndLiquidationPrice"
        status: pass
    human_judgment: false

duration: 45min
completed: 2026-08-20
status: complete
---

# Phase 45 Plan 01 Summary

**BTC Loan Simulator modal shell, live-recalculation ViewModel, DI wiring, and first VM tests**

## Performance

- **Duration:** 45 min
- **Started:** 2026-08-20T00:00:00Z
- **Completed:** 2026-08-20T00:45:00Z
- **Tasks:** 3
- **Files modified:** 9

## Accomplishments
- Created chromeless 800×760 `BtcLoanSimulatorView` with a 300/24/* inputs/results grid, `CustomTitleBar`, and Escape-to-close.
- Built `BtcLoanSimulatorViewModel` with all loan inputs, live `Recalculate()` backed by `BtcLoanSimulationCalculator`, and fiat/sats formatting via `CurrencyDisplay` and `RatesState`.
- Registered the modal in `ApplicationModalNames`, DI factory in `Extensions.cs`, and added the "BTC Loan Simulator" Tools menu item + `MainViewModel` command.
- Added `BtcLoanSimulationItem` placeholder prefill record and `BtcLoanSimulatorViewModelTests` covering valid inputs, breakdown rows, sats display, fallback, and invalid-input clearing.

## Task Commits

1. **Task 1–3: Modal, ViewModel, DI wiring, inputs/results panel, and VM tests** - `f8fb7a2` (feat)
2. **Summary** - current commit (docs)

## Files Created/Modified
- `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml` - Chromeless modal XAML with input and results panels.
- `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml.cs` - Code-behind with Escape-to-close.
- `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorViewModel.cs` - Input observables, live recalc, and formatted result strings.
- `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulationItem.cs` - Placeholder prefill item for Phase 47.
- `tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs` - VM unit tests for SIM-01/02/05/06/07.
- `src/Valt.UI/Views/ApplicationModalNames.cs` - Added `BtcLoanSimulator = 42`.
- `src/Valt.UI/Extensions.cs` - Added `BtcLoanSimulatorViewModel` DI registration and factory case.
- `src/Valt.UI/Views/Main/MainView.axaml` - Added Tools menu item.
- `src/Valt.UI/Views/Main/MainViewModel.cs` - Added `OpenBtcLoanSimulatorCommand`.

## Decisions Made
- Followed the approved 45-UI-SPEC: English literals in XAML, localization deferred to Phase 48.
- Mirrored `LeverageSimulatorView`/`LeverageSimulatorViewModel` patterns for layout, live recalc, and result rows.
- Kept the modal ephemeral: no persistence, no CQRS commands.

## Deviations from Plan

None - plan executed as written. Minor implementation details (e.g., explicit `SymbolOnRight` VM property, separate distance-to-liquidation row) align with the approved UI-SPEC and precedent.

## Issues Encountered
- `DateOnly` required explicit `using System;` in `BtcLoanSimulationItem.cs`.
- Test build flagged missing `INotificationPublisher` using directive and NUnit analyzer expected `RatesState.Dispose()` in `[TearDown]`; both fixed.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Plan 45-02 can add APR/distance edge-case tests and request human visual sign-off.
- Phase 46 can insert a schedule `ItemsControl` below the headline results in the existing `ScrollViewer`.

---
*Phase: 45-simulator-modal-ui-inputs-results-panel*
*Completed: 2026-08-20*
