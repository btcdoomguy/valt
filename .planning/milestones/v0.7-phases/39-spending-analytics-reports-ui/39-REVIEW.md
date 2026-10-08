---
phase: 39-spending-analytics-reports-ui
reviewed: 2026-08-05T00:45:00Z
depth: standard
files_reviewed: 31
files_reviewed_list:
  - src/Valt.App/Modules/SpendingAnalytics/Contracts/IBurnRateQueries.cs
  - src/Valt.App/Modules/SpendingAnalytics/Contracts/IFixedVsVariableQueries.cs
  - src/Valt.App/Modules/SpendingAnalytics/Contracts/ISavingsRateQueries.cs
  - src/Valt.App/Modules/SpendingAnalytics/DTOs/BurnRateDataDto.cs
  - src/Valt.App/Modules/SpendingAnalytics/DTOs/FixedVsVariableDataDto.cs
  - src/Valt.App/Modules/SpendingAnalytics/DTOs/SavingsRateDataDto.cs
  - src/Valt.App/Modules/SpendingAnalytics/Queries/GetBurnRateHandler.cs
  - src/Valt.App/Modules/SpendingAnalytics/Queries/GetBurnRateQuery.cs
  - src/Valt.App/Modules/SpendingAnalytics/Queries/GetFixedVsVariableHandler.cs
  - src/Valt.App/Modules/SpendingAnalytics/Queries/GetFixedVsVariableQuery.cs
  - src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateHandler.cs
  - src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateQuery.cs
  - src/Valt.Infra/Extensions.cs
  - src/Valt.Infra/Modules/SpendingAnalytics/Queries/BurnRateQueries.cs
  - src/Valt.Infra/Modules/SpendingAnalytics/Queries/FixedVsVariableQueries.cs
  - src/Valt.Infra/Modules/SpendingAnalytics/Queries/SavingsRateQueries.cs
  - src/Valt.UI/Extensions.cs
  - src/Valt.UI/Lang/language.Designer.cs
  - src/Valt.UI/Lang/language.resx
  - src/Valt.UI/Views/Main/Tabs/Reports/DashboardDataUserControl.axaml
  - src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs
  - src/Valt.UI/Views/Main/Tabs/Reports/Panels/BurnRatePanelViewModel.cs
  - src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml
  - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.DesignTime.cs
  - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
  - src/Valt.UI/Views/Main/Tabs/Reports/RowItem.cs
  - src/Valt.UI/Views/Main/Tabs/Reports/SavingsRateChartData.cs
  - tests/Valt.Tests/Reports/BurnRateQueriesTests.cs
  - tests/Valt.Tests/Reports/FixedVsVariableQueriesTests.cs
  - tests/Valt.Tests/Reports/SavingsRateQueriesTests.cs
  - tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs
findings:
  critical: 0
  warning: 6
  info: 3
  total: 9
status: issues_found
---

# Phase 39: Code Review Report

**Reviewed:** 2026-08-05T00:45:00Z
**Depth:** standard
**Files Reviewed:** 31
**Status:** issues_found

## Summary

Phase 39 introduces the SpendingAnalytics App/Infra CQRS module and wires three new report panels into the Reports tab: savings-rate line chart, burn-rate dashboard card, and fixed-vs-variable stacked-bar chart. The implementation broadly follows the established patterns (SpendingEvolution query shape, `IReportDataProvider` reuse, `DashboardPanelViewModel` cards, LiveCharts dispose/recreate).

No critical security or crash bugs were found. The main issues are:

1. A **labeling bug** in the fixed-vs-variable chart Y-axis that always formats as USD instead of the user's main currency.
2. A **task-chaining bug** in `ReportsViewModel.Initialize` where panel refreshes are not awaited through the fire-and-forget runner.
3. Several **quality/testability gaps** around unused constructor parameters, uninitialised resource usage in tests, and missing disposal of shared `SolidColorPaint` instances.

Financial calculations in the query tests are correct and match the documented decisions (D-01 through D-15). DI registrations and localization keys are complete for English.

## Critical Issues

None.

## Warnings

### WR-01: Fixed-vs-variable Y-axis labels are hardcoded to USD

**File:** `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs:70-73`
**Issue:** The chart's Y-axis `Labeler` always formats with `FiatCurrency.Usd.Code`, so users whose main currency is not USD will see USD symbols on a chart that is actually valued in their main currency. The DTO carries the correct `PrimaryCurrency` but it is never used.
**Fix:** Store the primary currency on the class (mirroring `MonthlyTotalsChartData.FiatCurrency`) and update it in `RefreshChart`, then use it in the labeler:

```csharp
public string PrimaryCurrency { get; set; } = FiatCurrency.Usd.Code;

private string FiatLabeler(double value) =>
    CurrencyDisplay.FormatFiat((decimal)value, PrimaryCurrency);

public void RefreshChart(FixedVsVariableDataDto data)
{
    PrimaryCurrency = data.PrimaryCurrency;
    ...
}
```

### WR-02: `SavingsRateQueries` accepts an unused `IStatisticsReport` dependency

**File:** `src/Valt.Infra/Modules/SpendingAnalytics/Queries/SavingsRateQueries.cs:20-31`
**Issue:** The constructor receives `IStatisticsReport statisticsReport` but never stores or uses it. It adds DI noise and requires tests and the registration to supply an unnecessary dependency. `BurnRateQueries` legitimately uses the same service.
**Fix:** Remove the `IStatisticsReport statisticsReport` parameter from the constructor and update `SavingsRateQueriesTests.cs:143` accordingly. The DI registration will still resolve the remaining parameters.

### WR-03: `BurnRatePanelViewModel` uses `TransactionGridResources` brushes that require runtime initialization

