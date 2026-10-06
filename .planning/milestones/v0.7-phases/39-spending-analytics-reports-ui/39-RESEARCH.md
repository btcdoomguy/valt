# Phase 39: Spending Analytics Reports & UI - Research

**Researched:** 2026-08-04
**Domain:** Financial analytics (savings rate, burn rate, fixed/variable split) in a .NET 10 / Avalonia desktop app
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Savings Rate (SPA-01)
- **D-01:** Income and expenses come from the same values `MonthlyTotalsReport` already computes per month — no new income/expense semantics.
- **D-02:** Months with zero income are skipped in the trend (gap in the line) — division by zero is undefined; do not show 0% or N/A.
- **D-03:** Negative savings rates are displayed as-is (e.g. `-25%`), matching `MonthlyReportItemViewModel`'s existing +/- formatting pattern.
- **D-04:** The trend covers complete months only — the current incomplete month is excluded.

#### Fixed vs Variable Ratio (SPA-03)
- **D-05:** "Fixed" = actual expense transactions bound to fixed expense records; "variable" = all remaining expense transactions. Planned fixed-expense range amounts are NOT used.
- **D-06:** Only records in `Paid` state (bound to an actual transaction) count toward fixed. `ManuallyPaid`, `Ignored`, and `Empty` contribute nothing.
- **D-07:** The ratio is a split of the month's total expenses — fixed + variable = 100% of spend (not measured against income).
- **D-08:** A month with expenses but no bound fixed-expense transactions renders as 100% variable.
- **D-09:** Complete months only — current incomplete month excluded (consistent with D-04).
- **D-10:** When the user has no fixed expenses registered at all, the panel shows an empty state with a hint prompting them to register fixed expenses to unlock the metric.

#### Burn Rate (SPA-02)
- **D-11:** Burn rate is a current-month gauge only — no historical per-month trend in this phase.
- **D-12:** Average daily spend = month-to-date expenses / days elapsed; projected month-end total = average daily × days in month.
- **D-13:** The projection is compared against `StatisticsReport`'s existing `MedianMonthlyExpenses` (median of last 12 months) — reuse it, don't compute a new median.
- **D-14:** "Spend" = the same Expenses figure `MonthlyTotalsReport` computes — consistent with all other panels.
- **D-15:** The projection is shown only from day 5 of the month onward. Before day 5, the panel shows spend-so-far and average daily without a projection (early-month projections are noise).

#### Panel Layout & Filters
- **D-16:** New panels plug into the existing `DashboardGridPanel` (like Wealth/Statistics/Indicators cards); savings rate and fixed vs variable get charts below alongside the existing charts; burn rate is a dashboard card.
- **D-17:** Savings rate = single line chart of monthly %. Fixed vs variable = stacked bar per month (fixed/variable segments).
- **D-18:** All new panels honor the existing Reports tab filters (accounts, categories, date range), like `MonthlyTotals` and `ExpensesByCategory` already do.

#### Cross-Cutting Conventions
- **D-19:** New queries follow the App-layer CQRS pattern per AGENTS.md and the SpendingEvolution module: query + contract + DTO in `Valt.App`, implementation in `Valt.Infra`.
- **D-20:** Metrics are fiat-denominated (main fiat currency). BTC-denominated spending metrics are Phase 40's scope.
- **D-21:** New user-facing strings may be English-only in this phase; full pt-BR/es localization lands in Phase 43 (same pattern as v0.5 Phases 30→31).

### the agent's Discretion
- Exact placement order of the new dashboard card within `DashboardGridPanel` and chart ordering on the tab — planner/designer picks what fits the existing grid layout.
- Loading-state and empty-state visuals beyond the behaviors specified above — follow existing panel patterns (`IsLoading` text, `IsLarge` row spans).

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope. (Historical daily-burn trend lines, planned-vs-actual fixed expense drift, and sats-denominated spending all belong to Phase 40 or the v2 backlog.)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SPA-01 | User can view monthly savings rate ((income − expenses) / income) with trend over time | `MonthlyTotalsReport` per-month `AllIncomeInFiat`/`AllExpensesInFiat` is the exact source (D-01); LiveCharts `EnableNullSplitting` gives the D-02 gap; `SavingsRateChartData` mirrors `MonthlyTotalsChartData` (verified patterns below) |
| SPA-02 | User can view burn rate — average daily spend and projected month-end total vs median | `StatisticsReport.MedianMonthlyExpenses` is the D-13 baseline (signature verified); `MonthlyTotalsReport` with a current-MTD range yields spend-so-far; `BurnRatePanelViewModel : DashboardPanelViewModel` is the card plumbing |
| SPA-03 | User can view fixed vs variable expense ratio per month (FixedExpenses vs actual transactions) | `FixedExpenseRecordEntity` (`st` = state, `Transaction` BsonRef) identifies bound Paid transactions (D-05/D-06); `SpendingEvolutionQueries` shows the direct-LiteDB monthly aggregation + currency conversion pattern |
</phase_requirements>

