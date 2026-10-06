# Phase 40: BTC-Denominated Metrics Reports & UI - Pattern Map

**Mapped:** 2026-08-05
**Files analyzed:** 17
**Analogs found:** 17 / 17

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Valt.App/Modules/BtcDenominatedMetrics/Contracts/IBtcDenominatedMetricsQueries.cs` | contract | request-response | `src/Valt.App/Modules/SpendingEvolution/Contracts/ISpendingEvolutionQueries.cs` | exact |
| `src/Valt.App/Modules/BtcDenominatedMetrics/Queries/GetBtcDenominatedMetricsQuery.cs` | model | request-response | `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionQuery.cs` | exact |
| `src/Valt.App/Modules/BtcDenominatedMetrics/Queries/GetBtcDenominatedMetricsHandler.cs` | handler | request-response | `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionHandler.cs` | exact |
| `src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/BtcDenominatedMetricsDataDto.cs` | model | transform | `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionDataDto.cs` | exact |
| `src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/BtcDenominatedMetricsMonthDto.cs` | model | transform | `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionMonthDto.cs` | exact |
| `src/Valt.App/Modules/BtcDenominatedMetrics/DTOs/SatsSpentByCategoryDto.cs` | model | transform | `src/Valt.Infra/Modules/Reports/ExpensesByCategory/ExpensesByCategoryData.cs` (shape) | role-match |
| `src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs` | service | CRUD/transform | `src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs` | exact |
| `src/Valt.UI/Views/Main/Tabs/Reports/BtcDenominatedMetricsChartData.cs` | component | transform | `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs` (grouped bars) + `ExpensesByCategoryChartData.cs` (category rows) | role-match |
| `src/Valt.UI/Views/Main/Tabs/Reports/StackVelocityChartData.cs` | component | transform | `src/Valt.UI/Views/Main/Tabs/Reports/SavingsRateChartData.cs` | role-match |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` (modify) | viewmodel | event-driven / request-response | `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` (existing `FetchSavingsRateAsync`, `FetchFixedVsVariableAsync`, `FetchExpensesByCategoryAsync`) | exact |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` (modify) | view | request-response | `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` (SavingsRate / FixedVsVariable / Categories expanders) | exact |
| `src/Valt.Infra/Extensions.cs` (modify) | config | DI registration | `src/Valt.Infra/Extensions.cs` lines 280-283 | exact |
| `src/Valt.UI/Lang/language.resx` (modify) | config | localization | `src/Valt.UI/Lang/language.resx` lines 2973-2999 | exact |
| `src/Valt.UI/Lang/language.Designer.cs` (modify) | config | localization | `src/Valt.UI/Lang/language.Designer.cs` lines 5310-5408 | exact |
| `tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs` | test | request-response | `tests/Valt.Tests/Reports/SavingsRateQueriesTests.cs` | exact |

## Pattern Assignments

### `IBtcDenominatedMetricsQueries.cs` (contract, request-response)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/Contracts/ISpendingEvolutionQueries.cs`

**Pattern** (lines 1-9):
```csharp
using Valt.App.Modules.SpendingEvolution.DTOs;
using Valt.App.Modules.SpendingEvolution.Queries;

namespace Valt.App.Modules.SpendingEvolution.Contracts;

public interface ISpendingEvolutionQueries
{
    Task<SpendingEvolutionDataDto> GetSpendingEvolutionAsync(GetSpendingEvolutionQuery query);
}
```

**Copy:** Declare one async method per query. Return the App-layer DTO; accept the query record. If the planner prefers separate queries for earned/spent, velocity and category breakdown, create one interface method per query following the same shape.

---

### `GetBtcDenominatedMetricsQuery.cs` (model, request-response)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionQuery.cs`

**Pattern** (lines 1-12):
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

**Copy:** Implement `IQuery<TDto>`; expose `From`, `To`, `CategoryIds`, `AccountIds`. The Reports tab already filters by selected accounts/categories, so wire those arrays into the query.