**File:** `src/Valt.UI/Views/Main/Tabs/Reports/Panels/BurnRatePanelViewModel.cs:79-85`
**Issue:** The `vs median` row picks `TransactionGridResources.Credit` or `.Debt` to colour the text. In the running app this is fine because `App.axaml.cs:90` calls `TransactionGridResources.Initialize()`. However, any unit test that calls `BurnRatePanelViewModel.RefreshAsync()` without first calling `TransactionGridResources.InitializeForTesting()` will throw `InvalidOperationException`. The current tests avoid this by not invoking `RefreshAsync()`, but future tests or refactors are at risk.
**Fix:** Either call `TransactionGridResources.InitializeForTesting()` in `ReportsViewModelTests.SetUp`, or use `IValueConverter`/dynamic-resource brushes that do not depend on static mutable state.

### WR-04: `ReportsViewModel.Initialize` chains `ContinueWith` over async lambdas without unwrapping

**File:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs:305-309`
**Issue:**

```csharp
LoadDataAndFetchAllReportsAsync()
    .ContinueWith(_ => _leveragePanel.RefreshAsync(), TaskScheduler.Default)
    .ContinueWith(_ => _btcLoansPanel.RefreshAsync(), TaskScheduler.Default)
    .ContinueWith(_ => _burnRatePanel.RefreshAsync(), TaskScheduler.Default)
    .FireAndForgetSafeAsync(_runner, _logger);
```

Because each lambda returns `Task`, `ContinueWith` returns `Task<Task>` (and then `Task<Task<Task>>`, etc.). The runner only awaits the outermost task, which completes as soon as the final continuation delegate returns — not when the actual panel refresh work completes. Errors or hangs inside `_leveragePanel.RefreshAsync()`/`_btcLoansPanel.RefreshAsync()`/`_burnRatePanel.RefreshAsync()` are not observed by the runner.
**Fix:** Use an async continuation and `Unwrap()` so the runner observes the full asynchronous chain:

```csharp
LoadDataAndFetchAllReportsAsync()
    .ContinueWith(async _ =>
    {
        await _leveragePanel.RefreshAsync();
        await _btcLoansPanel.RefreshAsync();
        await _burnRatePanel.RefreshAsync();
    }, TaskScheduler.Default)
    .Unwrap()
    .FireAndForgetSafeAsync(_runner, _logger);
```

### WR-05: Error paths in new chart fetches do not reset empty-state flags

**File:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs:979-1001` and `1003-1031`
**Issue:** In `FetchSavingsRateAsync` and `FetchFixedVsVariableAsync`, the `catch` blocks set `IsSavingsRateLoading`/`IsFixedVsVariableLoading` to `false` but leave `IsSavingsRateEmpty`, `IsFixedVsVariableEmpty`, and `HasNoFixedExpenses` unchanged. If a previous run showed the empty/hint state and a later refresh fails, the UI will continue showing the stale empty state instead of clearing it.
**Fix:** Clear the flags in the catch blocks:

```csharp
await Dispatcher.UIThread.InvokeAsync(() =>
{
    IsSavingsRateEmpty = false;
    IsSavingsRateLoading = false;
});
```

and similarly for the fixed-vs-variable path.

### WR-06: New chart-data classes do not dispose shared `SolidColorPaint` resources

**File:** `src/Valt.UI/Views/Main/Tabs/Reports/SavingsRateChartData.cs:30-32,125-131` and `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs:28-30,136-143`
**Issue:** Both new classes create `LegendTextPaint`, `TooltipTextPaint`, and `TooltipBackgroundPaint` in the constructor but only dispose per-series paints in `Dispose()`. The shared `SolidColorPaint` instances implement `IDisposable` and are not disposed. This mirrors the existing `MonthlyTotalsChartData`/`ExpensesByCategoryChartData` classes, but the new classes repeat the same resource-leak pattern.
**Fix:** Dispose the shared paints in `Dispose()`:

```csharp
public void Dispose()
{
    (LegendTextPaint as IDisposable)?.Dispose();
    (TooltipTextPaint as IDisposable)?.Dispose();
    (TooltipBackgroundPaint as IDisposable)?.Dispose();
    DisposeSeries();
    Series.Clear();
    ...
}
```

(Consider applying the same fix to the pre-existing chart-data classes in a separate cleanup pass.)

## Info

### IN-01: Spanish and Portuguese localization are deferred

**File:** `src/Valt.UI/Lang/language.resx` and `src/Valt.UI/Lang/language.Designer.cs`
**Issue:** All new user-facing strings are present in English only. This is explicitly allowed by D-21 and the phase notes (Phase 43 will add `language.pt-BR.resx` and `language.es.resx`). No action required for this phase.

### IN-02: No test coverage for BTC/sat conversion in the fixed-vs-variable query

**File:** `tests/Valt.Tests/Reports/FixedVsVariableQueriesTests.cs`
**Issue:** The query implementation handles `FromSatAmount` and converts via `"SATS"`, but none of the seven tests seed a BTC-denominated expense. The EUR-to-BRL path is covered, but the sat-to-fiat path is not. Add a test with a BTC-account expense to guard against regressions in the "SATS" conversion branch.

### IN-03: Hardcoded SKColor hex values in chart data classes

**File:** `src/Valt.UI/Views/Main/Tabs/Reports/SavingsRateChartData.cs:18-27` and `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs:19-26`
**Issue:** Chart colours are hardcoded to hex values that mirror the Default theme. This is consistent with the existing `MonthlyTotalsChartData` and `ExpensesByCategoryChartData` classes and is permitted by the UI-SPEC. No change required unless the project wants a theme-aware chart palette in the future.

---

_Reviewed: 2026-08-05T00:45:00Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_
