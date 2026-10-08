---
phase: 39-spending-analytics-reports-ui
verified: 2026-08-04T00:00:00Z
status: passed
score: 12/12 must-haves verified
behavior_unverified: 0
overrides_applied: 0
gaps: []
behavior_unverified_items: []
human_verification: []
---

# Phase 39: Spending Analytics Reports & UI Verification Report

**Phase Goal:** Users can understand their spending behavior — how much they save, how fast they burn cash, and how rigid their expense structure is
**Verified:** 2026-08-04
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Savings rate query computes `(income − |expenses|) / income * 100` per month, emits `null` for zero-income months, keeps negative sign, and excludes the current incomplete month | VERIFIED | `SavingsRateQueries.cs` lines 48-88; `SavingsRateQueriesTests.cs` passes 5 tests including positive rate, zero-income null, negative rate, current-month exclusion, and empty state |
| 2 | Burn rate query returns MTD spend, avg daily spend, projected month-end only from day ≥ 5, reuses `IStatisticsReport.MedianMonthlyExpenses`, and computes `vs-median %` | VERIFIED | `BurnRateQueries.cs` lines 35-96; `BurnRateQueriesTests.cs` passes 5 tests covering projection gate, median reuse, zero-median null, and no-data empty state |
| 3 | Fixed vs variable query splits expenses using only `Paid` fixed-expense records, excludes `ManuallyPaid`/`Ignored`/`Empty`, ensures `fixed + variable = 100%`, falls back to 100% variable, excludes current month, and flags `HasNoFixedExpenses` | VERIFIED | `FixedVsVariableQueries.cs` lines 39-119; `FixedVsVariableQueriesTests.cs` passes 7 tests covering all states, 100% variable, current-month exclusion, no-fixed-expenses flag, and multi-currency conversion |
| 4 | All three queries return empty/zero DTOs instead of throwing when no transaction data exists for the period | VERIFIED | Empty guards in `SavingsRateQueries.cs` (37-44), `BurnRateQueries.cs` (41-56), `FixedVsVariableQueries.cs` (43-52); covered by tests in all three query fixtures |
| 5 | Savings rate query is dispatchable via `IQueryDispatcher` and registered in DI | VERIFIED | `GetSavingsRateQuery.cs` implements `IQuery<SavingsRateDataDto>`; `GetSavingsRateHandler.cs` dispatches; `Valt.Infra/Extensions.cs` registers `ISavingsRateQueries` |
| 6 | Burn rate query is dispatchable via `IQueryDispatcher` and registered in DI | VERIFIED | `GetBurnRateQuery.cs` implements `IQuery<BurnRateDataDto>`; `GetBurnRateHandler.cs` dispatches; `Valt.Infra/Extensions.cs` registers `IBurnRateQueries` |
| 7 | Fixed vs variable query is dispatchable via `IQueryDispatcher` and registered in DI | VERIFIED | `GetFixedVsVariableQuery.cs` implements `IQuery<FixedVsVariableDataDto>`; `GetFixedVsVariableHandler.cs` dispatches; `Valt.Infra/Extensions.cs` registers `IFixedVsVariableQueries` |
| 8 | Burn rate dashboard card is present in `ReportsView.axaml`, bound to VM, shows all row variants, Credit/Debt vs-median coloring, and empty state | VERIFIED | `BurnRatePanelViewModel.cs` builds rows; `ReportsView.axaml` lines 117-127 include the card after Statistics; `DashboardDataUserControl.axaml` binds `RightTextForeground` with `Text100Brush` fallback; `ReportsViewModelTests.cs` covers visibility mirroring |
| 9 | Savings rate chart section is present in `ReportsView.axaml`, bound to VM, uses null-gap line chart, and refetches on date-range filter change | VERIFIED | `SavingsRateChartData.cs` creates `LineSeries<ObservablePoint>` with null Y for missing rates; `ReportsView.axaml` lines 389-445 adds the Expander section; `ReportsViewModel.cs` `FetchSavingsRateAsync` and `OnFilterRangeChanged` wire it; `ReportsViewModel.DesignTime.cs` populates sample data |
| 10 | Fixed vs variable chart section is present in `ReportsView.axaml`, bound to VM, uses stacked Fixed/Variable bars, and shows hint-box empty state when no fixed expenses registered | VERIFIED | `FixedVsVariableChartData.cs` creates two `StackedColumnSeries<double>`; `ReportsView.axaml` lines 446-518 adds the Expander section with `Border.hint-box` for `HasNoFixedExpenses`; `ReportsViewModel.cs` sets the flag and refetches on filter change; design-time sample renders the chart |
| 11 | English localization strings for all new panels are present in `language.resx` and `language.Designer.cs`; pt-BR/es files untouched | VERIFIED | 18 new `Reports_*` keys found in `language.resx` and `language.Designer.cs`; no matching strings in `language.pt-BR.resx` or `language.es.resx` (grep returned no files) |
| 12 | New chart-data classes are disposed in `ReportsViewModel.Dispose` alongside existing ones | VERIFIED | `ReportsViewModel.cs` lines 1225-1230 dispose `MonthlyTotalsChartData`, `ExpensesByCategoryChartData`, `IncomeByCategoryChartData`, `SavingsRateChartData`, `FixedVsVariableChartData`, `WealthOverviewChartData` |