---

### `GetBtcDenominatedMetricsHandler.cs` (handler, request-response)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionHandler.cs`

**Pattern** (lines 1-20):
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

**Copy:** Handler is a thin pass-through to the Infra query implementation. It is auto-discovered by `Valt.App.Extensions.AddValtApp()` (lines 30-39).

---

### `BtcDenominatedMetricsDataDto.cs` / `BtcDenominatedMetricsMonthDto.cs` (models, transform)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionDataDto.cs` and `SpendingEvolutionMonthDto.cs`

**Pattern** (lines 1-8):
```csharp
namespace Valt.App.Modules.SpendingEvolution.DTOs;

public record SpendingEvolutionDataDto
{
    public required IReadOnlyList<SpendingEvolutionMonthDto> Months { get; init; }
    public required bool HasMissingPriceInSats { get; init; }
    public required string PrimaryCurrency { get; init; }
}
```

**Pattern** (lines 1-9):
```csharp
namespace Valt.App.Modules.SpendingEvolution.DTOs;

public record SpendingEvolutionMonthDto
{
    public required DateOnly Month { get; init; }
    public required decimal FiatTotal { get; init; }
    public required long SatsTotal { get; init; }
    public required int TransactionCount { get; init; }
}
```

**Copy:** Use `record` with `required init` properties. For Phase 40 use a combined DTO such as:

```csharp
public record BtcDenominatedMetricsDataDto
{
    public required IReadOnlyList<BtcDenominatedMetricsMonthDto> Months { get; init; }
    public required IReadOnlyList<SatsSpentByCategoryDto> SpentByCategory { get; init; }
    public required string PrimaryCurrency { get; init; }
}

public record BtcDenominatedMetricsMonthDto
{
    public required DateOnly Month { get; init; }
    public required long SatsEarned { get; init; }
    public required long SatsSpent { get; init; }
    public required long StackVelocity { get; init; }
}

public record SatsSpentByCategoryDto
{
    public required string CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required long SatsTotal { get; init; }
    public required string? IconUnicode { get; init; }
    public required string? IconColor { get; init; }
}
```

If the planner splits the metrics into multiple queries, create one `*DataDto` / `*MonthDto` pair per query using the same pattern.

---

### `BtcDenominatedMetricsQueries.cs` (service, CRUD/transform)

**Analogs:**
- Aggregation/filtering: `src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs`
- BTC-denominated totals and transaction-type classification: `src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs`
- Receipt-date rate lookups: `src/Valt.Infra/Modules/Reports/IReportDataProvider.cs`

**Constructor / DI pattern** (`SpendingEvolutionQueries.cs` lines 17-32):
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

**Recommended constructor for BTC metrics** — prefer `IReportDataProviderFactory` + `IMonthlyTotalsReport` + `IClock` + `CurrencySettings` (mirrors `SavingsRateQueries.cs` lines 15-30):
```csharp
public class BtcDenominatedMetricsQueries : IBtcDenominatedMetricsQueries
{
    private readonly IReportDataProviderFactory _reportDataProviderFactory;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;
    private readonly IClock _clock;
    private readonly CurrencySettings _currencySettings;

    public BtcDenominatedMetricsQueries(
        IReportDataProviderFactory reportDataProviderFactory,
        IMonthlyTotalsReport monthlyTotalsReport,
        IClock clock,
        CurrencySettings currencySettings)
    {
        _reportDataProviderFactory = reportDataProviderFactory;
        _monthlyTotalsReport = monthlyTotalsReport;
        _clock = clock;
        _currencySettings = currencySettings;
    }
```

**Empty-state return** (`SavingsRateQueries.cs` lines 37-44):
```csharp
if (provider.AllTransactions.Count == 0)
{
    return new SavingsRateDataDto
    {
        Months = [],
        PrimaryCurrency = currency.Code
    };
}
```