## Summary

Phase 39 adds three read-only analytics panels to the existing Reports tab. All raw material already exists in the codebase: `MonthlyTotalsReport` computes per-month income/expense in the main fiat currency (savings rate + burn rate spend source), `StatisticsReport.MedianMonthlyExpenses` is the 12-month median the burn rate compares against, and `FixedExpenseRecordEntity` records in `Paid` state identify which transactions count as "fixed". The UI side is entirely a copy-and-adapt exercise: a new `DashboardPanelViewModel` subclass for the burn-rate card, and two new chart-data classes mirroring `MonthlyTotalsChartData` (line) and a new `StackedColumnSeries` chart for fixed/variable.

The main architectural decision is where computation lives. CONTEXT D-19 mandates the App-layer CQRS pattern (SpendingEvolution reference). The clean approach is a new `SpendingAnalytics` App module with three queries (`GetSavingsRateQuery`, `GetBurnRateQuery`, `GetFixedVsVariableQuery`), contracts + DTOs in `Valt.App`, implementations in `Valt.Infra`. The savings-rate and burn-rate infra implementations should **reuse `IMonthlyTotalsReport` + `IStatisticsReport` via `IReportDataProviderFactory`** so the numbers are byte-identical to the existing panels (D-01/D-13/D-14) — do not recompute income/expense semantics with a second direct-LiteDB aggregation. The fixed/variable query has no existing report to reuse, so it follows the `SpendingEvolutionQueries` direct-LiteDB pattern plus the `budget_fixedexpenserecords` collection.

One UI gap requires a small extension: `RowItem` currently carries no color, but the UI-SPEC requires the burn-rate "vs median" row to render green/red. Add an optional foreground to `RowItem` (or a converter) and bind it in `DashboardDataUserControl.axaml`, using `TransactionGridResources.Credit/Debt`.

**Primary recommendation:** One `Valt.App/Modules/SpendingAnalytics` module, three queries; savings-rate + burn-rate infra implementations delegate to `IMonthlyTotalsReport`/`IStatisticsReport` with an `IReportDataProviderFactory`-built provider; fixed/variable follows the `SpendingEvolutionQueries` LiteDB pattern; UI mirrors existing chart-data/dashboard-panel classes verbatim.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Savings-rate computation | Valt.App query contract | Valt.Infra (reuses `IMonthlyTotalsReport`) | App layer owns feature orchestration per AGENTS.md; Infra owns LiteDB/report access |
| Burn-rate computation (MTD, avg daily, projection) | Valt.App query contract | Valt.Infra (reuses `IMonthlyTotalsReport` + `IStatisticsReport`) | Same pattern; median must be identical to Statistics card (D-13) |
| Fixed vs variable computation | Valt.App query contract | Valt.Infra (direct LiteDB on transactions + `budget_fixedexpenserecords`) | No existing report computes this; SpendingEvolution is the reference |
| Panel rendering / loading / empty states | Valt.UI (ReportsViewModel + panel VMs) | — | Existing tab-level patterns (`FetchAllReportsAsync`, `Is*Loading`) |
| Currency conversion | Valt.Infra (`ICurrencyConversionService` / `IReportDataProvider` rates) | — | Conversion chain source→USD→target already established; never hand-roll |
| Filter plumbing (accounts/categories/date) | Valt.UI (`ReportsViewModel` filter state) | Valt.App (query properties) | SpendingEvolution query already carries `AccountIds`/`CategoryIds`/`From`/`To` |

## Standard Stack

**No new external packages.** Everything is already vendored.