**Score:** 12/12 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateQuery.cs` | `IQuery<SavingsRateDataDto>` record | VERIFIED | Exists with date range and filter arrays |
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateHandler.cs` | Pass-through handler | VERIFIED | Dispatches `ISavingsRateQueries` |
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetBurnRateQuery.cs` | `IQuery<BurnRateDataDto>` record | VERIFIED | Includes `CurrentWealthInFiat` and filter arrays |
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetBurnRateHandler.cs` | Pass-through handler | VERIFIED | Dispatches `IBurnRateQueries` |
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetFixedVsVariableQuery.cs` | `IQuery<FixedVsVariableDataDto>` record | VERIFIED | Includes date range and filter arrays |
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetFixedVsVariableHandler.cs` | Pass-through handler | VERIFIED | Dispatches `IFixedVsVariableQueries` |
| `src/Valt.App/Modules/SpendingAnalytics/DTOs/SavingsRateDataDto.cs` | DTO with `decimal? Rate` | VERIFIED | `Months` + `PrimaryCurrency` records |
| `src/Valt.App/Modules/SpendingAnalytics/DTOs/BurnRateDataDto.cs` | Gauge DTO | VERIFIED | All required fields including `ProjectedMonthEnd` nullable |
| `src/Valt.App/Modules/SpendingAnalytics/DTOs/FixedVsVariableDataDto.cs` | Split DTO with `HasNoFixedExpenses` | VERIFIED | `Months` + `HasNoFixedExpenses` + `PrimaryCurrency` |
| `src/Valt.Infra/Modules/SpendingAnalytics/Queries/SavingsRateQueries.cs` | Reuses `IMonthlyTotalsReport` | VERIFIED | Uses `IReportDataProviderFactory` + `IMonthlyTotalsReport` + `IClock` |
| `src/Valt.Infra/Modules/SpendingAnalytics/Queries/BurnRateQueries.cs` | Reuses `IMonthlyTotalsReport` + `IStatisticsReport` | VERIFIED | Median from `StatisticsReport.GetAsync` |
| `src/Valt.Infra/Modules/SpendingAnalytics/Queries/FixedVsVariableQueries.cs` | Direct LiteDB + fixed-record join | VERIFIED | Joins `budget_fixedexpenserecords` on `Paid` + `Transaction != null`; uses `ICurrencyConversionService` |
| `tests/Valt.Tests/Reports/SavingsRateQueriesTests.cs` | Behavior tests | VERIFIED | 5 tests, all passing |
| `tests/Valt.Tests/Reports/BurnRateQueriesTests.cs` | Behavior tests | VERIFIED | 5 tests, all passing |
| `tests/Valt.Tests/Reports/FixedVsVariableQueriesTests.cs` | Behavior tests | VERIFIED | 7 tests, all passing |
| `src/Valt.UI/Views/Main/Tabs/Reports/Panels/BurnRatePanelViewModel.cs` | Dashboard panel VM | VERIFIED | Dispatches `GetBurnRateQuery`, formats rows, Credit/Debt coloring |
| `src/Valt.UI/Views/Main/Tabs/Reports/RowItem.cs` | Optional `RightTextForeground` | VERIFIED | New positional parameter, existing call sites compile |
| `src/Valt.UI/Views/Main/Tabs/Reports/SavingsRateChartData.cs` | Line chart data | VERIFIED | `LineSeries<ObservablePoint>`, null gap, dispose pattern |
| `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs` | Stacked bar chart data | VERIFIED | Two `StackedColumnSeries<double>`, dispose pattern, `PrimaryCurrency` labeler |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` | Wiring | VERIFIED | Observables, fetch methods, refresh triggers, disposal |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` | New sections | VERIFIED | Burn rate card, savings rate Expander, fixed vs variable Expander |
| `src/Valt.UI/Lang/language.resx` | English strings | VERIFIED | 18 new keys |
| `src/Valt.UI/Lang/language.Designer.cs` | Generated properties | VERIFIED | 18 new static properties |
| `src/Valt.UI/Extensions.cs` | DI registration | VERIFIED | `services.AddSingleton<BurnRatePanelViewModel>()` |
| `src/Valt.Infra/Extensions.cs` | DI registrations | VERIFIED | `ISavingsRateQueries`, `IBurnRateQueries`, `IFixedVsVariableQueries` registered |
| `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` | Wiring tests | VERIFIED | Covers new panel visibility, loading flags, chart-data observables, `HasNoFixedExpenses` |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `SavingsRateQueries.cs` | `MonthlyTotalsReport.cs` | `IMonthlyTotalsReport.GetAsync` with factory-built provider | VERIFIED | gsd-tools confirmed pattern found |
| `BurnRateQueries.cs` | `StatisticsReport.cs` | `IStatisticsReport.GetAsync` -> `MedianMonthlyExpenses` | VERIFIED | gsd-tools confirmed pattern found |
| `Valt.Infra/Extensions.cs` | `SpendingAnalytics/Queries/` | `AddSingleton` registrations | VERIFIED | gsd-tools confirmed pattern found |
| `FixedVsVariableQueries.cs` | `FixedExpenseRecordEntity.cs` | `budget_fixedexpenserecords` join on `Paid` + non-null `Transaction` | VERIFIED | gsd-tools confirmed pattern found |
| `FixedVsVariableQueries.cs` | `SpendingEvolutionQueries.cs` | Same LiteDB filter + currency conversion pattern | VERIFIED | gsd-tools confirmed pattern found |
| `BurnRatePanelViewModel.cs` | `GetBurnRateQuery.cs` | `IQueryDispatcher.DispatchAsync` with `CurrentWealthInFiat` | VERIFIED | gsd-tools confirmed pattern found |
| `ReportsViewModel.cs` | `BurnRatePanelViewModel.cs` | `PropertyChanged` mirroring + `RefreshAsync` triggers | VERIFIED | gsd-tools confirmed pattern found |
| `DashboardDataUserControl.axaml` | `RowItem.cs` | `RightTextForeground` binding with `Text100Brush` fallback | VERIFIED | gsd-tools confirmed pattern found |
| `ReportsViewModel.cs` | `GetSavingsRateQuery.cs` | `FetchSavingsRateAsync` dispatch from `FetchAllReportsAsync` + `OnFilterRangeChanged` | VERIFIED | gsd-tools confirmed pattern found |
| `ReportsViewModel.cs` | `GetFixedVsVariableQuery.cs` | `FetchFixedVsVariableAsync` dispatch | VERIFIED | gsd-tools confirmed pattern found |
| `SavingsRateChartData.cs` | `null ObservablePoint` | `decimal? Rate` null -> `ObservablePoint(index, null)` -> `EnableNullSplitting` gap | VERIFIED (manual) | gsd-tools regex `ObservablePoint\(index, null\)` did not match because the code uses a conditional expression `new ObservablePoint(index, month.Rate.HasValue ? (double?)month.Rate.Value : null)` — functionally identical and null is passed when `Rate` is null |
| `ReportsView.axaml` | `Border.hint-box` | D-10 empty state replaces chart when `HasNoFixedExpenses` | VERIFIED | gsd-tools confirmed pattern found |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|-------------------|--------|
| `SavingsRateChartData` | `RateValues`, `MonthLabels` | `SavingsRateDataDto.Months` from `GetSavingsRateQuery` via `ReportsViewModel.FetchSavingsRateAsync` | Yes — seeded from LiteDB transactions in tests | FLOWING |
| `FixedVsVariableChartData` | `FixedValues`, `VariableValues`, `MonthLabels` | `FixedVsVariableDataDto.Months` from `GetFixedVsVariableQuery` via `ReportsViewModel.FetchFixedVsVariableAsync` | Yes — seeded from LiteDB transactions + fixed-expense records in tests | FLOWING |
| `BurnRatePanelViewModel` | `BurnRateData` rows | `BurnRateDataDto` from `GetBurnRateQuery` via `IQueryDispatcher` | Yes — computed from `MonthlyTotalsReport` + `StatisticsReport` | FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Solution builds with zero errors | `dotnet build Valt.sln --nologo -v q` | 0 errors, 106 warnings (pre-existing) | PASS |
| Spending-analytics query + UI tests pass | `dotnet test --filter "FullyQualifiedName~SavingsRateQueriesTests\|FullyQualifiedName~BurnRateQueriesTests\|FullyQualifiedName~FixedVsVariableQueriesTests\|FullyQualifiedName~ReportsViewModelTests" --nologo -v q` | 26 passed, 0 failed, 0 skipped | PASS |
| All spending-analytics-related tests pass | `dotnet test --filter "FullyQualifiedName~SpendingAnalytics\|FullyQualifiedName~SavingsRate\|FullyQualifiedName~BurnRate\|FullyQualifiedName~FixedVsVariable\|FullyQualifiedName~ReportsViewModel" --nologo -v q` | 36 passed, 0 failed, 0 skipped | PASS |