**Transaction-type classification helpers** (`MonthlyTotalsReport.cs`):
- Native bitcoin income: `TransactionEntityType.Bitcoin` with `FromSatAmount > 0` (lines 203-208).
- Native bitcoin expense: `TransactionEntityType.Bitcoin` with `FromSatAmount < 0` (lines 207-208).
- BTC purchase: `TransactionEntityType.FiatToBitcoin` with `ToSatAmount > 0` (lines 225-228).
- BTC sale: `TransactionEntityType.BitcoinToFiat` with `FromSatAmount < 0` (lines 210-213).
- Fiat income/expense: `TransactionEntityType.Fiat` with sign of `FromFiatAmount` (lines 259-270).

**Receipt-date rate lookup** (`IReportDataProvider.cs` lines 13-27):
```csharp
public interface IReportDataProvider
{
    FrozenDictionary<DateOnly, BitcoinDataEntity> BtcRates { get; }
    FrozenDictionary<DateOnly, ImmutableList<FiatDataEntity>> FiatRates { get; }

    decimal GetFiatRateAt(DateOnly date, FiatCurrency currency);
    decimal GetUsdBitcoinPriceAt(DateOnly date);
}
```

**Important:** `GetUsdBitcoinPriceAt` falls back to the closest prior date (binary search). For D-06 (skip transactions with no exact BTC price on the transaction date), check exact key presence before converting:

```csharp
if (!provider.BtcRates.ContainsKey(transactionDate))
    continue;   // silently skip per D-06/D-07
```

**Fiat → sats conversion path** (`CurrencyConversionService.cs` lines 112-135):
```csharp
private decimal ConvertFiatToBtc(decimal fiatAmount, string fiatCode,
    decimal? bitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? fiatRates)
{
    if (bitcoinPriceUsd is null or 0)
        return 0;

    decimal usdAmount;
    if (string.Equals(fiatCode, FiatCurrency.Usd.Code, StringComparison.OrdinalIgnoreCase))
    {
        usdAmount = fiatAmount;
    }
    else
    {
        var fiatRate = GetFiatRate(fiatCode, fiatRates);
        if (fiatRate == 0)
            return 0;
        usdAmount = fiatAmount / fiatRate;
    }

    return usdAmount / bitcoinPriceUsd.Value;
}
```

For receipt-date conversion, use `provider.GetFiatRateAt(date, accountCurrency)` and `provider.GetUsdBitcoinPriceAt(date)` (after the exact-date guard) instead of the latest rates.

**Internal-transfer exclusion** (D-04): skip when `transaction.ToAccountId.HasValue` and `provider.Accounts.ContainsKey(transaction.ToAccountId.Value)`.

---

### `BtcDenominatedMetricsChartData.cs` (component, transform)

**Analog:** `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs` (grouped bars) + `src/Valt.UI/Views/Main/Tabs/Reports/ExpensesByCategoryChartData.cs` (category rows)

**Group bar series pattern** (`FixedVsVariableChartData.cs` lines 42-92):
```csharp
public ObservableCollection<ISeries> Series { get; } = new();
public ObservableCollection<string> MonthLabels { get; } = new();
public ObservableCollection<double> FixedValues { get; } = new();
public ObservableCollection<double> VariableValues { get; } = new();

private StackedColumnSeries<double> CreateFixedSeries() => new()
{
    Name = language.Reports_FixedVariable_Fixed,
    Values = FixedValues,
    Stroke = null,
    Fill = new SolidColorPaint(FixedColor)
};

private StackedColumnSeries<double> CreateVariableSeries() => new()
{
    Name = language.Reports_FixedVariable_Variable,
    Values = VariableValues,
    Stroke = null,
    Fill = new SolidColorPaint(VariableColor)
};
```

