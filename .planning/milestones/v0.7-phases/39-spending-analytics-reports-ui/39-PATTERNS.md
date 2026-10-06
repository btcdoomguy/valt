# Phase 39: Spending Analytics Reports & UI - Pattern Map

**Mapped:** 2026-08-04
**Files analyzed:** 22 (15 new, 7 modified)
**Analogs found:** 22 / 22

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateQuery.cs` | query | request-response | `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionQuery.cs` | exact |
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetSavingsRateHandler.cs` | query handler | request-response | `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionHandler.cs` | exact |
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetBurnRateQuery.cs` (+Handler) | query + handler | request-response | `GetSpendingEvolutionQuery.cs` / `GetSpendingEvolutionHandler.cs` | exact |
| `src/Valt.App/Modules/SpendingAnalytics/Queries/GetFixedVsVariableQuery.cs` (+Handler) | query + handler | request-response | `GetSpendingEvolutionQuery.cs` / `GetSpendingEvolutionHandler.cs` | exact |
| `src/Valt.App/Modules/SpendingAnalytics/Contracts/ISavingsRateQueries.cs` (+ `IBurnRateQueries`, `IFixedVsVariableQueries`) | contract | request-response | `src/Valt.App/Modules/SpendingEvolution/Contracts/ISpendingEvolutionQueries.cs` | exact |
| `src/Valt.App/Modules/SpendingAnalytics/DTOs/*.cs` (4 DTOs) | DTO | request-response | `SpendingEvolutionDataDto.cs` / `SpendingEvolutionMonthDto.cs` | exact |
| `src/Valt.Infra/Modules/SpendingAnalytics/Queries/SavingsRateQueries.cs` | infra query impl | CRUD read | `StatisticsReport.cs` (report reuse) + `SpendingEvolutionQueries.cs` (shell) | role-match |
| `src/Valt.Infra/Modules/SpendingAnalytics/Queries/BurnRateQueries.cs` | infra query impl | CRUD read | `StatisticsReport.cs` (median reuse, empty handling) | role-match |
| `src/Valt.Infra/Modules/SpendingAnalytics/Queries/FixedVsVariableQueries.cs` | infra query impl | CRUD read + join | `SpendingEvolutionQueries.cs` (direct LiteDB aggregation) | exact |
| `src/Valt.UI/Views/Main/Tabs/Reports/SavingsRateChartData.cs` | chart-data wrapper | transform | `MonthlyTotalsChartData.cs` | exact |
| `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs` | chart-data wrapper | transform | `MonthlyTotalsChartData.cs` + `ExpensesByCategoryChartData.cs` | role-match |
| `src/Valt.UI/Views/Main/Tabs/Reports/Panels/BurnRatePanelViewModel.cs` | panel viewmodel | request-response | `Panels/BtcLoans/BtcLoansPanelViewModel.cs` (dispatches IQueryDispatcher) | exact |
| `tests/Valt.Tests/Reports/SavingsRateQueriesTests.cs` | test | CRUD read | `tests/Valt.Tests/Reports/StatisticsReportTests.cs` | exact |
| `tests/Valt.Tests/Reports/BurnRateQueriesTests.cs` | test | CRUD read | `tests/Valt.Tests/Reports/StatisticsReportTests.cs` | exact |
| `tests/Valt.Tests/Reports/FixedVsVariableQueriesTests.cs` | test | CRUD read + join | `tests/Valt.Tests/Reports/MonthlyTotalsReportTests.cs` + inline record inserts | role-match |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` (modify) | tab viewmodel | request-response | self — existing fetch/wiring sections | self |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` (modify) | view | request-response | self — existing card/chart blocks | self |
| `src/Valt.UI/Views/Main/Tabs/Reports/RowItem.cs` (modify) | model | — | self (add `RightTextForeground`) | self |
| `src/Valt.UI/Views/Main/Tabs/Reports/DashboardDataUserControl.axaml` (modify) | view | — | self (bind new foreground, line 121) | self |
| `src/Valt.Infra/Extensions.cs` (modify) | config/DI | — | line 278 `ISpendingEvolutionQueries` registration | self |
| `src/Valt.UI/Lang/language.resx` + `language.Designer.cs` (modify) | localization | — | existing `Reports_*` keys (English-only per D-21) | self |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.DesignTime.cs` (modify, optional) | design-time VM | — | existing `RefreshChart` sample calls (line 299-304) | self |

## Pattern Assignments

### `GetSavingsRateQuery.cs` / `GetBurnRateQuery.cs` / `GetFixedVsVariableQuery.cs` (query)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionQuery.cs` (entire file, 12 lines)

**Query record pattern:**
```csharp
using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingEvolution.DTOs;

namespace Valt.App.Modules.SpendingEvolution.Queries;

public record GetSpendingEvolutionQuery : IQuery<SpendingEvolutionDataDto>
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string[] CategoryIds { get; init; } = Array.Empty<string>();
    public string[] AccountIds { get; init; } = Array.Empty<string>();
}
```
Copy verbatim; rename namespace to `Valt.App.Modules.SpendingAnalytics.Queries`. Burn rate query may omit From/To (current-month gauge, D-11) but keep the filter arrays per Open Question Q1.

---

### `GetSavingsRateHandler.cs` (+ the other two handlers)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionHandler.cs` (entire file, 20 lines)

**Handler pass-through pattern:**
```csharp
using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingEvolution.Contracts;
using Valt.App.Modules.SpendingEvolution.DTOs;

namespace Valt.App.Modules.SpendingEvolution.Queries;

internal sealed class GetSpendingEvolutionHandler : IQueryHandler<GetSpendingEvolutionQuery, SpendingEvolutionDataDto>
{
    private readonly ISpendingEvolutionQueries _spendingEvolutionQueries;

    public GetSpendingEvolutionHandler(ISpendingEvolutionQueries spendingEvolutionQueries)
    {
        _spendingEvolutionQueries = spendingEvolutionQueries;
    }

    public Task<SpendingEvolutionDataDto> HandleAsync(GetSpendingEvolutionQuery query, CancellationToken ct = default)
    {
        return _spendingEvolutionQueries.GetSpendingEvolutionAsync(query);
    }
}
```
Note: `internal sealed` — works because `Valt.App/Extensions.cs` line 7 declares `[assembly: InternalsVisibleTo("Valt.Tests")]`. Handlers are auto-registered by the Scrutor scan (see Shared Patterns → DI).

---

### `Contracts/ISavingsRateQueries.cs` (+ `IBurnRateQueries`, `IFixedVsVariableQueries`)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/Contracts/ISpendingEvolutionQueries.cs` (entire file, 9 lines)

```csharp
using Valt.App.Modules.SpendingEvolution.DTOs;
using Valt.App.Modules.SpendingEvolution.Queries;

namespace Valt.App.Modules.SpendingEvolution.Contracts;

public interface ISpendingEvolutionQueries
{
    Task<SpendingEvolutionDataDto> GetSpendingEvolutionAsync(GetSpendingEvolutionQuery query);
}
```

---

### `DTOs/*.cs` (SavingsRateDataDto + MonthDto, BurnRateDataDto, FixedVsVariableDataDto + MonthDto)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionDataDto.cs` and `SpendingEvolutionMonthDto.cs` (entire files)

**DTO pattern — records with `required init`:**
```csharp
namespace Valt.App.Modules.SpendingEvolution.DTOs;

public record SpendingEvolutionDataDto
{
    public required IReadOnlyList<SpendingEvolutionMonthDto> Months { get; init; }
    public required bool HasMissingPriceInSats { get; init; }
    public required string PrimaryCurrency { get; init; }
}

public record SpendingEvolutionMonthDto
{
    public required DateOnly Month { get; init; }
    public required decimal FiatTotal { get; init; }
    public required long SatsTotal { get; init; }
    public required int TransactionCount { get; init; }
}
```
For savings rate: `SavingsRateMonthDto { DateOnly Month; decimal? Rate }` — nullable `Rate` encodes the D-02 zero-income gap (maps to null `ObservablePoint` in the chart). For fixed/variable: add a `bool HasNoFixedExpenses` flag on the data DTO to drive the D-10 hint-box empty state.

---

### `src/Valt.Infra/Modules/SpendingAnalytics/Queries/SavingsRateQueries.cs` + `BurnRateQueries.cs` (infra impl reusing reports)

**Analog 1 (shell/DI shape):** `src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs` lines 15-32

**Constructor injection pattern:**
```csharp
public class SpendingEvolutionQueries : ISpendingEvolutionQueries
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ICurrencyConversionService _currencyConversionService;
    private readonly CurrencySettings _currencySettings;

    public SpendingEvolutionQueries(
        ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        ICurrencyConversionService currencyConversionService,
        CurrencySettings currencySettings)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _currencyConversionService = currencyConversionService;
        _currencySettings = currencySettings;
    }
```
For savings/burn rate, inject instead: `IReportDataProviderFactory`, `IMonthlyTotalsReport`, `IStatisticsReport`, `IClock`, `CurrencySettings` (all registered — see Shared Patterns → DI).

**Analog 2 (report reuse + empty handling):** `src/Valt.Infra/Modules/Reports/Statistics/StatisticsReport.cs` lines 184-213

**Empty-provider guard + `Math.Abs` sign convention + median-reuse pattern:**
```csharp
// Get 12 months range ending before the reference month (excluding reference month since it may not be complete)
var endOfLastMonth = new DateOnly(referenceDate.Year, referenceDate.Month, 1).AddDays(-1);
var startOf12MonthsAgo = endOfLastMonth.AddMonths(-11);
startOf12MonthsAgo = new DateOnly(startOf12MonthsAgo.Year, startOf12MonthsAgo.Month, 1);

var displayRange = new DateOnlyRange(startOf12MonthsAgo, endOfLastMonth);

try
{
    var monthlyData = await _monthlyTotalsReport.GetAsync(referenceDate, displayRange, currency, provider, excludedCategoryIds);

    // Get the absolute values of expenses for each month (expenses are stored as negative)
    var monthlyExpenses = monthlyData.Items
        .Select(item => Math.Abs(item.AllExpensesInFiat))
        .Where(expense => expense > 0) // Only include months with actual expenses
        .OrderBy(x => x)
        .ToList();

    if (monthlyExpenses.Count == 0)
    {
        return 0m;
    }

    return CalculateMedian(monthlyExpenses);
}
catch (ApplicationException)
{
    // No transactions found
    return 0m;
}
```
Critical: `MonthlyTotalsReport.GetAsync` **throws** `ApplicationException("No transactions found")` when the provider is empty (`MonthlyTotalsReport.cs` lines 28-31). Copy this try/catch and return an empty DTO so the UI renders the empty states (success criterion 4). Also copy the "exclude current incomplete month" date math (D-04/D-09) — `new DateOnly(today.Year, today.Month, 1).AddDays(-1)` as the range end, using `IClock.GetCurrentLocalDate()` (`StatisticsReport.cs` line 25).

**StatisticsReport constructor (median-reuse dependency for BurnRateQueries):** `StatisticsReport.cs` lines 8-17
```csharp
internal class StatisticsReport : IStatisticsReport
{
    private readonly IClock _clock;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;

    public StatisticsReport(IClock clock, IMonthlyTotalsReport monthlyTotalsReport)
```
`BurnRateQueries` should call `IStatisticsReport.GetAsync(...)` and read `MedianMonthlyExpenses` (D-13) — note its signature needs `currentWealthInFiat`; the ReportsViewModel sources it from `_accountsTotalState.CurrentWealth.AllWealthInMainFiatCurrency` (`ReportsViewModel.cs` line 631), so the burn-rate query DTO may need that value passed in, or the infra impl injects the state. Planner decision — flag it.

---

### `src/Valt.Infra/Modules/SpendingAnalytics/Queries/FixedVsVariableQueries.cs` (direct LiteDB)

**Analog:** `src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs` lines 34-101 (query build + monthly aggregation)

**LiteDB-side filtering pattern:**
```csharp
// Build transaction query with LiteDB-side filtering
var transactionQuery = _localDatabase.GetTransactions().Query();

// Filter by date range
var fromDate = query.From.ToValtDateTime();
var toDate = query.To.ToValtDateTime();
transactionQuery = transactionQuery.Where(x => x.Date >= fromDate && x.Date <= toDate);

// Filter by category IDs if provided
if (query.CategoryIds.Length > 0)
{
    var categoryObjectIds = query.CategoryIds.Select(id => new ObjectId(id)).ToList();
    transactionQuery = transactionQuery.Where(x => categoryObjectIds.Contains(x.CategoryId));
}

// Filter by selected accounts
if (selectedAccountIds.Count < allAccountsList.Count)
{
    transactionQuery = transactionQuery.Where(x => selectedAccountIds.Contains(x.FromAccountId));
}

// Include all debit transactions (spending): fiat expenses OR bitcoin expenses with auto-calculated sats
transactionQuery = transactionQuery.Where(x =>
    (x.FromFiatAmount.HasValue && x.FromFiatAmount.Value < 0) ||
    (x.SatAmount.HasValue && x.FromSatAmount.HasValue && x.FromSatAmount.Value < 0));

// Execute query - load filtered results into memory
var transactions = transactionQuery.ToList();
```

**Monthly aggregation + currency conversion pattern (lines 113-147, 192-206):**
```csharp
var yearMonth = (transaction.Date.Year, transaction.Date.Month);
// ...
var absoluteFiat = Math.Abs(transaction.FromFiatAmount.Value);
var convertedFiat = self.ConvertToPrimaryCurrency(absoluteFiat, account.Currency, primaryCurrency, bitcoinPriceUsd, fiatRates);
current.FiatTotal += convertedFiat;
```
```csharp
private decimal ConvertToPrimaryCurrency(decimal amount, string? sourceCurrency, string targetCurrency, decimal? bitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? fiatRates)
{
    if (string.IsNullOrEmpty(sourceCurrency) || sourceCurrency == targetCurrency)
        return amount;

    try
    {
        return _currencyConversionService.Convert(amount, sourceCurrency, targetCurrency, bitcoinPriceUsd, fiatRates);
    }
    catch
    {
        // If conversion fails, return original amount
        return amount;
    }
}
```

**Fixed-record join — entity shape:** `src/Valt.Infra/Modules/Budget/FixedExpenses/FixedExpenseRecordEntity.cs` (entire file)
```csharp
public class FixedExpenseRecordEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    [BsonRef("budget_fixedexpenses")]
    public required FixedExpenseEntity FixedExpense { get; set; }
    [BsonField("dt")] public DateTime ReferenceDate { get; set; }
    [BsonRef("budget_transactions")]
    public TransactionEntity? Transaction { get; set; }
    [BsonField("st")]
    public int FixedExpenseRecordStateId { get; set; }
}
```
State enum: `src/Valt.Core/Modules/Budget/FixedExpenses/FixedExpenseRecordState.cs` — `Empty = 0, Paid = 1, ManuallyPaid = 2, Ignored = 3`. Filter `FixedExpenseRecordStateId == (int)FixedExpenseRecordState.Paid && r.Transaction != null` (D-06); sum the **transaction's** converted amount, never the fixed-expense range (D-05). D-10 check: `_localDatabase.GetFixedExpenses().Count() == 0` → return `HasNoFixedExpenses = true`.

---

### `src/Valt.UI/Views/Main/Tabs/Reports/SavingsRateChartData.cs` (line chart with null gaps)

**Analog:** `src/Valt.UI/Views/Main/Tabs/Reports/MonthlyTotalsChartData.cs` (entire file, 200 lines)

**Palette + paints pattern (lines 19-43):**
```csharp
// Color palette - Bitcoin (Orange shades from Accent)
private static readonly SKColor BtcPrimary = SKColor.Parse("#ffa122");       // Accent400
private static readonly SKColor BtcLight = SKColor.Parse("#ffcc88");         // Accent200
private static readonly SKColor BtcDark = SKColor.Parse("#e98805");          // Accent500
private static readonly SKColor BtcFill = SKColor.Parse("#ffa122").WithAlpha(40);

// Grid and text colors
private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");        // Background700
private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");        // Text400

public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
public ObservableCollection<ObservablePoint> FiatValues { get; } = new();
public ObservableCollection<string> MonthLabels { get; } = new();
```

**Series creation (lines 104-114):**
```csharp
private LineSeries<ObservablePoint> CreateFiatSeries() => new()
{
    Name = "Total Wealth",
    Values = FiatValues,
    Stroke = new SolidColorPaint(FiatPrimary) { StrokeThickness = 2.5f },
    GeometryStroke = new SolidColorPaint(FiatDark) { StrokeThickness = 2 },
    GeometryFill = new SolidColorPaint(FiatLight),
    GeometrySize = 8,
    Fill = new SolidColorPaint(FiatFill),
    LineSmoothness = 0.3
};
```
`EnableNullSplitting` defaults to `true` — add zero-income months as `new ObservablePoint(index, null)` for the D-02 gap. Y-axis `Labeler = value => $"{value:F0}%"`.

**RefreshChart + DisposeSeries stale-geometry pattern (lines 139-190) — copy verbatim:**
```csharp
public void RefreshChart(MonthlyTotalsData monthlyTotalsData)
{
    // ...
    // Dispose and detach the previous series so LiveCharts rebuilds the line
    // path from scratch — reusing the same series instance leaves stale geometry
    // after a window resize combined with a data refresh.
    DisposeSeries();
    Series.Clear();

    for (var index = 0; index < monthlyTotalsData.Items.Count; index++)
    {
        var item = monthlyTotalsData.Items[index];
        MonthLabels.Add(item.MonthYear.ToString("MMM yyyy", CultureInfo.InvariantCulture));
        FiatValues.Add(new ObservablePoint(index, (double)item.FiatTotal));
    }

    _fiatSeries = CreateFiatSeries();
    Series.Add(_fiatSeries);
}

private void DisposeSeries()
{
    if (_fiatSeries is not null)
    {
        (_fiatSeries.Stroke as IDisposable)?.Dispose();
        (_fiatSeries.GeometryStroke as IDisposable)?.Dispose();
        (_fiatSeries.GeometryFill as IDisposable)?.Dispose();
        (_fiatSeries.Fill as IDisposable)?.Dispose();
        _fiatSeries = null;
    }
    // ...
}
```

---

### `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs` (stacked column chart)

**Analog:** `MonthlyTotalsChartData.cs` (axes, month labels, dispose pattern) + `ExpensesByCategoryChartData.cs` lines 110-191 (single-series refresh + dispose for non-line series)

**Stacked series:** use `StackedColumnSeries<double>` with two named series (`Fixed`, `Variable`), one entry per month. Reuse the X-axis month-label pattern from `MonthlyTotalsChartData` lines 59-71:
```csharp
XAxes[0] =
    new Axis
    {
        ForceStepToMin = true,
        MinStep = 1,
        SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(60)) { StrokeThickness = 1 },
        LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
        TextSize = 12,
        Position = AxisPosition.End,
        Labels = MonthLabels,
        LabelsRotation = -45,
        MinZoomDelta = 1
    };
```
Dispose pattern for column series mirrors `ExpensesByCategoryChartData.DisposeSeries()` lines 181-191 (`(series.Fill as IDisposable)?.Dispose()` per series).

---

### `src/Valt.UI/Views/Main/Tabs/Reports/Panels/BurnRatePanelViewModel.cs` (dashboard card)

**Analog:** `src/Valt.UI/Views/Main/Tabs/Reports/Panels/BtcLoans/BtcLoansPanelViewModel.cs` (entire file, 145 lines) — the freshest panel VM that dispatches an App-layer query via `IQueryDispatcher` (unlike legacy panels calling `I*Report` directly).

**Imports + constructor pattern (lines 1-45):**
```csharp
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Queries.GetBtcLoansDashboard;
// ...
public partial class BtcLoansPanelViewModel : DashboardPanelViewModel, IBtcLoansPanelViewModel
{
    private readonly IQueryDispatcher _queryDispatcher;
    // ...
    public BtcLoansPanelViewModel(
        IQueryDispatcher queryDispatcher,
        // ...
        ILogger<BtcLoansPanelViewModel> logger)
        : base(logger)
```

**RefreshAsync dispatch + rows + error/finally pattern (lines 47-139):**
```csharp
public override async Task RefreshAsync()
{
    try
    {
        IsLoading = true;
        // ...
        var dto = await _queryDispatcher.DispatchAsync(new GetBtcLoansDashboardQuery
        {
            MainCurrencyCode = mainCurrency,
            // ...
        });

        if (!dto.HasActiveLoans)   // <-- empty-state gate; burn rate: dto.HasData / day < 5 branch (D-15)
        {
            IsVisible = false;
            IsLoading = false;
            return;
        }

        var rows = new ObservableCollection<RowItem>
        {
            new(language.Reports_BtcLoans_TotalDebt, CurrencyDisplay.FormatFiat(dto.TotalDebtInMainCurrency, fiatCurrency.Code)),
            // ...
            RowItem.Separator(),
            // ...
        };

        SetData(language.Reports_BtcLoans_Title, rows, "\uE227");
        IsVisible = true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating BTC loans data");
        IsVisible = false;
    }
    finally
    {
        IsLoading = false;
    }
}

public override void Refresh()
{
    RefreshAsync().FireAndForgetSafeAsync(new FireAndForgetTaskRunner(), _logger);
}
```
For the "vs median" row needing green/red (UI-SPEC), pass `RightTextForeground: TransactionGridResources.Credit` / `.Debt` on the new `RowItem` property (see RowItem modification below). Icon unicode lookup: `src/Valt.UI/Assets/Fonts/MaterialSymbolsOutlined-map.json`.

**Base class API:** `Panels/DashboardPanelViewModel.cs` lines 43-71 — `SetData(title, rows, icon, isStale)`, `SetError(title, ex, icon)`, `SetEmpty(title, icon)`, `FormatFiat`, `FormatPercent`.

---

### `src/Valt.UI/Views/Main/Tabs/Reports/RowItem.cs` (modify — add foreground)

**Current (entire file):**
```csharp
public record RowItem(string LeftText, string RightText, Control? Tooltip = null, string? Url = null, bool IsSeparator = false)
{
    public bool HasUrl => !string.IsNullOrEmpty(Url);
    public static RowItem Separator() => new(string.Empty, string.Empty, IsSeparator: true);
}
```
Add optional `IBrush? RightTextForeground = null` positional parameter (keep it last to avoid breaking existing call sites). Brush sources: `TransactionGridResources.Credit` / `.Debt` (`src/Valt.UI/Views/Main/Tabs/Transactions/Models/TransactionGridResources.cs` lines 48-70 — static `SolidColorBrush` properties, throw if `Initialize()` not called).

---

### `src/Valt.UI/Views/Main/Tabs/Reports/DashboardDataUserControl.axaml` (modify — bind foreground)

**Current hardcoded foreground (line 117-126):**
```xml
<TextBlock x:Name="RightTextBlock"
           Text="{Binding RightText}"
           FontSize="{DynamicResource FontSizeSmall}"
           FontWeight="Medium"
           Foreground="{DynamicResource Text100Brush}"
           DockPanel.Dock="Right"
           ... />
```
Bind `Foreground` to `RightTextForeground` with a fallback to `Text100Brush` (converter or `TargetNullValue`/`FallbackValue`). Note the `.stale` style at lines 20-22 also sets `RightTextBlock` foreground — keep precedence in mind.

---

### `ReportsViewModel.cs` (modify — wiring)

**Parallel fetch pattern (lines 355-365) — add the two chart fetches + burn-rate panel refresh:**
```csharp
private async Task FetchAllReportsAsync(IReportDataProvider provider)
{
    await Task.WhenAll(
        FetchMonthlyTotalsAsync(provider),
        FetchExpensesByCategoryAsync(provider),
        FetchIncomeByCategoryAsync(provider),
        FetchAllTimeHighDataAsync(provider),
        FetchMaxBtcStackDataAsync(provider),
        FetchStatisticsDataAsync(provider),
        FetchWealthOverviewAsync(provider));
}
```

**Per-fetch try/catch/loading-flag pattern (lines 912-942):**
```csharp
private async Task FetchMonthlyTotalsAsync(IReportDataProvider provider)
{
    try
    {
        var monthlyTotalsData = await _monthlyTotalsReport.GetAsync(
            DateOnly.FromDateTime(FilterMainDate),
            new DateOnlyRange(DateOnly.FromDateTime(FilterRange.Start), DateOnly.FromDateTime(FilterRange.End)),
            FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency),
            provider);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            MonthlyTotalsChartData.RefreshChart(monthlyTotalsData);
            // ...
            IsMonthlyTotalsLoading = false;
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching monthly totals");
        await Dispatcher.UIThread.InvokeAsync(() => { IsMonthlyTotalsLoading = false; });
    }
}
```
New fetches should dispatch via `_queryDispatcher` (already injected, field line 76, ctor line 199) instead of calling `I*Report` directly — BtcLoansPanelViewModel is the precedent for query dispatch from this area.

**Filter-refetch trigger pattern (lines 457-469) — hook savings-rate/fixed-variable refetch here (Open Question Q2):**
```csharp
partial void OnFilterRangeChanged(DateRange value)
{
    if (!_ready) return;

    IsMonthlyTotalsLoading = true;
    FetchMonthlyTotalsWithProviderAsync().FireAndForgetSafeAsync(_runner, _logger);
}
```

**Panel PropertyChanged mirroring pattern (lines 1053-1059) — copy for BurnRatePanel:**
```csharp
private void OnIndicatorsPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(DashboardPanelViewModel.Data))
        IndicatorsData = _indicatorsPanel.Data;
    else if (e.PropertyName == nameof(DashboardPanelViewModel.IsLoading))
        IsIndicatorsLoading = _indicatorsPanel.IsLoading;
}
```

**Dispose pattern (lines 1088-1117) — add unsubscribe + chart-data disposal:**
```csharp
public void Dispose()
{
    // ...
    _indicatorsPanel.PropertyChanged -= OnIndicatorsPanelPropertyChanged;
    // ...
    MonthlyTotalsChartData.Dispose();
    ExpensesByCategoryChartData.Dispose();
    IncomeByCategoryChartData.Dispose();
    WealthOverviewChartData.Dispose();

    // Clear the provider on dispose
    _cachedProvider = null;
}
```

---

### `ReportsView.axaml` (modify — dashboard card + two chart sections)

**Dashboard card block pattern (lines 107-116) — copy for the Burn Rate card inside `DashboardGridPanel`:**
```xml
<!-- Statistics Dashboard -->
<Grid MinHeight="150"
      reports1:DashboardGridPanel.RowSpan="{Binding StatisticsData.IsLarge, Converter={x:Static reports1:DashboardGridPanel.IsLargeToRowSpanConverter}}">
    <Panel IsVisible="{Binding !IsStatisticsLoading}">
        <reports1:DashboardDataUserControl DataContext="{Binding StatisticsData}" />
    </Panel>
    <TextBlock Text="{x:Static lang:language.Loading}" FontSize="{DynamicResource FontSizeMedium}" Foreground="{DynamicResource Text400Brush}"
               HorizontalAlignment="Center" VerticalAlignment="Center"
               IsVisible="{Binding IsStatisticsLoading}" />
</Grid>
```

**Chart section pattern (lines 189-213) — copy for savings-rate and fixed/variable charts:**
```xml
<Grid MinHeight="500">
    <Border Background="{DynamicResource Background900Brush}"
            BorderBrush="{DynamicResource Background700Brush}"
            BorderThickness="1"
            CornerRadius="4"
            Padding="10"
            IsVisible="{Binding !IsWealthOverviewLoading}">
        <avalonia:CartesianChart
            x:Name="WealthOverviewChart"
            Background="{DynamicResource Background900Brush}"
            Series="{Binding WealthOverviewChartData.Series}"
            XAxes="{Binding WealthOverviewChartData.XAxes}"
            YAxes="{Binding WealthOverviewChartData.YAxes}"
            LegendPosition="Top"
            LegendTextSize="13"
            TooltipTextSize="12"
            ZoomMode="None"
            Height="480"
            Margin="20, 10, 20, 10">
        </avalonia:CartesianChart>
    </Border>
    <TextBlock Text="{x:Static lang:language.Loading}" ...
               IsVisible="{Binding IsWealthOverviewLoading}" />
</Grid>
```
Sections live in `Expander Classes="section"` with a `section-header-icon` + `section-header` header (lines 221-227). For the D-10 fixed/variable empty state, add a `Border Classes="hint-box"` sibling (style at `src/Valt.UI/Styles/DefaultStyles.axaml` line 1595; usage example `src/Valt.UI/Views/Main/Modals/FixedExpenseEditor/FixedExpenseEditorView.axaml` line 267).

---

### Tests (`tests/Valt.Tests/Reports/SavingsRate*Tests.cs`, `BurnRate*Tests.cs`, `FixedVsVariable*Tests.cs`)

**Analog:** `tests/Valt.Tests/Reports/StatisticsReportTests.cs` lines 18-84 + `MonthlyTotalsReportTests.cs` lines 15-66

**Fixture + seed pattern:**
```csharp
[TestFixture]
public class StatisticsReportTests : DatabaseTest
{
    private AccountEntity _brlAccount = null!;
    private CategoryId _categoryId = null!;

    protected override Task SeedDatabase()
    {
        _categoryId = IdGenerator.Generate();

        _brlAccount = new FiatAccountBuilder()
        {
            Name = "BRL Account",
            FiatCurrency = FiatCurrency.Brl,
            Value = FiatValue.New(10000m)
        }.Build();
        _localDatabase.GetAccounts().Insert(_brlAccount);

        // Feed fake rates for 2024 and 2025
        var initialDate = new DateTime(2024, 01, 01);
        var finalDate = new DateTime(2025, 12, 31);
        var currentDate = initialDate;
        while (currentDate <= finalDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity() { Date = currentDate, Price = 100000m });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity() { Date = currentDate, Currency = FiatCurrency.Brl.Code, Price = 5.5m });
            currentDate = currentDate.AddDays(1);
        }

        return base.SeedDatabase();
    }
```

**SUT construction + assertion pattern (lines 68-84) — note direct construction with `FakeClock` + `NullLogger`, no DI:**
```csharp
[Test]
public async Task Should_Return_Zero_When_No_Transactions()
{
    var clock = new FakeClock(new DateTime(2025, 12, 31));
    var provider = new ReportDataProvider(_priceDatabase, _localDatabase, clock);
    var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
    var statisticsReport = new StatisticsReport(clock, monthlyTotalsReport);

    var result = await statisticsReport.GetAsync(FiatCurrency.Brl, 10000m, provider);

    using (Assert.EnterMultipleScope())
    {
        Assert.That(result.MedianMonthlyExpenses.Value, Is.EqualTo(0m));
    }
}
```
For FixedVsVariable tests: seed `FixedExpenseRecordEntity` directly via `_localDatabase.GetFixedExpenseRecords().Insert(...)` (no builder exists — Wave 0 gap). Use `TransactionBuilder`, `FiatAccountBuilder`, `FixedExpenseBuilder` for the rest. Test the D-15 day-≥5 gate with two `FakeClock` values (e.g., day 3 vs day 15).

---

### `src/Valt.Infra/Extensions.cs` (modify — DI registration)

**Analog (line 278 + lines 193-196):**
```csharp
services.AddSingleton<IMonthlyTotalsReport, MonthlyTotalsReport>();
services.AddSingleton<IStatisticsReport, StatisticsReport>();
services.AddSingleton<IReportDataProviderFactory, ReportDataProviderFactory>();
// ...
services.AddSingleton<ISpendingEvolutionQueries, SpendingEvolutionQueries>();
```
Add `ISavingsRateQueries`/`IBurnRateQueries`/`IFixedVsVariableQueries` singleton registrations next to line 278. Handlers need no manual registration (Scrutor scan, below).

## Shared Patterns

### DI / Auto-registration
**Source:** `src/Valt.App/Extensions.cs` lines 21-49
**Apply to:** All new handlers — nothing to do, they are auto-registered:
```csharp
// Auto-register query handlers (singleton - stateless reads)
// Includes: Budget, Assets, AvgPrice, Goals, and SpendingEvolution query handlers
services.Scan(scan => scan
    .FromAssemblyOf<AssemblyMarker>()
    .AddClasses(classes => classes.Where(type =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))), publicOnly: false)
    .AsImplementedInterfaces()
    .WithSingletonLifetime());
```
Only the three `I*Queries` infra implementations need explicit registration in `src/Valt.Infra/Extensions.cs`.

### Sign & formatting conventions
**Source:** `src/Valt.Infra/Modules/Reports/Statistics/StatisticsReport.cs` lines 195-197; `src/Valt.UI/Views/Main/Tabs/Reports/Models/MonthlyReportItemViewModel.cs` lines 59-75
**Apply to:** SavingsRateQueries (rate math), BurnRateQueries (spend), BurnRatePanelViewModel (row formatting)
```csharp
// Expenses stored negative — always Math.Abs before use:
.Select(item => Math.Abs(item.AllExpensesInFiat))

// +/- percent formatting with Credit/Debt color (D-03):
public string BtcMonthlyChangeFormatted => BtcMonthlyChange is not null
    ? BtcMonthlyChange >= 0 ? $"+{BtcMonthlyChange}%" : $"{BtcMonthlyChange}%"
    : string.Empty;

public SolidColorBrush BtcMonthlyChangeColor => BtcMonthlyChange is not null
    ? Process(BtcMonthlyChange.Value)
    : TransactionGridResources.Credit;
```

### Currency conversion chain
**Source:** `SpendingEvolutionQueries.cs` lines 149-206
**Apply to:** `FixedVsVariableQueries` only (savings/burn inherit conversion via `IMonthlyTotalsReport`)
- Chain: source currency → USD → target fiat via `ICurrencyConversionService.Convert(amount, source, target, bitcoinPriceUsd, fiatRates)`; on failure return original amount (catch-all).
- Target = `_currencySettings.MainFiatCurrency` (D-20).

### Currency formatting in UI
**Source:** `DashboardPanelViewModel.cs` lines 64-71
```csharp
protected static string FormatBtc(long sats) =>
    Valt.Infra.Kernel.CurrencyDisplay.FormatSatsAsBitcoin(sats) + " BTC";
protected static string FormatFiat(decimal amount, string currencyCode) =>
    Valt.Infra.Kernel.CurrencyDisplay.FormatFiat(amount, currencyCode);
protected static string FormatPercent(decimal value, int decimals = 2) =>
    value.ToString($"F{decimals}") + "%";
```

### Localization (English-only this phase, D-21)
**Source:** `ReportsViewModel.cs` lines 644-685 (`language.Reports_Statistics_*` usage)
**Apply to:** BurnRatePanelViewModel rows, chart section headers, hint-box text
- Add keys to `language.resx` + static property in `language.Designer.cs` only. Do NOT touch `language.pt-BR.resx` / `language.es.resx` (Phase 43).

## No Analog Found

None — every file has an exact or role-match analog in the codebase.

## Metadata

**Analog search scope:** `src/Valt.App/Modules/SpendingEvolution/`, `src/Valt.Infra/Modules/{SpendingEvolution,Reports,Budget/FixedExpenses}/`, `src/Valt.UI/Views/Main/Tabs/Reports/`, `tests/Valt.Tests/Reports/`, `src/Valt.App/Extensions.cs`, `src/Valt.Infra/Extensions.cs`
**Files scanned:** 20
**Pattern extraction date:** 2026-08-04