### Core (already in project — do not add alternatives)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| LiveChartsCore.SkiaSharpView.Avalonia | 2.0.2 [VERIFIED: ~/.nuget/packages cache] | Line + stacked-column charts | Already renders all Reports charts; `EnableNullSplitting` verified in vendored XML docs |
| Avalonia (FluentTheme) | 12.x (project-managed) | AXAML panels | Project standard |
| CommunityToolkit.Mvvm | project-managed | `[ObservableProperty]`, `[RelayCommand]` | All ViewModels use it |
| LiteDB | project-managed | transactions + fixed-expense-records collections | Embedded store |
| Microsoft.Extensions.DependencyInjection (Scrutor `services.Scan`) | project-managed | Auto-registration of handlers/queries | `Valt.App/Extensions.cs` scans assemblies |

### Supporting (patterns to reuse)
| Component | Purpose | When to Use |
|-----------|---------|-------------|
| `IReportDataProviderFactory` / `IReportDataProvider` | Pre-indexed frozen data + historical rates | Savings-rate and burn-rate query implementations |
| `IMonthlyTotalsReport` | Per-month `Income`/`Expenses`/`AllIncomeInFiat`/`AllExpensesInFiat` | D-01/D-14 source values |
| `IStatisticsReport` | `MedianMonthlyExpenses` (12-mo median) | D-13 baseline |
| `ICurrencyConversionService` | source→USD→target conversion | Fixed/variable query (SpendingEvolution pattern) |
| `CurrencySettings.MainFiatCurrency` | Target currency (D-20) | All three queries |

**Installation:** none.

**Version verification:** LiveChartsCore 2.0.2 confirmed present in the local NuGet cache and referenced from `Valt.UI.csproj` [VERIFIED: filesystem]. Project targets `net10.0` with .NET SDK 10.0.101 installed [VERIFIED: `dotnet --version`]. (AGENTS.md says ".NET 9" — code wins; csproj says net10.0.)

## Package Legitimacy Audit

No external packages are installed in this phase. LiveChartsCore.SkiaSharpView.Avalonia 2.0.2 is already vendored and verified in the local package cache. **No audit rows required.**

## Architecture Patterns

### System Architecture Diagram

```
ReportsViewModel (tab active)
   │ FetchAllReportsAsync(provider)  ── Task.WhenAll
   ├─► GetSavingsRateQuery ──► ISavingsRateQueries (Infra)
   │      └─ IReportDataProviderFactory → IMonthlyTotalsReport
   │         └─ per-month (AllIncomeInFiat, AllExpensesInFiat)
   │            → rate = (income − |expenses|) / income   [skip income==0 → null]
   ├─► GetBurnRateQuery ──► IBurnRateQueries (Infra)
   │      ├─ IMonthlyTotalsReport(MTD range) → expensesMTD
   │      ├─ IStatisticsReport → MedianMonthlyExpenses   (D-13 reuse)
   │      └─ avgDaily = expensesMTD / dayOfMonth;
   │         projection = avgDaily × daysInMonth   (only if day ≥ 5, D-15)
   └─► GetFixedVsVariableQuery ──► IFixedVsVariableQueries (Infra)
          ├─ LiteDB transactions (expenses, date range, filters)
          ├─ budget_fixedexpenserecords WHERE st = Paid   (D-06)
          └─ per month: fixed = Σ paid-bound |expenses|; variable = total − fixed
                 │
                 ▼
      Valt.UI: SavingsRateChartData (LineSeries, null-gap) ·
               FixedVsVariableChartData (StackedColumnSeries) ·
               BurnRatePanelViewModel (DashboardData rows)
```

### Recommended Project Structure
```
src/Valt.App/Modules/SpendingAnalytics/
├── Contracts/   ISavingsRateQueries.cs, IBurnRateQueries.cs, IFixedVsVariableQueries.cs
├── DTOs/        SavingsRateDataDto.cs (+SavingsRateMonthDto),
│                BurnRateDataDto.cs, FixedVsVariableDataDto.cs (+FixedVsVariableMonthDto)
└── Queries/     GetSavingsRateQuery.cs (+Handler), GetBurnRateQuery.cs (+Handler),
                 GetFixedVsVariableQuery.cs (+Handler)

src/Valt.Infra/Modules/SpendingAnalytics/
└── Queries/     SavingsRateQueries.cs, BurnRateQueries.cs, FixedVsVariableQueries.cs

src/Valt.UI/Views/Main/Tabs/Reports/
├── SavingsRateChartData.cs          (mirror MonthlyTotalsChartData)
├── FixedVsVariableChartData.cs      (StackedColumnSeries)
├── Panels/BurnRatePanelViewModel.cs (mirror WealthPanelViewModel)
└── ReportsViewModel.cs / ReportsView.axaml (wire-in)
```