### Probe Execution

No phase-specific probes declared. Verification relies on build and NUnit test spot-checks above.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| SPA-01 | 39-01, 39-04 | Monthly savings rate with trend over time | SATISFIED | `SavingsRateQueries` + `SavingsRateChartData` + `ReportsView.axaml` section + tests |
| SPA-02 | 39-01, 39-03 | Burn rate — avg daily, projected month-end, vs median | SATISFIED | `BurnRateQueries` + `BurnRatePanelViewModel` + `ReportsView.axaml` card + tests |
| SPA-03 | 39-02, 39-04 | Fixed vs variable expense ratio per month | SATISFIED | `FixedVsVariableQueries` + `FixedVsVariableChartData` + `ReportsView.axaml` section + tests |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| None | — | — | — | No `TBD`, `FIXME`, `XXX`, `TODO`, `HACK`, `PLACEHOLDER`, or placeholder returns found in the new `SpendingAnalytics` module, chart-data classes, or panel VM. | Info |

Notes from the 39-REVIEW.md warnings — all were addressed in the follow-up commit `0bbc3aa`:
- `FixedVsVariableChartData` Y-axis labeler now uses `PrimaryCurrency` (was hardcoded USD).
- `SavingsRateQueries` no longer accepts the unused `IStatisticsReport` dependency.
- `ReportsViewModel.Initialize` now uses `async _ => { ... }` + `.Unwrap()` for the panel continuation chain.
- Error paths in `FetchSavingsRateAsync` and `FetchFixedVsVariableAsync` now clear the empty-state flags.
- Both new chart-data classes dispose shared `SolidColorPaint` instances in `Dispose()`.
- `ReportsViewModelTests` calls `TransactionGridResources.InitializeForTesting()` to avoid static-state exceptions.

### Human Verification Required

None. All phase behaviors are covered by automated build and targeted NUnit tests; the UI AXAML is compiled (zero errors) and bindings are wired to the new ViewModel observables and chart-data classes.

### Gaps Summary

No gaps identified. The Phase 39 goal, all four roadmap success criteria, and all plan-level must-haves are satisfied. The 4 pre-existing full-suite failures (live-network CoinGecko/Bitcoin-dominance API tests returning 403, and unrelated `AssetToolsSoldStateTests` integration failures) are explicitly out of scope for this phase and are not blockers.

---

_Verified: 2026-08-04_
_Verifier: the agent (gsd-verifier)_