**Category row-series pattern** (`ExpensesByCategoryChartData.cs` lines 144-178):
```csharp
var rowSeries = new RowSeries<double>
{
    Values = values,
    DataLabelsPaint = new SolidColorPaint(SKColors.White) { ... },
    DataLabelsSize = 12,
    DataLabelsPosition = DataLabelsPosition.End,
    DataLabelsFormatter = point => CurrencyDisplay.FormatFiat(-(decimal)point.Model, FiatCurrency.Code),
    XToolTipLabelFormatter = point => $"{percentage:F1}%",
    YToolTipLabelFormatter = _ => null!,
    Padding = 2
};

rowSeries.PointMeasured += point =>
{
    if (point.Visual is null) return;
    var index = point.Index;
    if (index < colors.Count)
        point.Visual.Fill = new SolidColorPaint(colors[index]);
};
```

**Copy:** Create two rendering modes in the same chart-data class:
- Monthly mode: two `StackedColumnSeries<double>` (or `ColumnSeries<double>`) for sats earned / sats spent.
- Category mode: one `RowSeries<double>` for sats spent per category, colored by category icon.

Use `CurrencyDisplay.FormatSatsAsBitcoin` or `FormatAsBitcoin` for axis labels/tooltips.

---

### `StackVelocityChartData.cs` (component, transform)

**Analog:** `src/Valt.UI/Views/Main/Tabs/Reports/SavingsRateChartData.cs`

**Line series pattern** (lines 42-110):
```csharp
public ObservableCollection<ObservablePoint> RateValues { get; } = new();
public ObservableCollection<string> MonthLabels { get; } = new();

private LineSeries<ObservablePoint> CreateRateSeries() => new()
{
    Name = language.Reports_SavingsRate_Title,
    Values = RateValues,
    Stroke = new SolidColorPaint(BtcDark) { StrokeThickness = 2.5f },
    GeometryStroke = new SolidColorPaint(BtcPrimary) { StrokeThickness = 2 },
    GeometryFill = new SolidColorPaint(BtcLight),
    GeometrySize = 8,
    Fill = new SolidColorPaint(BtcFill),
    LineSmoothness = 0.3
};

public void RefreshChart(SavingsRateDataDto savingsRateData)
{
    RateValues.Clear();
    MonthLabels.Clear();
    XAxes[0].MinLimit = null;
    XAxes[0].MaxLimit = null;
    DisposeSeries();
    Series.Clear();

    for (var index = 0; index < savingsRateData.Months.Count; index++)
    {
        var month = savingsRateData.Months[index];
        MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
        RateValues.Add(new ObservablePoint(index, month.Rate.HasValue ? (double?)month.Rate.Value : null));
    }

    _rateSeries = CreateRateSeries();
    Series.Add(_rateSeries);
}
```

**Copy:** Replace the nullable rate with the long `StackVelocity`. Months with zero velocity must emit `0`, not `null`, per D-11.

---

### `ReportsViewModel.cs` modifications (viewmodel, event-driven / request-response)

