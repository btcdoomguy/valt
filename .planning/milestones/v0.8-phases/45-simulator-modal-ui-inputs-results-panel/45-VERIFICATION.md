---
phase: 45-simulator-modal-ui-inputs-results-panel
verified: 2026-08-21T10:15:00Z
status: passed
score: 9/9 must-haves verified
behavior_unverified: 0
overrides_applied: 0
---

# Phase 45: Simulator Modal UI (Inputs + Results Panel) Verification Report

**Phase Goal:** Users can open the BTC Loan Simulator from the Tools menu, enter loan parameters, and immediately see the full cost and risk picture in fiat and sats
**Verified:** 2026-08-21T10:15:00Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | The BTC Loan Simulator modal exists as a chromeless 800×760 `ValtBaseWindow` with `CustomTitleBar` and a 300/24/* input/results grid. | VERIFIED | `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml` declares `WindowDecorations="None"`, `Min/MaxWidth="800"`, `Min/MaxHeight="760"`, uses `CustomTitleBar`, and defines `Grid.ColumnDefinitions="300,24,*"`. |
| 2 | The modal is registered in `ApplicationModalNames` and the DI factory, and a Tools menu item plus `MainViewModel` command open it. | VERIFIED | `ApplicationModalNames.BtcLoanSimulator = 42`; `Extensions.cs` registers `BtcLoanSimulatorViewModel` and factory case; `MainView.axaml` adds `MenuItem Header="BTC Loan Simulator" Command="{Binding OpenBtcLoanSimulatorCommand}"`; `MainViewModel.OpenBtcLoanSimulator()` calls `_modalLauncher.ShowAsync(ApplicationModalNames.BtcLoanSimulator, Window!)`. |
| 3 | `BtcLoanSimulatorViewModel` accepts all loan inputs, builds a `BtcLoanSimulationInput`, and calls `BtcLoanSimulationCalculator.Calculate`. | VERIFIED | `BtcLoanSimulatorViewModel.cs` exposes `CollateralBtcValue`, `AmountTakenFiatValue`, `LiquidationLtvText`, `AprText`, `FeesFiatValue`, `StartDate`, `EndDate`, `IsSimple`; `Recalculate()` validates, constructs `BtcLoanSimulationInput`, and invokes `BtcLoanSimulationCalculator.Calculate`. |
| 4 | Results recalculate live as any input changes, mirroring the Leverage Simulator behavior. | VERIFIED | Every `[ObservableProperty]` input has a `partial void OnXChanged(...)` handler that calls `Recalculate()`; `CurrencyChange_TriggersRecalculate_AndUpdatesCurrencyCode` test passes. |
| 5 | Total to repay, principal, interest, fees, liquidation BTC price, and effective APR are displayed in the results panel. | VERIFIED | `BtcLoanSimulatorView.axaml` binds `TotalRepayFiat`, `PrincipalFiat`, `InterestFiat`, `FeesFiat`, `LiquidationPriceFiat`, `EffectiveAprText`; tests `Recalculate_WithValidInputs_ShowsTotalRepay`, `Recalculate_ProvidesBreakdownAndLiquidationPrice`, and `EffectiveApr_WithZeroFeesAnd365DayTerm_IsNominalApr` pass. |
| 6 | Result values are shown in fiat and sats at the current live BTC price from `RatesState`, with a conversion-basis label, and sats are hidden when no price is available. | VERIFIED | `FormatResults` uses `RatesState.BitcoinPrice` and `FiatRates` to compute `btcPriceInCurrency`, formats sats via `FormatSats`, sets `IsBtcPriceAvailable`, and shows fallback copy; tests `Recalculate_WhenBtcPriceAvailable_ShowsSatsAndConversionBasis` and `Recalculate_WhenBtcPriceUnavailable_HidesSatsAndShowsFallback` pass. |
| 7 | Distance to liquidation versus the current live BTC price is displayed, color-coded, and the display currency can be switched. | VERIFIED | `DistanceToLiquidation` and `DistanceToLiquidationColor` are computed from `price` vs `result.LiquidationPrice`; bound with `StringToColorBrushConverter`; tests `DistanceToLiquidation_WhenPriceAboveLiquidation_ShowsPositivePercentage`, red/green color, and `CurrencyChange_TriggersRecalculate_AndUpdatesCurrencyCode` pass. |
| 8 | Invalid inputs, currency changes, and interest-mode toggles clear or update results as expected. | VERIFIED | `InvalidInputs_ClearResults` covers empty collateral, empty amount, end date ≤ start date, and LTV > 100; `InterestModeToggle_CompoundProducesHigherTotalThanSimple` confirms compound > simple for multi-day loans. |
| 9 | A human verifies the modal visually before the phase is considered complete. | VERIFIED | `45-UAT.md` documents the interactive visual sign-off with 10/10 passed, including modal open/close, live recalc, color switching, currency change, and invalid-input clearing. |

**Score:** 9/9 truths verified (0 present-but-behavior-unverified)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml` | Chromeless modal XAML with input and results panels | VERIFIED | 257 lines; all required inputs and result rows present; no hardcoded hex brushes. |
| `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorViewModel.cs` | Input observables, live `Recalculate`, formatted result strings | VERIFIED | 340 lines; calls Core calculator; formats fiat/sats/distance/APR; handles invalid inputs. |
| `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulationItem.cs` | Placeholder prefill list item for Phase 47 | VERIFIED | Exposes `DisplayName`, `IsNewSimulation`, and Phase 47 fields. |
| `tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs` | VM unit tests for SIM-01/02/05/06/07/08/09 | VERIFIED | 15 tests, all passing. |
| `src/Valt.UI/Views/ApplicationModalNames.cs` | `BtcLoanSimulator` enum member | VERIFIED | `BtcLoanSimulator = 42`. |
| `src/Valt.UI/Extensions.cs` | DI registration and factory case | VERIFIED | `services.AddTransient<BtcLoanSimulatorViewModel>()` and factory case return `BtcLoanSimulatorView`. |
| `src/Valt.UI/Views/Main/MainView.axaml` | Tools menu entry | VERIFIED | MenuItem bound to `OpenBtcLoanSimulatorCommand`. |
| `src/Valt.UI/Views/Main/MainViewModel.cs` | `OpenBtcLoanSimulatorCommand` | VERIFIED | `[RelayCommand] private async Task OpenBtcLoanSimulator()`. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `BtcLoanSimulatorViewModel.Recalculate` | `BtcLoanSimulationCalculator.Calculate` | `BtcLoanSimulationInput` built from VM observables | WIRED | `Recalculate` → `TryBuildInput` → `BtcLoanSimulationCalculator.Calculate(input)`. |
| `MainViewModel.OpenBtcLoanSimulatorCommand` | `BtcLoanSimulatorView` | `ApplicationModalNames.BtcLoanSimulator` + `IModalFactory` | WIRED | Command calls `_modalLauncher.ShowAsync(ApplicationModalNames.BtcLoanSimulator, Window!)`; factory case in `Extensions.cs` creates `BtcLoanSimulatorView`. |
| `RatesState` | `BtcLoanSimulatorViewModel` | `BitcoinPrice` + `FiatRates` for sats conversion and basis label | WIRED | `GetBtcPriceInCurrency()` reads `_ratesState.BitcoinPrice` and `_ratesState.FiatRates[currencyCode]`. |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `BtcLoanSimulatorViewModel` | `TotalRepayFiat`, `TotalRepaySats`, etc. | `BtcLoanSimulationCalculator.Calculate` via `BtcLoanSimulationInput` | Yes — uses real parsed user inputs and Phase 44 parity-tested math. | FLOWING |
| `BtcLoanSimulatorViewModel` | `TotalRepaySats`, conversion basis | `RatesState.BitcoinPrice` × `RatesState.FiatRates[CurrencyCode]` | Yes — live price state from background jobs. | FLOWING |
| `BtcLoanSimulatorViewModel` | `DistanceToLiquidationColor` | Computed from `result.LiquidationPrice` vs live BTC price | Yes — derived from live price and calculator output. | FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Solution builds cleanly | `dotnet build Valt.sln --nologo` | Build succeeded, 0 warnings, 0 errors | PASS |
| Targeted VM tests pass | `dotnet test --filter "FullyQualifiedName~BtcLoanSimulatorViewModelTests" --nologo` | 15 passed, 0 failed | PASS |
| Full regression suite passes | `dotnet test --nologo` | 1746 passed, 0 failed, 0 skipped | PASS |

### Probe Execution

No phase-declared probe scripts; verification relies on build and targeted tests instead.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| SIM-01 | 45-01-PLAN | User can input loan parameters: collateral, amount taken, liquidation LTV, start date, interest rate, fees, end date | SATISFIED | All input controls bound in `BtcLoanSimulatorView.axaml`; VM accepts and validates them. |
| SIM-02 | 45-01-PLAN | Results recalculate live as inputs change | SATISFIED | `OnXChanged` partial methods call `Recalculate`; tests verify currency and interest-mode recalc. |
| SIM-03 | Phase 44 | Simple or compound interest mode selection | Complete (Phase 44) | `IsSimple` toggle bound in XAML; passed through `BtcLoanSimulationInput.InterestMode`. |
| SIM-04 | Phase 44 | Simple interest act/365 parity; compound daily accrual | Complete (Phase 44) | `BtcLoanSimulationCalculator` shipped in Phase 44 with parity tests. |
| SIM-05 | 45-01-PLAN | Total to repay + interest/fees breakdown shown | SATISFIED | Result rows for total, principal, interest, fees bound and tested. |
| SIM-06 | 45-01-PLAN | Fiat and sats values with current BTC price conversion basis | SATISFIED | `FormatResults` computes sats and basis; tests cover available/unavailable price. |
| SIM-07 | 45-01-PLAN | Liquidation BTC price derived from LTV and total debt | SATISFIED | `LiquidationPriceFiat` bound and test asserts value contains "31". |
| SIM-08 | 45-02-PLAN | Effective fee-inclusive APR displayed | SATISFIED | `EffectiveAprText` test asserts "12.00%". |
| SIM-09 | 45-02-PLAN | Distance to liquidation vs current BTC price displayed | SATISFIED | Distance string and color tests pass. |

No orphaned requirements: `REQUIREMENTS.md` maps SIM-01/02/05-09 to Phase 45; SIM-03/04 are completed in Phase 44.

### Anti-Patterns Found

No blockers, stubs, unresolved debt markers, or hardcoded-empty data paths found in the new files. The only `return null` statements are conditional fallbacks in `GetBtcPriceInCurrency()` when the live BTC price or fiat rate is unavailable.

### Human Verification Required

None remaining. The interactive visual sign-off is documented in `45-UAT.md` (10/10 passed) and was completed before this verification.

### Gaps Summary

No gaps found. All Phase 45 must-haves, success criteria, and requirements are satisfied by the implementation.

---
_Verified: 2026-08-21T10:15:00Z_
_Verifier: the agent (gsd-verifier)_
