---
phase: 40-btc-denominated-metrics-reports-ui
verified: 2026-08-06T19:20:00Z
status: passed
score: 31/31 must-haves verified
behavior_unverified: 2
overrides_applied: 0
re_verification:
  previous_status: gaps_found
  previous_score: 29/31
  gaps_closed:

    - "Metrics honor the existing Reports tab filters (date range, accounts, categories) — account filter now wired"
    - "Zero categories in the breakdown render the same empty-state copy as the monthly view"
  gaps_remaining: []
  regressions: []
behavior_unverified_items:

  - truth: "Stack velocity line chart X-axis labels rotate or truncate at >12 months without overflowing the 400px-high chart container"
    test: "Open the Reports tab, expand Stack velocity, select a date range spanning more than 12 months with data, and observe the X-axis labels."
    expected: "Labels remain inside the 400px chart container; they rotate, truncate, or step without overflowing/colliding."
    why_human: "Automated checks can verify the XAML declares LabelsRotation=-45 and Height=400, but whether Avalonia/LiveCharts actually clips/truncates labels for dense months is a visual/layout behavior."

  - truth: "Long empty-state body text wraps inside the hint-box and remains readable"
    test: "Open the Reports tab with no transactions, expand Sats earned & spent and Stack velocity, and view the empty-state boxes."
    expected: "Multi-line empty-state body text is fully visible, wrapped, and not clipped by the hint-box border."
    why_human: "XAML sets TextWrapping=Wrap and MaxWidth=500, but readability and actual wrapping behavior depend on runtime font metrics and container sizing."
human_verification:

  - test: "Open the Reports tab, expand Stack velocity, select a date range spanning more than 12 months with data, and observe the X-axis labels."
    expected: "Labels remain inside the 400px chart container; they rotate, truncate, or step without overflowing/colliding."
    why_human: "Automated checks can verify the XAML declares LabelsRotation=-45 and Height=400, but whether Avalonia/LiveCharts actually clips/truncates labels for dense months is a visual/layout behavior."

  - test: "Open the Reports tab with no transactions, expand Sats earned & spent and Stack velocity, and view the empty-state boxes."
    expected: "Multi-line empty-state body text is fully visible, wrapped, and not clipped by the hint-box border."
    why_human: "XAML sets TextWrapping=Wrap and MaxWidth=500, but readability and actual wrapping depend on runtime font metrics and container sizing."
---

# Phase 40: BTC-Denominated Metrics Reports & UI Verification Report