**Analog:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` existing fetch methods

**Observable properties pattern** (lines 98-162):
```csharp
[ObservableProperty] private SavingsRateChartData _savingsRateChartData = new();
[ObservableProperty] private FixedVsVariableChartData _fixedVsVariableChartData = new();
[ObservableProperty] private bool _isSavingsRateLoading = true;
[ObservableProperty] private bool _isSavingsRateEmpty;
[ObservableProperty] private bool _isFixedVsVariableLoading = true;
[ObservableProperty] private bool _isFixedVsVariableEmpty;
[ObservableProperty] private bool _hasNoFixedExpenses;
```

**Add for Phase 40:**
```csharp
[ObservableProperty] private BtcDenominatedMetricsChartData _btcDenominatedMetricsChartData = new();
[ObservableProperty] private StackVelocityChartData _stackVelocityChartData = new();
[ObservableProperty] private bool _isBtcDenominatedMetricsLoading = true;
[ObservableProperty] private bool _isStackVelocityLoading = true;
[ObservableProperty] private bool _isBtcDenominatedMetricsEmpty;
[ObservableProperty] private bool _isStackVelocityEmpty;
```

**Dispatcher-bound fetch pattern** (lines 1011-1073):
```csharp
private async Task FetchSavingsRateAsync(IReportDataProvider provider)
{
    try
    {
        var data = await _queryDispatcher.DispatchAsync(new GetSavingsRateQuery
        {
            From = DateOnly.FromDateTime(FilterRange.Start),
            To = DateOnly.FromDateTime(FilterRange.End),
            CategoryIds = GetSelectedAnalyticsCategoryIds()
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SavingsRateChartData.RefreshChart(data);
            IsSavingsRateEmpty = data.Months.Count == 0;
            IsSavingsRateLoading = false;
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching savings rate");
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsSavingsRateEmpty = false;
            IsSavingsRateLoading = false;
        });
    }
}
```

**Copy:** Add `FetchBtcDenominatedMetricsAsync` and `FetchStackVelocityAsync` following this exact shape, dispatching the new App-layer query and refreshing the corresponding chart data on the UI thread.

**Parallel fetch orchestration** (lines 388-400):
```csharp
private async Task FetchAllReportsAsync(IReportDataProvider provider)
{
    await Task.WhenAll(
        FetchMonthlyTotalsAsync(provider),
        ...
        FetchSavingsRateAsync(provider),
        FetchFixedVsVariableAsync(provider),
        ...);
}
```

**Copy:** Include the two new fetch methods in `Task.WhenAll`.

---

### `ReportsView.axaml` modifications (view, request-response)

**Analog:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` (SavingsRate / FixedVsVariable / Categories expanders)

**Expander + chart pattern** (lines 401-529):
```xml
<Expander Classes="section" VerticalAlignment="Stretch" HorizontalAlignment="Stretch" IsExpanded="True">
    <Expander.Header>
        <StackPanel Orientation="Horizontal">
            <TextBlock Classes="section-header-icon" Text="&#xE2EB;" />
            <TextBlock Classes="section-header" Text="{x:Static lang:language.Reports_SavingsRate_Title}" />
        </StackPanel>
    </Expander.Header>
    <StackPanel Orientation="Vertical">
        <Grid MinHeight="400">
            <Panel IsVisible="{Binding !IsSavingsRateLoading}">
                <Border ... IsVisible="{Binding !IsSavingsRateEmpty}">
                    <avalonia:CartesianChart
                        Series="{Binding SavingsRateChartData.Series}"
                        XAxes="{Binding SavingsRateChartData.XAxes}"
                        YAxes="{Binding SavingsRateChartData.YAxes}"
                        ... />
                </Border>
                <Border Classes="hint-box" IsVisible="{Binding IsSavingsRateEmpty}">
                    ... empty-state text ...
                </Border>
            </Panel>
            <TextBlock Text="{x:Static lang:language.Loading}" ... IsVisible="{Binding IsSavingsRateLoading}" />
        </Grid>
    </StackPanel>
</Expander>
```

**Copy:** Append a new Expander after the Phase 39 panels (after the `<!--Categories-->` expander, before the closing `</StackPanel>`). Inside it:
- A toggle (`ToggleSwitch` or `ComboBox`) bound to a view-model boolean such as `IsCategoryBreakdownSelected`.
- A chart bound to `BtcDenominatedMetricsChartData` for the monthly grouped-bar view.
- A second chart bound to the same chart-data class for the category row view, toggled via `IsVisible`.
- Empty-state and loading blocks matching the SavingsRate pattern.

Add a second Expander below for the stack-velocity line chart, bound to `StackVelocityChartData`.

---

### `src/Valt.Infra/Extensions.cs` modifications (config, DI)

**Analog:** `src/Valt.Infra/Extensions.cs` lines 271-286

**Pattern** (lines 280-283):
```csharp
services.AddSingleton<ISpendingEvolutionQueries, SpendingEvolutionQueries>();
services.AddSingleton<ISavingsRateQueries, SavingsRateQueries>();
services.AddSingleton<IBurnRateQueries, BurnRateQueries>();
services.AddSingleton<IFixedVsVariableQueries, FixedVsVariableQueries>();
```

