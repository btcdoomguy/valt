---
phase: 45-simulator-modal-ui-inputs-results-panel
reviewed: 2026-08-21T14:20:00Z
depth: standard
files_reviewed: 9
files_reviewed_list:
  - src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml
  - src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml.cs
  - src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorViewModel.cs
  - src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulationItem.cs
  - tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs
  - src/Valt.UI/Views/ApplicationModalNames.cs
  - src/Valt.UI/Extensions.cs
  - src/Valt.UI/Views/Main/MainView.axaml
  - src/Valt.UI/Views/Main/MainViewModel.cs
findings:
  critical: 0
  warning: 2
  info: 2
  total: 4
status: issues_found
---

# Phase 45: Code Review Report

**Reviewed:** 2026-08-21T14:20:00Z
**Depth:** standard
**Files Reviewed:** 9
**Status:** issues_found

## Summary

Phase 45 delivers the BTC Loan Simulator modal UI, ViewModel, DI wiring, Tools menu command, and VM unit tests. The implementation closely follows the existing Leverage Simulator pattern and correctly wires into the Phase 44 `BtcLoanSimulationCalculator`. Build and targeted tests pass (15/15).

The review surfaced two maintainability warnings in the test file related to culture-dependent string assertions and ad-hoc fiat-string parsing, plus two minor info-level items about defensive catch-all blocks and a misleading helper name. No security vulnerabilities or crash/data-loss defects were found.

## Warnings

### WR-01: Conversion-basis assertion depends on host UI culture

**File:** `tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs:123`
**Issue:** `ConversionBasis` is formatted with `CultureInfo.CurrentUICulture` in the ViewModel, but the test only checks for the en-US group separator:

```csharp
Assert.That(viewModel.ConversionBasis, Does.Contain("100,000").Or.Contains("100000"));
```

If the test host's `CurrentUICulture` uses a different grouping separator (e.g., German "100.000,00" or French "100 000,00"), the assertion fails even though the VM behavior is correct. The test currently passes because the local environment has en-US UI culture, but it is not deterministic across developer machines or CI.

**Fix:** Assert against the formatted price using the same culture the VM uses:

```csharp
var expectedPrice = (100_000m).ToString("N2", CultureInfo.CurrentUICulture);
Assert.That(viewModel.ConversionBasis, Does.Contain(expectedPrice));
```

Alternatively, use a culture-aware regex that accepts common group separators.

### WR-02: Ad-hoc fiat-string parsing in interest-mode test

**File:** `tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs:325-326`
**Issue:** The compound-vs-simple test parses formatted fiat strings by stripping `$` and `,` and then parsing with `CultureInfo.InvariantCulture`:

```csharp
var simpleValue = decimal.Parse(simpleTotal.Replace("$", "").Replace(",", "").Trim(), CultureInfo.InvariantCulture);
var compoundValue = decimal.Parse(viewModel.TotalRepayFiat.Replace("$", "").Replace(",", "").Trim(), CultureInfo.InvariantCulture);
```

This hard-codes USD/en-US formatting assumptions and couples the test to the exact output shape of `CurrencyDisplay.FormatFiat`. It will silently break if formatting changes or if the test is extended to a currency whose symbol is not `$` or whose group separator is not `,`.

**Fix:** Avoid parsing formatted strings. Assert on the underlying numeric results instead. Options:

1. Expose the raw `TotalRepay` decimal from the ViewModel for tests, or
2. Compute the expected simple and compound totals by calling `BtcLoanSimulationCalculator.Calculate` directly in the test and compare the VM's formatted fiat strings to `CurrencyDisplay.FormatFiat(expected, currencyCode)`.

## Info

### IN-01: Catch-all blocks swallow unexpected exceptions

**File:** `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorViewModel.cs:100, 175`
**Issue:** Both `LoadAvailableCurrencies` and `Recalculate` use bare `catch` blocks:

```csharp
catch
{
    // Skip invalid currency codes
}
```

and

```csharp
catch
{
    ClearResults();
}
```

While the first is intentionally defensive and the second matches the UI-SPEC requirement to silently clear on invalid input, both swallow *all* exception types. An unexpected bug in `BtcLoanSimulationCalculator` or in `FiatCurrency.GetFromCode` would be hidden from logs and from the developer.

**Fix:** Catch specific exception types (`ArgumentException`, `FormatException`, `KeyNotFoundException`) and rethrow or log truly unexpected exceptions. If logging is not available in the VM, at least narrow the catch scope so only the documented error conditions are swallowed.

### IN-02: `SetDefaultDates` also initializes fees

**File:** `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorViewModel.cs:125-131`
**Issue:** The method name `SetDefaultDates` suggests it only sets date defaults, but it also assigns `FeesFiatValue = FiatValue.New(0m)`. This is misleading for future maintainers.

**Fix:** Rename to `SetDefaultInputs` or move the fees default into a separate `SetDefaultFees` method called alongside it in the constructor.

---

_Reviewed: 2026-08-21T14:20:00Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_