**Phase Goal:** Users can see their income and spending denominated in sats, including how fast their stack is growing
**Verified:** 2026-08-06T19:20:00Z
**Status:** human_needed
**Re-verification:** Yes — after gap closure by plan 40-04

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Reports tab contains a 'Sats earned & spent' section after 'Fixed vs variable expenses' and before 'Categories' | VERIFIED | `ReportsView.axaml` lines 530–649; expander is placed immediately after the Fixed vs variable expander and before the Categories expander. |
| 2 | The 'Stack velocity' section is placed below the 'Sats earned & spent' section | VERIFIED | `ReportsView.axaml` lines 650–727; expander follows the Sats earned & spent expander. |
| 3 | Sats earned per month includes fiat income converted at the exact transaction-date BTC rate plus native bitcoin income | VERIFIED | `BtcDenominatedMetricsQueries.cs` lines 85–112; `Should_Convert_Fiat_Income_And_Expense_To_Sats_Per_Month` and `Should_Include_Native_Bitcoin_Income_And_Expense_By_Sign` pass. |
| 4 | Sats spent per month includes fiat expenses converted at the exact transaction-date BTC rate plus native bitcoin expenses | VERIFIED | `BtcDenominatedMetricsQueries.cs` lines 85–112; tests pass. |
| 5 | BTC purchases, BTC sales, and internal transfers between the user's own accounts are excluded from sats earned and sats spent | VERIFIED | `BtcDenominatedMetricsQueries.cs` lines 69–76, 114–122; `Should_Exclude_Btc_Purchases_And_Sales_From_Earned_And_Spent` and `Should_Exclude_Internal_Transfers` pass. |
| 6 | Transactions whose transaction date has no exact BTC price are silently skipped from earned/spent calculations | VERIFIED | `BtcDenominatedMetricsQueries.cs` lines 75–76; `Should_Skip_Missing_Btc_Rate_Date` passes. |
| 7 | For non-main-currency fiat accounts, the conversion uses source currency → USD → BTC on the transaction date | VERIFIED | `BtcDenominatedMetricsQueries.cs` lines 177–186; `Should_Honor_Account_Filter` covers a BRL account and validates the conversion path. |
| 8 | The current incomplete month is included in the earned/spent monthly trend | VERIFIED | `BtcDenominatedMetricsQueries.cs` lines 128–144 baseline loop; `Should_Include_Current_Incomplete_Month` passes. |
| 9 | Stack velocity is computed per month as sats earned − sats spent + BTC purchases − BTC sales, with internal transfers excluded | VERIFIED | `BtcDenominatedMetricsQueries.cs` line 142; `Should_Include_Btc_Purchases_And_Sales_In_Stack_Velocity` and `Should_Exclude_Internal_Transfers` pass. |
| 10 | Stack velocity is rendered as a monthly line chart and the current incomplete month is included | VERIFIED | `StackVelocityChartData.cs` builds a `LineSeries<ObservablePoint>`; `ReportsView.axaml` binds to `StackVelocityChartData.Series/XAxes/YAxes`; baseline months include incomplete month. |
| 11 | Months with zero earned, zero spent, or zero stack velocity render as zero on the baseline, not omitted or gapped | VERIFIED | `BtcDenominatedMetricsQueries.cs` lines 128–144 emit every month in the range with zero defaults; `Should_Return_Zero_Velocity_Months_On_Baseline` passes. |
| 12 | When the query returns zero months, the 'Sats earned & spent' chart shows the hint-box empty state | VERIFIED | `ReportsView.axaml` lines 604–622 bind `IsVisible="{Binding IsBtcMetricsEmpty}"` to the hint-box with `Reports_BtcMetrics_EmptyHeading/EmptyBody`. |
| 13 | When the query returns zero months, the 'Stack velocity' chart shows the hint-box empty state | VERIFIED | `ReportsView.axaml` lines 682–700 bind `IsVisible="{Binding IsStackVelocityEmpty}"` to the hint-box with `Reports_StackVelocity_EmptyHeading/EmptyBody`. |
| 14 | While IsBtcMetricsLoading is true, the panel shows a centered Loading TextBlock | VERIFIED | `ReportsView.axaml` lines 644–646 bind `IsVisible="{Binding IsBtcMetricsLoading}"`. |
| 15 | While IsStackVelocityLoading is true, the panel shows a centered Loading TextBlock | VERIFIED | `ReportsView.axaml` lines 722–724 bind `IsVisible="{Binding IsStackVelocityLoading}"`. |
| 16 | On a fetch exception, both panels render the specified error copy | VERIFIED | `ReportsViewModel.cs` sets `IsBtcMetricsError`/`IsStackVelocityError`; `ReportsView.axaml` lines 624–642 and 702–719 show `Reports_BtcMetrics_ErrorTitle/ErrorBody`. |
| 17 | In populated state, the monthly view shows grouped Earned and Spent columns per month | VERIFIED | `BtcDenominatedMetricsChartData.cs` lines 104–118 create two `StackedColumnSeries<double>` named Earned/Spent. |
| 18 | In populated state, the stack velocity view shows a single line series for net sats per month | VERIFIED | `StackVelocityChartData.cs` lines 73–83 create a `LineSeries<ObservablePoint>` named Velocity. |
| 19 | New App-layer queries follow the CQRS pattern | VERIFIED | `IBtcDenominatedMetricsQueries`, `GetBtcDenominatedMetricsQuery`, `GetBtcDenominatedMetricsHandler`, and DTOs exist; `Extensions.cs` line 286 registers `IBtcDenominatedMetricsQueries`; `AddValtApp` auto-registers `IQueryHandler` implementations. |
| 20 | Panels render sensible zero/empty states when no transaction data exists for the period | VERIFIED | Empty-state borders and loading TextBlocks are bound; baseline months render zero values. |
| 21 | The 'Sats earned & spent' panel has a toggle that switches between monthly grouped bars and per-category horizontal-bar breakdown | VERIFIED | `ReportsView.axaml` lines 539–553 provide a ToggleSwitch bound to `IsBtcMetricsCategoryView`; monthly and category chart panels toggle visibility. |
| 22 | Category breakdown is computed from selected categories for the same date range and excludes internal transfers and missing-rate transactions | VERIFIED | `BtcDenominatedMetricsQueries.cs` applies the same From/To, account/category filters, internal-transfer guard, and missing-rate guard to the category aggregation; `Should_Aggregate_Sats_Spent_By_Category` and `Should_Honor_Category_Exclusion` pass. |
| 23 | Categories with zero sats spent are omitted from the category chart while remaining categories keep their relative scale | VERIFIED | `BtcDenominatedMetricsQueries.cs` lines 146–147 filter `Where(x => x.Value > 0)` before returning `SpentByCategory`. |
| 24 | Zero categories in the breakdown render the same empty-state copy as the monthly view | VERIFIED | `ReportsViewModel.cs` line 1116 sets `IsBtcMetricsEmpty = IsBtcMetricsCategoryView ? data.SpentByCategory.Count == 0 : data.Months.Count == 0`; line 1150 recomputes on toggle. |
| 25 | The category chart is wrapped in a ScrollViewer with MaxHeight='600' and VerticalScrollBarVisibility='Auto' | VERIFIED | `ReportsView.axaml` lines 587–588 wrap the category CartesianChart in a ScrollViewer with `MaxHeight="600"` and `VerticalScrollBarVisibility="Auto"`. |
| 26 | Comprehensive query tests cover scope decisions D-01 through D-09 and UI placement D-13, D-14, D-16 | VERIFIED | `BtcDenominatedMetricsQueriesTests.cs` contains 12 tests covering fiat conversion, native BTC signs, BTC purchase/sale exclusion/inclusion, internal-transfer exclusion, missing-rate skip, current month, baseline zeros, category aggregation, category exclusion, and account filter. |
| 27 | The build and the BTC metrics test suite remain green | VERIFIED | `dotnet build Valt.sln` succeeded with 0 warnings, 0 errors. `dotnet test --filter "FullyQualifiedName~BtcDenominatedMetrics"` passed 12/12. |
| 28 | The Sats earned & spent chart does not crash when toggling from Monthly to By category | VERIFIED | `BtcDenominatedMetricsChartData.cs` lines 77–86 set `CategoryXAxes[0].MinLimit = 0` and right padding to prevent LiveCharts separator overflow. |
| 29 | The By category row chart has a fixed lower X-axis bound of 0 and right-side padding for data labels | VERIFIED | `BtcDenominatedMetricsChartData.cs` lines 83–84 match `IncomeByCategoryChartData`/`ExpensesByCategoryChartData` configuration. |
| 30 | Metrics honor the existing Reports tab filters (date range, accounts, categories) | VERIFIED | `ReportsViewModel.cs` lines 1108 and 1167 pass `AccountIds = SelectedAccounts.Select(x => x.Id.ToString()).ToArray()`; `CategoryIds` is populated from `GetSelectedAnalyticsCategoryIds()`; date range comes from `FilterRange`; `DebouncedFetchCategoriesAsync` re-fetches on selection changes. |
| 31 | Changing SelectedAccounts re-fetches the Sats earned & spent and Stack velocity panels | VERIFIED | `ReportsViewModel.cs` lines 581–582 set loading flags in `OnSelectedFiltersChanged`; lines 595–596 include `FetchBtcDenominatedMetricsAsync` and `FetchStackVelocityAsync` in the debounced `Task.WhenAll`. |