**Copy:** Add the new interface/implementation pair in `AddQueries()`:

```csharp
services.AddSingleton<IBtcDenominatedMetricsQueries, BtcDenominatedMetricsQueries>();
```

Also add the corresponding `using` statements for the new App contract and Infra implementation namespaces.

---

### Localization (`language.resx` and `language.Designer.cs`) modifications (config)

**Analog:** `src/Valt.UI/Lang/language.resx` lines 2973-2999 and `language.Designer.cs` lines 5310-5408

**resx entry pattern** (lines 2973-2980):
```xml
<data name="Reports_SavingsRate_Title" xml:space="preserve">
    <value>Savings rate</value>
</data>
<data name="Reports_SavingsRate_EmptyHeading" xml:space="preserve">
    <value>No savings data yet</value>
</data>
```

**Designer accessor pattern** (lines 5310-5314):
```csharp
public static string Reports_SavingsRate_Title {
    get {
        return ResourceManager.GetString("Reports_SavingsRate_Title", resourceCulture);
    }
}
```

**Copy:** Add English-only strings per D-19, e.g.:
- `Reports_BtcMetrics_Title` = "Sats earned & spent"
- `Reports_BtcMetrics_Earned` = "Earned"
- `Reports_BtcMetrics_Spent` = "Spent"
- `Reports_BtcMetrics_PerCategory` = "By category"
- `Reports_BtcMetrics_StackVelocity_Title` = "Stack velocity"
- `Reports_BtcMetrics_EmptyHeading` / `Reports_BtcMetrics_EmptyBody`
- `Reports_StackVelocity_EmptyHeading` / `Reports_StackVelocity_EmptyBody`

> **Conflict note:** D-19 says English-only in this phase. AGENTS.md says update all three language files. Planner should follow D-19 (English-only for Phase 40) and schedule pt-BR/es in Phase 43.

---

### `tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs` (test, request-response)

**Analog:** `tests/Valt.Tests/Reports/SavingsRateQueriesTests.cs`

**Test fixture pattern** (lines 1-60):
```csharp
[TestFixture]
public class SavingsRateQueriesTests : DatabaseTest
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

**SUT construction pattern** (lines 162-169):
```csharp
var monthlyTotalsReport = new MonthlyTotalsReport(clock, new NullLogger<MonthlyTotalsReport>());
var currencySettings = new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
{
    MainFiatCurrency = FiatCurrency.Brl.Code
};
var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, clock);
var sut = new SavingsRateQueries(factory, monthlyTotalsReport, clock, currencySettings);
```

**Builder pattern for transactions** (lines 203-214):
```csharp
_localDatabase.GetTransactions().Insert(new TransactionBuilder()
{
    Id = IdGenerator.Generate(),
    CategoryId = categoryId,
    Date = new DateOnly(month.Year, month.Month, 10),
    Name = $"Income {month}",
    AutoSatAmountDetails = AutoSatAmountDetails.Pending,
    TransactionDetails = new FiatDetails(_brlAccount.Id.ToString(), amount, true)
}.Build());
```

**Copy:** Create tests inheriting `DatabaseTest`, seed BTC/fiat rates, use `TransactionBuilder`/`FiatAccountBuilder`/`BtcAccountBuilder`, and assert monthly sats earned/spent/velocity and category breakdown. Include explicit tests for D-04 (internal transfers excluded) and D-06 (missing BTC price skips transaction).

## Shared Patterns

### Receipt-Date BTC Conversion & Missing-Rate Skip
**Source:** `src/Valt.Infra/Modules/Reports/IReportDataProvider.cs` (lines 19-27) + custom exact-date check
**Apply to:** `BtcDenominatedMetricsQueries.cs`

`IReportDataProvider.GetUsdBitcoinPriceAt(date)` returns the closest prior date. For Phase 40, D-06 requires skipping a transaction when its exact date has no BTC price. Use the exposed frozen dictionary:

```csharp
if (!provider.BtcRates.ContainsKey(transactionDate))
    continue;