### Pattern 1: App-layer query (SpendingEvolution reference)
**What:** Query record + handler + `I*Queries` contract + DTO in `Valt.App`; implementation in `Valt.Infra`; auto-registered via `services.Scan` in `Valt.App/Extensions.cs` plus an explicit singleton registration in `src/Valt.Infra/Extensions.cs` (line ~278 for `ISpendingEvolutionQueries`).
**When to use:** all three metrics (D-19).
**Example:**
```csharp
// Source: src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionQuery.cs [VERIFIED: codebase]
public record GetSavingsRateQuery : IQuery<SavingsRateDataDto>
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string[] CategoryIds { get; init; } = Array.Empty<string>();
    public string[] AccountIds { get; init; } = Array.Empty<string>();
}
```
Infra registration follows `services.AddSingleton<ISpendingEvolutionQueries, SpendingEvolutionQueries>();` in `src/Valt.Infra/Extensions.cs`.

### Pattern 2: Reuse MonthlyTotalsReport for identical numbers (D-01/D-13/D-14)
**What:** Inject `IReportDataProviderFactory`, `IMonthlyTotalsReport`, `IStatisticsReport`, `IClock` into the Infra query implementation; build a provider, call the existing reports; derive the metric from their outputs.
**Why:** guarantees savings-rate/burn-rate numbers match the existing Monthly totals and Statistics cards pixel-for-pixel; avoids a second (divergent) income/expense implementation.
**Caveat:** `MonthlyTotalsReport.GetAsync` **throws `ApplicationException("No transactions found")` when the provider has zero transactions** [VERIFIED: MonthlyTotalsReport.cs line 28-31]. `StatisticsReport` already swallows this and returns 0 [VERIFIED: StatisticsReport.cs line 209-213]. New implementations must do the same and return an empty/zero DTO so the UI renders the D-empty states (success criterion 4).

### Pattern 3: Line-chart gap for zero-income months (D-02)
**What:** `LineSeries<ObservablePoint>.EnableNullSplitting` (default `true`) splits the line at null points; `ObservablePoint` accepts nullable X/Y.
**Source:** Vendored `LiveChartsCore.xml` doc comments: *"Gets or sets a value indicating whether the line should split every null point, enabling it has a performance impact, default is true."* [VERIFIED: ~/.nuget/packages/livechartscore/2.0.2/lib/netstandard2.0/LiveChartsCore.xml]
```csharp
// Zero-income month → null Y creates the gap
values.Add(new ObservablePoint(index, null));
```

### Pattern 4: Dashboard card panel
**What:** `BurnRatePanelViewModel : DashboardPanelViewModel` exposing `RefreshAsync()`; `SetData(title, rows, icon)` / `SetError(...)`; `ReportsViewModel` subscribes to its `PropertyChanged` and mirrors `Data`/`IsLoading` into a `BurnRateData` observable — exactly like `WealthPanelViewModel`/`IndicatorsPanelViewModel` [VERIFIED: Panels/DashboardPanelViewModel.cs, ReportsViewModel.cs lines 1029-1079].
**DI:** Panel VMs are constructor-injected into `ReportsViewModel` — register `BurnRatePanelViewModel` wherever the other panel VMs are registered.

### Pattern 5: Chart-data wrapper
**What:** `SavingsRateChartData : IDisposable` mirrors `MonthlyTotalsChartData` — static SKColor palette constants, `RefreshChart(data)` clears + rebuilds series (dispose previous paints first — see the comment in `MonthlyTotalsChartData.RefreshChart` about stale geometry after window resize) [VERIFIED: MonthlyTotalsChartData.cs lines 139-190]. `FixedVsVariableChartData` uses `StackedColumnSeries<double>` with two named series (`Fixed`, `Variable`).
**Anti-gotcha:** `ReportsViewModel.Dispose()` must dispose the new chart-data classes alongside the existing ones (line 1111-1114).