**Score:** 31/31 functional must-haves verified (2 present but behavior-unverified)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/Valt.App/Modules/BtcDenominatedMetrics/Contracts/IBtcDenominatedMetricsQueries.cs` | Query contract | VERIFIED | Declares `GetBtcDenominatedMetricsAsync`. |
| `src/Valt.App/Modules/BtcDenominatedMetrics/Queries/GetBtcDenominatedMetricsQuery.cs` | Query record | VERIFIED | Implements `IQuery<BtcDenominatedMetricsDataDto>` with From/To/CategoryIds/AccountIds. |
| `src/Valt.App/Modules/BtcDenominatedMetrics/Queries/GetBtcDenominatedMetricsHandler.cs` | Query handler | VERIFIED | Thin pass-through to `IBtcDenominatedMetricsQueries`. |
| `src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/BtcDenominatedMetricsDataDto.cs` | Aggregate DTOs | VERIFIED | Contains `BtcDenominatedMetricsDataDto`, `BtcDenominatedMetricsMonthDto`, `SatsSpentByCategoryDto` (combined file). |
| `src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs` | Infra implementation | VERIFIED | Implements aggregation, rate conversion, internal-transfer/missing-rate guards, category breakdown. |
| `src/Valt.UI/Views/Main/Tabs/Reports/BtcDenominatedMetricsChartData.cs` | Monthly/category chart data | VERIFIED | Stacked columns, row series, `MinLimit=0`, right padding, disposal. |
| `src/Valt.UI/Views/Main/Tabs/Reports/StackVelocityChartData.cs` | Velocity line chart data | VERIFIED | Line series with fill, labels, disposal. |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` | ViewModel wiring | VERIFIED | New observables, fetch methods, dispose calls, account-filter wiring, view-aware empty state. |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` | XAML expanders | VERIFIED | Two expanders in correct order, loading/empty/error states, toggle, ScrollViewer. |
| `src/Valt.UI/Lang/language.resx` / `.pt-BR.resx` / `.es.resx` | Localization keys | VERIFIED | All `Reports_BtcMetrics_*` and `Reports_StackVelocity_*` keys present; pt-BR/es contain English placeholders per plan. |
| `src/Valt.UI/Lang/language.Designer.cs` | Generated accessors | VERIFIED | Static string accessors exist for all new keys. |
| `tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs` | Query tests | VERIFIED | 12 tests, all passing. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| ReportsViewModel.FetchBtcDenominatedMetricsAsync | GetBtcDenominatedMetricsQuery | `_queryDispatcher.DispatchAsync` | WIRED | `ReportsViewModel.cs` lines 1103–1109. |
| ReportsViewModel.FetchStackVelocityAsync | GetBtcDenominatedMetricsQuery | `_queryDispatcher.DispatchAsync` | WIRED | `ReportsViewModel.cs` lines 1162–1168. |
| GetBtcDenominatedMetricsQuery | BtcDenominatedMetricsQueries | DI + handler auto-registration | WIRED | Handler in Valt.App is auto-registered by `AddValtApp`; `IBtcDenominatedMetricsQueries` is registered in `src/Valt.Infra/Extensions.cs` line 286. |
| BtcDenominatedMetricsQueries.SpentByCategory | BtcDenominatedMetricsChartData category mode | `RefreshCategoryChart` | WIRED | `ReportsViewModel.cs` line 1139 calls `RefreshCategoryChart(data.SpentByCategory)`. |
| ReportsViewModel.IsBtcMetricsCategoryView | ReportsView.axaml toggle/chart visibility | TwoWay binding + `OnIsBtcMetricsCategoryViewChanged` | WIRED | `ReportsView.axaml` ToggleSwitch binds to `IsBtcMetricsCategoryView`; VM refreshes chart without re-fetching. |
| ReportsViewModel.SelectedAccounts | GetBtcDenominatedMetricsQuery.AccountIds | ViewModel wiring | WIRED | `ReportsViewModel.cs` lines 1108 and 1167 populate `AccountIds` from `SelectedAccounts`. |
| ReportsView.axaml | BtcDenominatedMetricsChartData.Series/XAxes/YAxes | XAML bindings | WIRED | `ReportsView.axaml` lines 568–570 and 594–596. |
| ReportsView.axaml | StackVelocityChartData.Series/XAxes/YAxes | XAML bindings | WIRED | `ReportsView.axaml` lines 671–673. |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| BtcDenominatedMetricsChartData.Series | EarnedValues/SpentValues | `BtcDenominatedMetricsQueries.GetBtcDenominatedMetricsAsync` → `_queryDispatcher.DispatchAsync` | Yes — seeded BTC/fiat rates and transactions in tests; real query uses `IReportDataProvider` rates. | FLOWING |
| BtcDenominatedMetricsChartData.Series (category mode) | values, CategoryLabels | `BtcDenominatedMetricsQueries.SpentByCategory` → `RefreshCategoryChart` | Yes — aggregated from transactions and category icons. | FLOWING |
| StackVelocityChartData.Series | VelocityValues, MonthLabels | Same query → `RefreshChart` | Yes — computed `StackVelocity` from aggregation. | FLOWING |
| ReportsView.axaml empty/loading/error states | IsBtcMetricsEmpty, IsStackVelocityEmpty, etc. | `ReportsViewModel` sets flags after fetch success/exception | Yes — driven by query result counts and exception state. | FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Solution builds cleanly | `dotnet build Valt.sln` | 0 warnings, 0 errors | PASS |
| BTC metrics query tests pass | `dotnet test --filter "FullyQualifiedName~BtcDenominatedMetrics"` | 12 passed, 0 failed | PASS |
| Full test suite | `dotnet test` | 1708 passed, 2 failed (pre-existing `BitcoinDominanceProviderTests.GetAsync_ReturnsValidData` and `CoinGeckoProviderTests.Should_Get_Prices_With_Usd_And_Up_To_Date` returning HTTP 403) | PASS with documented pre-existing failures |

### Probe Execution

No phase-declared or conventional probes were required for this UI/CQRS phase.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| BTC-01 | 40-01, 40-02, 40-04 | User can view sats earned per month | SATISFIED | Query returns `SatsEarned` per month; UI renders grouped column chart; tests verify fiat/native-BTC conversion. |
| BTC-02 | 40-01, 40-02, 40-04 | User can view sats spent per month and per category | SATISFIED | Query returns `SatsSpent` and `SpentByCategory`; UI has monthly/category toggle and row chart; account/category filters wired. |
| BTC-03 | 40-01, 40-02, 40-04 | User can view stack velocity as a trend chart | SATISFIED | Query computes `StackVelocity`; UI renders line chart with baseline months; account filter wired. |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| None | — | — | — | No `TODO`/`FIXME`/`XXX`/`HACK`/placeholder markers found in the new BTC metrics files. |

### Human Verification Required

1. **Stack velocity X-axis label density at >12 months**
   - **Test:** Open the Reports tab, expand Stack velocity, select a date range spanning more than 12 months with data, and observe the X-axis labels.
   - **Expected:** Labels remain inside the 400px chart container; they rotate, truncate, or step without overflowing/colliding.
   - **Why human:** Automated checks can verify the XAML declares `LabelsRotation="-45"` and `Height="400"`, but whether Avalonia/LiveCharts actually clips/truncates labels for dense months is a visual/layout behavior.

2. **Empty-state text wrapping/readability**
   - **Test:** Open the Reports tab with no transactions, expand Sats earned & spent and Stack velocity, and view the empty-state boxes.
   - **Expected:** Multi-line empty-state body text is fully visible, wrapped, and not clipped by the hint-box border.
   - **Why human:** XAML sets `TextWrapping="Wrap"` and `MaxWidth="500"`, but readability and actual wrapping depend on runtime font metrics and container sizing.

### Gaps Summary

All functional gaps identified in the initial verification have been closed by plan 40-04:

1. **Account filter wired to BTC metrics.** `FetchBtcDenominatedMetricsAsync` and `FetchStackVelocityAsync` now pass `SelectedAccounts` into `GetBtcDenominatedMetricsQuery.AccountIds`, and `DebouncedFetchCategoriesAsync` re-fetches both panels when the account selection changes.
2. **Category-breakdown empty state matches monthly view.** `IsBtcMetricsEmpty` is now computed from `SpentByCategory.Count` when the category view is active and from `Months.Count` when the monthly view is active, with the toggle handler recomputing from cached data.

The remaining two items are visual/layout backstops that require human UI review; they do not block the functional goal.

---
_Verified: 2026-08-06T19:20:00Z_
_Verifier: the agent (gsd-verifier)_