var btcPriceUsd = provider.GetUsdBitcoinPriceAt(transactionDate);
```

For non-main fiat, follow the source → USD → BTC path from `CurrencyConversionService.cs` lines 112-135 using `provider.GetFiatRateAt(transactionDate, accountCurrency)` and the transaction-date BTC price.

### Internal-Transfer Exclusion
**Source:** inferred from `IReportDataProvider.Accounts` and `TransactionEntity.ToAccountId`
**Apply to:** `BtcDenominatedMetricsQueries.cs`

```csharp
if (transaction.ToAccountId is { } toId && provider.Accounts.ContainsKey(toId))
    continue;   // D-04: user's own accounts on both sides
```

### Currency Conversion to Sats
**Source:** `src/Valt.Infra/Modules/Currency/Services/CurrencyConversionService.cs` lines 112-135
**Apply to:** `BtcDenominatedMetricsQueries.cs`

Convert fiat to sats using the transaction-date USD/BTC price and the transaction-date fiat/USD rate. Native sat amounts (`TransactionEntity.SatAmount` or `FromSatAmount`/`ToSatAmount`) are already in sats.

### LiveCharts Series Disposal
**Source:** `SavingsRateChartData.cs` lines 99-123 / `FixedVsVariableChartData.cs` lines 108-138
**Apply to:** `BtcDenominatedMetricsChartData.cs`, `StackVelocityChartData.cs`

Always dispose the previous series instance and recreate it in `RefreshChart`. Reusing the same series object causes stale geometry after resize + refresh.

### Dispatcher-Thread UI Updates
**Source:** `ReportsViewModel.cs` lines 1011-1037
**Apply to:** New fetch methods in `ReportsViewModel.cs`

All UI-bound collections/chart data must be updated inside `await Dispatcher.UIThread.InvokeAsync(...)`.

### Error Handling / Loading Flags
**Source:** `ReportsViewModel.cs` fetch methods
**Apply to:** New fetch methods in `ReportsViewModel.cs`

Set the loading flag to `true` before dispatch, set it to `false` in both success and exception paths, and log the exception with `_logger.LogError`.

### Empty-State Pattern
**Source:** `ReportsView.axaml` lines 431-449
**Apply to:** New XAML sections

Use a `Border Classes="hint-box"` with centered `TextBlock`s bound to `IsBtcDenominatedMetricsEmpty` / `IsStackVelocityEmpty`.

### Query-Handler Auto-Registration
**Source:** `src/Valt.App/Extensions.cs` lines 30-39
**Apply to:** New `GetBtcDenominatedMetricsHandler`

No manual registration is needed for handlers; assembly scanning picks up any `IQueryHandler<,>` implementation as a singleton.

### Localization Naming Convention
**Source:** `src/Valt.UI/Lang/language.resx`
**Apply to:** All new user-facing strings

Use dot-separated names under `Reports_*`, e.g. `Reports_BtcMetrics_Title`. Add the matching `public static string` accessor in `language.Designer.cs`.

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| Receipt-date exact-match BTC price guard | service rule | transform | Existing `IReportDataProvider.GetUsdBitcoinPriceAt` falls back to the closest prior date. Phase 40 needs an explicit exact-date check against `provider.BtcRates`, so no existing query copies this behavior verbatim. |

## Metadata

**Analog search scope:**
- `src/Valt.App/Modules/SpendingEvolution/`
- `src/Valt.App/Modules/SpendingAnalytics/`
- `src/Valt.Infra/Modules/SpendingEvolution/Queries/`
- `src/Valt.Infra/Modules/SpendingAnalytics/Queries/`
- `src/Valt.Infra/Modules/Reports/`
- `src/Valt.UI/Views/Main/Tabs/Reports/`
- `src/Valt.UI/Lang/`
- `tests/Valt.Tests/Reports/`

**Files scanned:** 30+
**Pattern extraction date:** 2026-08-05