### Pattern 6: RowItem foreground extension (required by UI-SPEC)
**What:** `RowItem` currently has no color/foreground property [VERIFIED: RowItem.cs]; `DashboardDataUserControl.axaml` hardcodes `Foreground="{DynamicResource Text100Brush}"` on `RightTextBlock`. The burn-rate "vs median" row needs green/red. Add `IBrush? RightTextForeground` to `RowItem` and bind it (fallback to `Text100Brush`), sourcing brushes from `TransactionGridResources.Credit`/`Debt` (#78DB55 / #FF7D7D per UI-SPEC).

### Anti-Patterns to Avoid
- **Recomputing income/expense semantics in a second aggregator:** SpendingEvolution's direct-LiteDB aggregation is correct for its own metric, but using it for savings rate risks divergence from the Monthly totals panel (D-01 forbids this). Reuse `IMonthlyTotalsReport`.
- **Computing a new median for burn rate:** D-13 mandates reuse of `IStatisticsReport.MedianMonthlyExpenses` (which applies the statistics excluded-categories config — see Open Questions).
- **Rendering an empty fixed/variable chart:** D-10/UI-SPEC require the `Border.hint-box` empty state instead.
- **Hardcoded hex in AXAML:** all brushes via `{DynamicResource *Brush}`; SKColor hex constants live only in chart-data classes (mirroring `MonthlyTotalsChartData`).
- **`[BsonField]`/`[JsonPropertyName]` attributes on domain classes:** AGENTS.md forbids; this phase likely adds no domain types at all (metrics are derived), but if a domain helper emerges, keep it attribute-free.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Per-month income/expense | New LiteDB aggregation | `IMonthlyTotalsReport` | D-01/D-14; multi-currency + BTC-account semantics already encoded |
| 12-month expense median | Custom median | `IStatisticsReport.MedianMonthlyExpenses` | D-13; median edge cases (even/odd count, rounding) already solved |
| Currency conversion | Manual rate math | `ICurrencyConversionService` / `IReportDataProvider.GetFiatRateAt` | source→USD→target chain with fallbacks |
| Line-gap rendering | Skipping points / custom drawing | `EnableNullSplitting` + null `ObservablePoint` | Built-in, verified in vendored docs |
| Dashboard card chrome | New card control | `DashboardData`/`RowItem`/`DashboardGridPanel` | Consistent look, `IsLarge` row-span, stale styles |
| Loading/empty/error states | New overlay system | `Is*Loading` flags + `SetError` + `Border.hint-box` | Established per-panel pattern |

**Key insight:** This phase is 90% reuse; the only genuinely new computation is the per-month fixed/variable split and three small DTO-shaping derivations. Any plan task that writes new income/expense aggregation logic is a red flag.

## Common Pitfalls

### Pitfall 1: `MonthlyTotalsReport` throws on empty provider
**What goes wrong:** Calling it with no transactions throws `ApplicationException("No transactions found")`; success criterion 4 demands graceful empty states.
**How to avoid:** Mirror `StatisticsReport.CalculateMedianExpensesForPeriodAsync` — check `provider.AllTransactions.Count == 0` first and/or catch `ApplicationException`, returning an empty DTO that the UI maps to the copy in the UI-SPEC ("No savings data yet", etc.).
**Warning signs:** Unhandled exception in logs; `Is*Loading` stuck true (each fetch must clear the flag in `finally`, as existing fetches do).

### Pitfall 2: "Complete months only" leaks into the UI (D-04/D-09)
**What goes wrong:** The display date range typically ends at Dec 31 (default filter is full current year), so the current incomplete month appears with a partial, misleading value.
**How to avoid:** In the query/handler, drop months >= first-of-current-month (via injected `IClock`) before returning. Tests must use `FakeClock`.
**Warning signs:** Savings-rate line's last point jumps when the month rolls over.

### Pitfall 3: Burn-rate day-of-month boundary (D-15)
**What goes wrong:** Projection shown on days 1–4 (noise) or the note row missing.
**How to avoid:** Compute `dayOfMonth` from `IClock.GetCurrentLocalDate()`; emit projection rows only when `>= 5`; otherwise emit the two base rows plus the `Projection available from day 5` note row. Test both branches with `FakeClock` (e.g., day 3 vs day 15).

### Pitfall 4: Fixed/variable uses planned amounts instead of bound transactions (D-05/D-06)
**What goes wrong:** Summing `FixedExpenseRange` amounts or counting `ManuallyPaid` records inflates "fixed".
**How to avoid:** Join `budget_fixedexpenserecords` on `FixedExpenseRecordStateId == (int)FixedExpenseRecordState.Paid` **and** `Transaction != null`; sum the *transaction's* expense amount (converted), never the range. `FixedExpenseRecordEntity.FixedExpenseRecordStateId` is an int (`st` field) [VERIFIED: FixedExpenseRecordEntity.cs].

### Pitfall 5: Sign conventions
**What goes wrong:** Expenses are stored negative (`FromFiatAmount < 0` is the expense predicate in both `StatisticsReport` and `SpendingEvolutionQueries`); savings rate = (income − |expenses|) / income; burn spend = |AllExpensesInFiat|.
**How to avoid:** `StatisticsReport` uses `Math.Abs(item.AllExpensesInFiat)` [VERIFIED: line 197]. Reuse that convention; unit-test a month with expenses > income → negative savings rate renders `-25%` (D-03).

### Pitfall 6: Series reuse leaves stale geometry
**What goes wrong:** Reusing `LineSeries` instances across `RefreshChart` calls leaves stale line paths after window resize (documented in `MonthlyTotalsChartData`).
**How to avoid:** Copy the `DisposeSeries()` + recreate-series pattern verbatim.

### Pitfall 7: Filter semantics drift (D-18 ambiguity)
**What goes wrong:** `MonthlyTotalsReport` has **no account filter** (only `excludedCategoryIds`), while `ExpensesByCategory` has both. D-18 says "like MonthlyTotals and ExpensesByCategory already do" — these differ.
**How to avoid:** See Open Questions Q1. Recommended default: savings-rate + fixed/variable follow `MonthlyTotals` semantics (date range only), burn rate is filter-independent like Statistics. Planner should confirm in plan or ask.

## Code Examples

Verified patterns from the codebase:

### Savings-rate derivation (from MonthlyTotalsData)
```csharp
// Source: derived from src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsData.cs
//   + StatisticsReport.cs lines 195-207 (Math.Abs convention) [VERIFIED: codebase]
foreach (var item in monthlyTotalsData.Items)
{
    if (item.MonthYear >= firstOfCurrentMonth) continue;      // D-04
    var income = item.AllIncomeInFiat;
    if (income <= 0) { months.Add(new(month, null)); continue; } // D-02 → null → chart gap
    var expenses = Math.Abs(item.AllExpensesInFiat);           // sign convention
    var rate = Math.Round((income - expenses) / income * 100, 2); // D-03: keep sign
    months.Add(new(month, rate));
}
```

### Burn-rate derivation (D-12/D-13/D-15)
```csharp
// Source: pattern from StatisticsReport.GetAsync date math [VERIFIED: codebase lines 25-35]
var today = _clock.GetCurrentLocalDate();
var mtdRange = new DateOnlyRange(new DateOnly(today.Year, today.Month, 1), today);
var mtd = await _monthlyTotalsReport.GetAsync(today, mtdRange, currency, provider);
var spentMtd = Math.Abs(mtd.Total.AllExpensesInFiat);          // D-14
var avgDaily = spentMtd / today.Day;                           // D-12
var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
decimal? projected = today.Day >= 5 ? avgDaily * daysInMonth : null;  // D-15
var stats = await _statisticsReport.GetAsync(currency, wealth, provider, excluded);
var median = stats.MedianMonthlyExpenses.Value;                // D-13
decimal? vsMedian = projected.HasValue && median > 0
    ? Math.Round((projected.Value - median) / median * 100, 2)
    : null;
```

### Fixed vs variable query skeleton (SpendingEvolution pattern + records join)
```csharp
// Source: adapted from src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs
//   + FixedExpenseRecordEntity.cs [VERIFIED: codebase]
var paidTransactionIds = _localDatabase.GetFixedExpenseRecords()
    .Query()
    .Where(r => r.FixedExpenseRecordStateId == (int)FixedExpenseRecordState.Paid
                && r.Transaction != null)
    .ToList()
    .Select(r => r.Transaction!.Id)
    .ToHashSet();
// Then per expense transaction in range (SpendingEvolution aggregation):
//   fixed = paidTransactionIds.Contains(t.Id) ? amount : 0; variable = amount - fixed;
// D-08: months with expenses but no bound records fall out naturally as 100% variable.
// D-10: _localDatabase.GetFixedExpenses().Count() == 0 → return HasNoFixedExpenses flag
//        so the UI renders the hint-box instead of the chart.
```

### Chart-data skeleton (line with gaps)
```csharp
// Source: mirrors src/Valt.UI/Views/Main/Tabs/Reports/MonthlyTotalsChartData.cs [VERIFIED: codebase]
private LineSeries<ObservablePoint> CreateSavingsRateSeries() => new()
{
    Name = "Savings rate",
    Values = RateValues,
    Stroke = new SolidColorPaint(SKColor.Parse("#e98805")) { StrokeThickness = 2.5f },
    GeometryFill = new SolidColorPaint(SKColor.Parse("#ffcc88")),
    GeometryStroke = new SolidColorPaint(SKColor.Parse("#ffa122")),
    GeometrySize = 8,
    Fill = new SolidColorPaint(SKColor.Parse("#ffa122").WithAlpha(40)),
    LineSmoothness = 0.3
    // EnableNullSplitting defaults to true → zero-income months (null Y) render as gaps [VERIFIED: LiveChartsCore.xml]
};
// Y-axis Labeler: value => $"{value:F0}%"; X-axis "MMM yyyy", LabelsRotation = -45
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Direct Infra service calls from ViewModels | App-layer CQRS (`IQueryDispatcher`) for new features | v0.5+ (SpendingEvolution, Assets modules) | New panels must dispatch queries, not call `I*Report` from the VM directly... **however** existing Reports panels call `I*Report` directly (legacy). Planner must decide: dispatch new queries via `_queryDispatcher` (already injected in `ReportsViewModel`, line 76/199) while the report reuse stays inside Infra implementations. |
| Hardcoded strings then resx later (Phases 30→31) | resx keys added English-only immediately, pt-BR/es deferred | D-21 + UI-SPEC copywriting contract | Add keys to `language.resx` + `language.Designer.cs` only this phase |

**Deprecated/outdated:**
- AGENTS.md ".NET 9" claim — csprojs target `net10.0` [VERIFIED: Valt.UI.csproj]. Follow the csproj.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | New module named `SpendingAnalytics` (single module, three queries) rather than extending `SpendingEvolution` or three separate modules | Architecture Patterns | Low — naming/structure only; planner can rename |
| A2 | Savings rate uses `AllIncomeInFiat`/`AllExpensesInFiat` (includes BTC-account flows converted) rather than fiat-only `Income`/`Expenses` | Code Examples | Medium — D-01 says "same values MonthlyTotalsReport computes" without naming fields; `StatisticsReport` uses the `All*` variants, which is the strongest precedent. Planner should confirm in plan |
| A3 | Burn rate's median reuse includes the statistics excluded-categories config (`GetStatisticsExcludedCategoryIds`) because it goes through `IStatisticsReport.GetAsync` | Burn-rate example | Low — that IS what "reuse StatisticsReport's median" (D-13) means; flagged for transparency |
| A4 | `StackedColumnSeries<double>` is the correct LiveCharts type for per-month stacked bars | UI-SPEC/Patterrn 5 | Low — standard LiveCharts type; verify during implementation against vendored 2.0.2 |

## Open Questions

1. **Filter semantics per panel (D-18 is ambiguous)**
   - What we know: `MonthlyTotals` honors date range only (no account/category filter); `ExpensesByCategory` honors all three. D-18 references both.
   - What's unclear: Should savings-rate/fixed-variable react to the category filter? Burn rate is a current-month gauge — filters don't naturally apply.
   - Recommendation: default to MonthlyTotals semantics (date range, `FilterRange`) for the two monthly charts; burn rate filter-independent (like Statistics). Planner should state this explicitly in the plan; if the user wants category filtering, the query DTOs already carry the arrays (SpendingEvolution precedent).

2. **Where the new queries' date range comes from for savings rate/fixed-variable**
   - What we know: `FilterRange` drives MonthlyTotals; default = full current year.
   - Recommendation: reuse `FilterRange` and trigger refetch in `OnFilterRangeChanged` alongside `IsMonthlyTotalsLoading` (pattern at ReportsViewModel lines 457-469).

3. **Dashboard card count parity / grid fit (discretion area)**
   - Burn rate card adds a 9th card to `DashboardGridPanel`; placement after Statistics is the UI-SPEC default. No research blocker.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | Build/test | ✓ | 10.0.101 | — |
| LiteDB | Queries | ✓ | project-managed | — |
| LiveChartsCore.SkiaSharpView.Avalonia | Charts | ✓ | 2.0.2 (vendored) | — |
| Avalonia | UI | ✓ | 12.x (project-managed) | — |

**Missing dependencies:** none. No external services involved (all metrics computed from local data).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | NUnit (+ NSubstitute), project `tests/Valt.Tests/Valt.Tests.csproj` (net10.0) |
| Config file | none — standard NUnit attributes |
| Quick run command | `dotnet test --filter "FullyQualifiedName~SpendingAnalytics"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SPA-01 | rate = (income − \|expenses\|) / income; zero-income → null/gap; negative shown; current month excluded | unit (`DatabaseTest` + seeded accounts/transactions/rates, `FakeClock`) | `dotnet test --filter "FullyQualifiedName~SavingsRate"` | ❌ Wave 0 |
| SPA-02 | avg daily, projection, day-≥5 gate, median reuse, empty month | unit | `dotnet test --filter "FullyQualifiedName~BurnRate"` | ❌ Wave 0 |
| SPA-03 | Paid-only fixed split; ManuallyPaid/Ignored/Empty excluded; D-08 100% variable; D-10 none-registered flag | unit | `dotnet test --filter "FullyQualifiedName~FixedVsVariable"` | ❌ Wave 0 |
| — | Handler validation/dispatch smoke | integration (`IntegrationTest` base) | covered by query tests | ❌ Wave 0 |
| — | ReportsViewModel wiring (new panels fetch + loading flags) | unit (existing `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` precedent) | `dotnet test --filter "FullyQualifiedName~ReportsViewModel"` | partial — extend existing |

Existing test infrastructure to copy: `tests/Valt.Tests/Reports/StatisticsReportTests.cs` (DatabaseTest + FakeClock + seeded price data), `MonthlyTotalsReportTests.cs` (TransactionBuilder seeding), Builders: `FixedExpenseBuilder`, `FiatAccountBuilder`, `TransactionBuilder`. Note: no existing builder for `FixedExpenseRecordEntity` — Wave 0 may seed records directly via `_localDatabase.GetFixedExpenseRecords().Insert(...)` or add a small builder.

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~SpendingAnalytics"`
- **Per wave merge:** `dotnet test`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `tests/Valt.Tests/Application/SpendingAnalytics/` (or `tests/Valt.Tests/Reports/`) — query tests for all three metrics
- [ ] Fixed-expense-record seeding helper (builder or inline inserts)
- [ ] Extend `ReportsViewModelTests` for new panel wiring (optional but recommended)

## Security Domain

Read-only analytics over local data; no new input surface, no network, no secrets. Applied ASVS categories are minimal:

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | — |
| V3 Session Management | no | — |
| V4 Access Control | no | — |
| V5 Input Validation | yes (weak) | Query DTOs accept account/category ID strings from UI filter state only (no free-text user input); LiteDB queries use parameterized `.Query()` APIs (SpendingEvolution precedent) — no string-built queries |
| V6 Cryptography | no | — |
| V12 Files/Resources | no | — |

### Known Threat Patterns for {stack}
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Division by zero in rate math | Tampering/DoS (crash) | D-02 null-skip; `median > 0` guard before vs-median % |
| Sensitive values visible on screen | Information disclosure | Existing tab-level secure-mode overlay covers new panels (UI-SPEC Interaction Contract) — no per-panel handling |
| Exception leaking DB internals to UI | Information disclosure | `SetError` renders `ex.Message` (existing convention) — acceptable for local single-user desktop app |

## Sources

### Primary (HIGH confidence)
- Codebase (read in-session): `MonthlyTotalsReport.cs`, `MonthlyTotalsData.cs`, `StatisticsReport.cs`, `SpendingEvolutionQueries.cs`, `GetSpendingEvolutionQuery.cs`, `FixedExpenseRecordEntity.cs`, `ReportsViewModel.cs`, `DashboardPanelViewModel.cs`, `MonthlyTotalsChartData.cs`, `RowItem.cs`, `DashboardData.cs`, `DashboardDataUserControl.axaml`, `TransactionGridResources.cs`, `Valt.App/Extensions.cs`, `Valt.Infra/Extensions.cs`, `.claude/docs/reports.md`, `.claude/docs/fixedexpenses.md`
- Vendored package docs: `~/.nuget/packages/livechartscore/2.0.2/lib/netstandard2.0/LiveChartsCore.xml` — `EnableNullSplitting` + nullable `ObservablePoint`
- `dotnet --version` → 10.0.101; csproj `net10.0`

### Secondary (MEDIUM confidence)
- UI-SPEC (39-UI-SPEC.md) — binding design contract, checker-approved

### Tertiary (LOW confidence)
- None — no unverified external claims. LiveCharts online docs were unreachable (404/timeout), so the null-gap claim was verified against the vendored XML docs instead.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; all versions verified on disk
- Architecture: HIGH — every pattern cited to a file read in-session; one naming assumption (A1) and one field-choice assumption (A2) flagged
- Pitfalls: HIGH — each pitfall traces to code read in-session

**Research date:** 2026-08-04
**Valid until:** 2026-09-03 (stable domain; codebase-local research)
