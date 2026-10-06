# Phase 42: Loans & Leverage Reports & UI - Pattern Map

**Mapped:** 2026-08-11
**Files analyzed:** 15
**Analogs found:** 15 / 15

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Valt.App/Modules/LoanReports/Queries/GetLoanReportsQuery.cs` | query | request-response | `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionQuery.cs` | exact |
| `src/Valt.App/Modules/LoanReports/Queries/GetLoanReportsHandler.cs` | handler | request-response | `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionHandler.cs` | exact |
| `src/Valt.App/Modules/LoanReports/Contracts/ILoanReportsQueries.cs` | contract | request-response | `src/Valt.App/Modules/SpendingEvolution/Contracts/ISpendingEvolutionQueries.cs` | exact |
| `src/Valt.App/Modules/LoanReports/DTOs/LoanReportsDataDto.cs` | model/dto | transform | `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionDataDto.cs` | exact |
| `src/Valt.App/Modules/LoanReports/DTOs/LoanCostMonthDto.cs` | model/dto | transform | `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionMonthDto.cs` | exact |
| `src/Valt.App/Modules/LoanReports/DTOs/LiquidationDistanceMonthDto.cs` | model/dto | transform | `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionMonthDto.cs` | exact |
| `src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs` | service/query-impl | CRUD/transform | `src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs` | role-match |
| `src/Valt.Infra/Extensions.cs` (modify) | config | registration | existing `AddQueries()` block | exact |
| `src/Valt.UI/Views/Main/Tabs/Reports/LoanCostChartData.cs` | component | transform | `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs` | exact |
| `src/Valt.UI/Views/Main/Tabs/Reports/LiquidationDistanceChartData.cs` | component | transform | `src/Valt.UI/Views/Main/Tabs/Reports/StackVelocityChartData.cs` | exact |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` (modify) | controller/vm | request-response | existing `FetchFixedVsVariableAsync`, `FetchStackVelocityAsync` | exact |
| `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` (modify) | view | request-response | existing Stack velocity `Expander` block | exact |
| `src/Valt.UI/Lang/language.resx` (modify) | config/localization | static | existing `Reports_StackVelocity_*` entries | exact |
| `src/Valt.UI/Lang/language.Designer.cs` (modify) | config/localization | static | existing `Reports_StackVelocity_*` properties | exact |
| `tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs` | test | CRUD/transform | `tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs` | role-match |
| `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` (modify) | test | request-response | existing `ConfigureDefaultQueryDispatcherBehavior` and constructor wiring | exact |

## Pattern Assignments

### `src/Valt.App/Modules/LoanReports/Queries/GetLoanReportsQuery.cs` (query, request-response)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionQuery.cs`

**Imports pattern** (lines 1-4):
```csharp
using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingEvolution.DTOs;
```

**Core query pattern** (lines 6-12):
```csharp
public record GetSpendingEvolutionQuery : IQuery<SpendingEvolutionDataDto>
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string[] CategoryIds { get; init; } = Array.Empty<string>();
    public string[] AccountIds { get; init; } = Array.Empty<string>();
}
```

**Apply to new file:**
```csharp
using Valt.App.Kernel.Queries;
using Valt.App.Modules.LoanReports.DTOs;

namespace Valt.App.Modules.LoanReports.Queries;

public record GetLoanReportsQuery : IQuery<LoanReportsDataDto>
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public decimal? CustomBtcPriceUsd { get; init; }
}
```

---

### `src/Valt.App/Modules/LoanReports/Queries/GetLoanReportsHandler.cs` (handler, request-response)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/Queries/GetSpendingEvolutionHandler.cs`

**Imports pattern** (lines 1-5):
```csharp
using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingEvolution.Contracts;
using Valt.App.Modules.SpendingEvolution.DTOs;
```

**Core handler pattern** (lines 7-19):
```csharp
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

**Apply to new file:**
```csharp
using Valt.App.Kernel.Queries;
using Valt.App.Modules.LoanReports.Contracts;
using Valt.App.Modules.LoanReports.DTOs;

namespace Valt.App.Modules.LoanReports.Queries;

internal sealed class GetLoanReportsHandler : IQueryHandler<GetLoanReportsQuery, LoanReportsDataDto>
{
    private readonly ILoanReportsQueries _loanReportsQueries;

    public GetLoanReportsHandler(ILoanReportsQueries loanReportsQueries)
    {
        _loanReportsQueries = loanReportsQueries;
    }

    public Task<LoanReportsDataDto> HandleAsync(GetLoanReportsQuery query, CancellationToken ct = default)
    {
        return _loanReportsQueries.GetLoanReportsAsync(query);
    }
}
```

---

### `src/Valt.App/Modules/LoanReports/Contracts/ILoanReportsQueries.cs` (contract, request-response)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/Contracts/ISpendingEvolutionQueries.cs`

**Core contract pattern** (lines 1-9):
```csharp
using Valt.App.Modules.SpendingEvolution.DTOs;
using Valt.App.Modules.SpendingEvolution.Queries;

namespace Valt.App.Modules.SpendingEvolution.Contracts;

public interface ISpendingEvolutionQueries
{
    Task<SpendingEvolutionDataDto> GetSpendingEvolutionAsync(GetSpendingEvolutionQuery query);
}
```

**Apply to new file:**
```csharp
using Valt.App.Modules.LoanReports.DTOs;
using Valt.App.Modules.LoanReports.Queries;

namespace Valt.App.Modules.LoanReports.Contracts;

public interface ILoanReportsQueries
{
    Task<LoanReportsDataDto> GetLoanReportsAsync(GetLoanReportsQuery query);
}
```

---

### `src/Valt.App/Modules/LoanReports/DTOs/LoanReportsDataDto.cs` (model/dto, transform)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionDataDto.cs`

**DTO pattern** (lines 1-8):
```csharp
namespace Valt.App.Modules.SpendingEvolution.DTOs;

public record SpendingEvolutionDataDto
{
    public required IReadOnlyList<SpendingEvolutionMonthDto> Months { get; init; }
    public required bool HasMissingPriceInSats { get; init; }
    public required string PrimaryCurrency { get; init; }
}
```

**Apply to new file:**
```csharp
namespace Valt.App.Modules.LoanReports.DTOs;

public record LoanReportsDataDto
{
    public required IReadOnlyList<LoanCostMonthDto> CostMonths { get; init; }
    public required IReadOnlyList<LiquidationDistanceMonthDto> DistanceMonths { get; init; }
    public required bool HasActiveLoans { get; init; }
    public required string PrimaryCurrency { get; init; }
}
```

---

### `src/Valt.App/Modules/LoanReports/DTOs/LoanCostMonthDto.cs` (model/dto, transform)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionMonthDto.cs`

**Month DTO pattern** (lines 1-9):
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

**Apply to new file:**
```csharp
namespace Valt.App.Modules.LoanReports.DTOs;

public record LoanCostMonthDto
{
    public required DateOnly Month { get; init; }
    public required decimal CombinedCost { get; init; }
    public required decimal Interest { get; init; }
    public required decimal Fees { get; init; }
}
```

---

### `src/Valt.App/Modules/LoanReports/DTOs/LiquidationDistanceMonthDto.cs` (model/dto, transform)

**Analog:** `src/Valt.App/Modules/SpendingEvolution/DTOs/SpendingEvolutionMonthDto.cs`

**Apply to new file:**
```csharp
namespace Valt.App.Modules.LoanReports.DTOs;

public record LiquidationDistanceMonthDto
{
    public required DateOnly Month { get; init; }
    public required decimal DistanceToLiquidation { get; init; }
    public required string ClosestLoanName { get; init; }
}
```

---

### `src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs` (service/query-impl, CRUD/transform)

**Analog:** `src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs`

**Imports pattern** (lines 1-13):
```csharp
using System.Collections.Frozen;
using LiteDB;
using Valt.App.Modules.BtcDenominatedMetrics.Contracts;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.App.Modules.BtcDenominatedMetrics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Settings;
```

**Constructor / DI pattern** (lines 16-35):
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

**Month loop / zero-fill pattern** (lines 120-143):
```csharp
var startMonth = new DateOnly(query.From.Year, query.From.Month, 1);
var endMonth = new DateOnly(query.To.Year, query.To.Month, 1);
var sortedMonths = new List<BtcDenominatedMetricsMonthDto>();
for (var month = startMonth; month <= endMonth; month = month.AddMonths(1))
{
    var key = (month.Year, month.Month);
    var aggregation = monthlyData.TryGetValue(key, out var current)
        ? current
        : new MonthSatsAggregation();
    sortedMonths.Add(new BtcDenominatedMetricsMonthDto
    {
        Month = month,
        SatsEarned = aggregation.SatsEarned,
        SatsSpent = aggregation.SatsSpent,
        StackVelocity = aggregation.SatsEarned - aggregation.SatsSpent + aggregation.BtcPurchases - aggregation.BtcSales
    });
}
```

**Historical rate lookup pattern** (lines 145-154):
```csharp
private static long ConvertFiatToSats(decimal fiatAmount, DateOnly transactionDate, AccountEntity account, IReportDataProvider provider)
{
    var accountCurrency = FiatCurrency.GetFromCode(account.Currency!);
    var fiatRateToUsd = provider.GetFiatRateAt(transactionDate, accountCurrency);
    var btcPriceUsd = provider.GetUsdBitcoinPriceAt(transactionDate);

    var usdAmount = fiatAmount / fiatRateToUsd;
    var btcAmount = usdAmount / btcPriceUsd;
    return (long)(btcAmount * SatoshisPerBitcoin);
}
```

**Currency conversion helper to copy** (from `src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs` lines 38-54):
```csharp
decimal ConvertToMain(decimal value, string currency)
{
    if (currency == mainCurrencyCode)
        return value;
    if (fiatRates is null)
        return 0m;
    var valueInUsd = currency == FiatCurrency.Usd.Code
        ? value
        : fiatRates.TryGetValue(currency, out var rate) && rate > 0
            ? value / rate
            : 0m;
    if (mainCurrencyCode == FiatCurrency.Usd.Code)
        return valueInUsd;
    return fiatRates.TryGetValue(mainCurrencyCode, out var mainRate)
        ? valueInUsd * mainRate
        : 0m;
}
```

**Apply to new implementation sketch:**
- Inject `IAssetQueries`, `IClock`, `CurrencySettings`.
- Load all assets via `_assetQueries.GetAllAsync()`.
- Filter `AssetTypeId == (int)AssetTypes.BtcLoan` and active status at month-end using snapshots.
- For each month from `From` to `To`, compute combined cost and worst-case distance using snapshot interpolation.
- Use `IReportDataProviderFactory` or current rates for historical month-end BTC/fiat prices.
- Return every month zero-filled.

---

### `src/Valt.Infra/Extensions.cs` (modify) (config, registration)

**Analog:** existing `AddQueries()` block

**Registration pattern** (lines 273-288):
```csharp
private static IServiceCollection AddQueries(this IServiceCollection services)
{
    services.AddSingleton<IAccountQueries, AccountQueries>();
    services.AddSingleton<IAvgPriceQueries, AvgPriceQueries>();
    services.AddSingleton<ICategoryQueries, CategoryQueries>();
    services.AddSingleton<IFixedExpenseQueries, FixedExpenseQueries>();
    services.AddSingleton<ITransactionQueries, TransactionQueries>();
    services.AddSingleton<IGoalQueries, GoalQueries>();
    services.AddSingleton<IAssetQueries, AssetQueries>();
    services.AddSingleton<ISpendingEvolutionQueries, SpendingEvolutionQueries>();
    services.AddSingleton<IBurnRateQueries, BurnRateQueries>();
    services.AddSingleton<IFixedVsVariableQueries, FixedVsVariableQueries>();
    services.AddSingleton<IBtcDenominatedMetricsQueries, BtcDenominatedMetricsQueries>();

    return services;
}
```

**Apply:** Add `services.AddSingleton<ILoanReportsQueries, LoanReportsQueries>();` alongside the other query registrations.

---

### `src/Valt.UI/Views/Main/Tabs/Reports/LoanCostChartData.cs` (component, transform)

**Analog:** `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs`

**Imports pattern** (lines 1-14):
```csharp
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.UI.Lang;
```

**Chart setup pattern** (lines 17-41):
```csharp
public class FixedVsVariableChartData : IDisposable
{
    private static readonly SKColor FixedColor = SKColor.Parse("#0566e9");
    private static readonly SKColor VariableColor = SKColor.Parse("#ffa122");
    private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");
    private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");
    private static readonly SKColor LegendTextColor = SKColor.Parse("#eeebe8");
    private static readonly SKColor ChartBackground = SKColor.Parse("#333333");

    public string PrimaryCurrency { get; set; } = FiatCurrency.Usd.Code;

    public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipTextPaint { get; } = new(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(ChartBackground);

    public ObservableCollection<ISeries> Series { get; } = new();
    public ObservableCollection<string> MonthLabels { get; } = new();

    public Axis[] XAxes { get; } = new Axis[1];
    public Axis[] YAxes { get; } = new Axis[1];
```

**Axis / labeler pattern** (lines 45-70):
```csharp
public FixedVsVariableChartData()
{
    XAxes[0] = new Axis
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

    YAxes[0] = new Axis
    {
        Labeler = FiatLabeler,
        LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
        TextSize = 12,
        SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
        MinLimit = 0,
        MinZoomDelta = 1
    };
}
```

**Refresh pattern** (lines 93-123):
```csharp
public void RefreshChart(FixedVsVariableDataDto data)
{
    PrimaryCurrency = data.PrimaryCurrency;

    FixedValues.Clear();
    VariableValues.Clear();
    MonthLabels.Clear();

    XAxes[0].MinLimit = null;
    XAxes[0].MaxLimit = null;

    DisposeSeries();
    Series.Clear();

    foreach (var month in data.Months)
    {
        MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
        FixedValues.Add((double)month.FixedTotal);
        VariableValues.Add((double)month.VariableTotal);
    }

    _fixedSeries = CreateFixedSeries();
    _variableSeries = CreateVariableSeries();

    Series.Add(_fixedSeries);
    Series.Add(_variableSeries);
}
```

**Apply to new file:** Use a single `StackedColumnSeries<double>` for combined cost, fiat y-axis labeler, and dispose previous series on refresh.

---

### `src/Valt.UI/Views/Main/Tabs/Reports/LiquidationDistanceChartData.cs` (component, transform)

**Analog:** `src/Valt.UI/Views/Main/Tabs/Reports/StackVelocityChartData.cs`

**Line series pattern** (lines 73-83):
```csharp
private LineSeries<ObservablePoint> CreateVelocitySeries() => new()
{
    Name = language.Reports_StackVelocity_Velocity,
    Values = VelocityValues,
    Stroke = new SolidColorPaint(AccentPrimary) { StrokeThickness = 2.5f },
    GeometryStroke = new SolidColorPaint(AccentLight) { StrokeThickness = 2 },
    GeometryFill = new SolidColorPaint(AccentLight),
    GeometrySize = 8,
    Fill = new SolidColorPaint(AccentFill),
    LineSmoothness = 0.3
};
```

**Refresh pattern** (lines 85-109):
```csharp
public void RefreshChart(BtcDenominatedMetricsDataDto data)
{
    VelocityValues.Clear();
    MonthLabels.Clear();

    XAxes[0].MinLimit = null;
    XAxes[0].MaxLimit = null;

    DisposeSeries();
    Series.Clear();

    for (var index = 0; index < data.Months.Count; index++)
    {
        var month = data.Months[index];
        MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
        VelocityValues.Add(new ObservablePoint(index, (double)month.StackVelocity));
    }

    _velocitySeries = CreateVelocitySeries();
    Series.Add(_velocitySeries);
}
```

**Apply to new file:** Use `LineSeries<ObservablePoint>` with percentage y-axis labeler. For risk bands, select a single stroke/fill color per refresh based on the minimum distance in the dataset using `DashboardDataBrushes.ForLtv` thresholds (inverted semantics).

---

### `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` (modify) (controller/vm, request-response)

**Analog:** existing `FetchFixedVsVariableAsync`, `FetchStackVelocityAsync`, and `FetchAllReportsAsync`

**Fetch-all wiring pattern** (lines 397-410):
```csharp
private async Task FetchAllReportsAsync(IReportDataProvider provider)
{
    await Task.WhenAll(
        FetchMonthlyTotalsAsync(provider),
        FetchExpensesByCategoryAsync(provider),
        FetchIncomeByCategoryAsync(provider),
        FetchFixedVsVariableAsync(provider),
        FetchBtcDenominatedMetricsAsync(provider),
        FetchStackVelocityAsync(provider),
        FetchAllTimeHighDataAsync(provider),
        FetchMaxBtcStackDataAsync(provider),
        FetchStatisticsDataAsync(provider),
        FetchWealthOverviewAsync(provider));
}
```

**Per-chart fetch pattern** (lines 1035-1069 and 1071-1103):
```csharp
private async Task FetchFixedVsVariableAsync(IReportDataProvider provider)
{
    try
    {
        var data = await _queryDispatcher.DispatchAsync(new GetFixedVsVariableQuery
        {
            From = DateOnly.FromDateTime(FilterRange.Start),
            To = DateOnly.FromDateTime(FilterRange.End),
            CategoryIds = GetSelectedAnalyticsCategoryIds()
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            HasNoFixedExpenses = data.HasNoFixedExpenses;
            IsFixedVsVariableEmpty = data.Months.Count == 0 && !data.HasNoFixedExpenses;

            if (!data.HasNoFixedExpenses)
            {
                FixedVsVariableChartData.RefreshChart(data);
            }

            IsFixedVsVariableLoading = false;
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching fixed vs variable expenses");
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            HasNoFixedExpenses = false;
            IsFixedVsVariableEmpty = false;
            IsFixedVsVariableLoading = false;
        });
    }
}
```

**Apply:** Add `IsLoanReportsLoading`, `IsLoanReportsVisible`, `IsLoanReportsEmpty` observable properties; `LoanCostChartData` and `LiquidationDistanceChartData` properties; `FetchLoanReportsAsync(provider)`; include it in `FetchAllReportsAsync` and `FetchReportsWithDateRangeFilterAsync`; add `LoanReportsChartData.Dispose()` in `Dispose()`.

---

### `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` (modify) (view, request-response)

**Analog:** Stack velocity `Expander` block (lines 582-665)

**Section pattern** (lines 582-665):
```xml
<!--Stack velocity-->
<Expander Classes="section" VerticalAlignment="Stretch" HorizontalAlignment="Stretch" IsExpanded="True">
    <Expander.Header>
        <StackPanel Orientation="Horizontal">
            <TextBlock Classes="section-header-icon" Text="&#xE9E4;" />
            <TextBlock Classes="section-header" Text="{x:Static lang:language.Reports_StackVelocity_Title}" />
        </StackPanel>
    </Expander.Header>
    <StackPanel Orientation="Vertical">
        <TextBlock Text="{x:Static lang:language.Reports_StackVelocity_Description}"
                   FontSize="{DynamicResource FontSizeSmall}"
                   Foreground="{DynamicResource Text500Brush}"
                   TextWrapping="Wrap"
                   MaxWidth="900"
                   Margin="0,0,0,12" />
        <Grid MinHeight="400">
            <Panel IsVisible="{Binding !IsStackVelocityLoading}">
                <Panel IsVisible="{Binding !IsStackVelocityError}">
                    <Border Background="{DynamicResource Background900Brush}"
                            BorderBrush="{DynamicResource Background700Brush}"
                            BorderThickness="1"
                            CornerRadius="4"
                            Padding="10"
                            IsVisible="{Binding !IsStackVelocityEmpty}">
                        <avalonia:CartesianChart
                            x:Name="StackVelocityChart"
                            Background="{DynamicResource Background900Brush}"
                            Series="{Binding StackVelocityChartData.Series}"
                            XAxes="{Binding StackVelocityChartData.XAxes}"
                            YAxes="{Binding StackVelocityChartData.YAxes}"
                            LegendPosition="Top"
                            LegendTextSize="13"
                            TooltipTextSize="12"
                            ZoomMode="None"
                            Height="400"
                            Margin="20, 10, 20, 10">
                        </avalonia:CartesianChart>
                    </Border>
                    <Border Classes="hint-box"
                            VerticalAlignment="Center"
                            HorizontalAlignment="Center"
                            IsVisible="{Binding IsStackVelocityEmpty}">
                        ...empty state...
                    </Border>
                </Panel>
                ...error state...
            </Panel>
            <TextBlock Text="{x:Static lang:language.Loading}"
                       IsVisible="{Binding IsStackVelocityLoading}" />
        </Grid>
    </StackPanel>
</Expander>
```

**Apply:** Insert a new `Expander` after the Stack velocity section (line 665) and before the Categories section (line 669). Include two charts side by side or vertically, loading / empty / error states, and bind to new VM properties.

---

### `src/Valt.UI/Lang/language.resx` and `language.Designer.cs` (modify) (config/localization)

**Analog:** existing `Reports_StackVelocity_*` entries and generated properties

**resx pattern** (lines 3015-3027):
```xml
<data name="Reports_StackVelocity_Title" xml:space="preserve">
    <value>Stack Velocity</value>
</data>
<data name="Reports_StackVelocity_Velocity" xml:space="preserve">
    <value>Velocity</value>
</data>
```

**Designer pattern** (lines 5934-5954):
```csharp
public static string Reports_StackVelocity_Title {
    get {
        return ResourceManager.GetString("Reports_StackVelocity_Title", resourceCulture);
    }
}
```

**Apply:** Add English-only keys:
- `Reports_LoanReports_Title`
- `Reports_LoanReports_CostDescription`
- `Reports_LoanReports_DistanceDescription`
- `Reports_LoanReports_EmptyHeading`
- `Reports_LoanReports_EmptyBody`
- `Reports_LoanReports_CostSeries`
- `Reports_LoanReports_DistanceSeries`

---

### `tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs` (test, CRUD/transform)

**Analog:** `tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs`

**Test fixture pattern** (lines 1-71):
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.App.Modules.BtcDenominatedMetrics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.BtcDenominatedMetrics.Queries;
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class BtcDenominatedMetricsQueriesTests : DatabaseTest
{
    protected override Task SeedDatabase()
    {
        var initialDate = new DateTime(2024, 01, 01);
        var finalDate = new DateTime(2025, 12, 31);
        var currentDate = initialDate;
        while (currentDate <= finalDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity() { Date = currentDate, Price = 100000m });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity() { Date = currentDate, Currency = FiatCurrency.Brl.Code, Price = 5.5m });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity() { Date = currentDate, Currency = FiatCurrency.Usd.Code, Price = 1m });
            currentDate = currentDate.AddDays(1);
        }

        return base.SeedDatabase();
    }
```

**Apply:** Inherit `DatabaseTest`, seed BTC and fiat prices, insert BTC-backed loans with snapshots, instantiate `LoanReportsQueries` directly with `_assetQueries`, `_clock`, and `CurrencySettings`, then assert monthly cost and distance values.

---

### `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` (modify) (test, request-response)

**Analog:** existing constructor wiring and `ConfigureDefaultQueryDispatcherBehavior`

**Query dispatcher default behavior pattern** (lines 145-149):
```csharp
private void ConfigureDefaultQueryDispatcherBehavior()
{
    _queryDispatcher.DispatchAsync(Arg.Any<GetFixedVsVariableQuery>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<FixedVsVariableDataDto>(new InvalidOperationException("Fixed vs variable fetch triggered")));
}
```

**Apply:** Add a default stub for `GetLoanReportsQuery` returning an empty `LoanReportsDataDto`, and verify `FetchLoanReportsAsync` is invoked during `FetchAllReportsAsync`.

## Shared Patterns

### CQRS Query + Handler + Contract + DTO
**Source:** `src/Valt.App/Modules/SpendingEvolution/`
**Apply to:** All backend files in `Valt.App/Modules/LoanReports/`
- Query record implements `IQuery<TDto>`.
- Handler is `internal sealed`, implements `IQueryHandler<TQuery, TDto>`, delegates to `I*Queries` contract.
- DTOs are `public record` with `required` init properties.

### DI Registration
**Source:** `src/Valt.Infra/Extensions.cs` lines 273-288
**Apply to:** `LoanReportsQueries` registration
```csharp
services.AddSingleton<ILoanReportsQueries, LoanReportsQueries>();
```

### Reports Tab Fetch Pattern
**Source:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` lines 397-410, 1035-1103
**Apply to:** New `FetchLoanReportsAsync` method and `FetchAllReportsAsync` wiring
- Dispatch query via `_queryDispatcher.DispatchAsync(new GetLoanReportsQuery { ... })`.
- Set `IsLoading` / `IsEmpty` flags on `Dispatcher.UIThread.InvokeAsync`.
- Log errors and reset loading state in catch blocks.

### LiveCharts Chart-Data Class Pattern
**Source:** `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs`, `StackVelocityChartData.cs`
**Apply to:** `LoanCostChartData.cs`, `LiquidationDistanceChartData.cs`
- `ObservableCollection<ISeries> Series`, `Axis[] XAxes`, `Axis[] YAxes`.
- `RefreshChart(TDto data)` clears values, disposes previous series, rebuilds series and labels.
- `Dispose()` disposes paints and clears collections.

### LTV Risk Color Bands
**Source:** `src/Valt.UI/Views/Main/Tabs/Reports/DashboardDataBrushes.cs` lines 11-16
**Apply to:** `LiquidationDistanceChartData` color selection
```csharp
public static IBrush ForLtv(decimal ltv) => ltv switch
{
    <= 60m => TransactionGridResources.Credit,
    <= 75m => TransactionGridResources.Warning,
    _ => TransactionGridResources.Debt
};
```
Use the inverse distance thresholds (e.g., distance <= 5% red, <= 15% yellow, else green) for the line color.

### Month-End Active-Loan Filtering
**Source:** `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs` lines 164-166, 218-249; `src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs` lines 24-29
**Apply to:** `LoanReportsQueries` projection
- Find the effective snapshot as of month-end (`MaxBy(s => s.EffectiveDate)` where `EffectiveDate <= monthEnd`).
- Skip loans whose effective status is not `Active`.
- For APR loans, accrue `TotalBorrowed * Apr / 365` per day up to month-end or next snapshot.
- For fixed-debt loans, interest = 0; assign fees to snapshot's effective month.

### Currency Conversion Chain
**Source:** `src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs` lines 38-54
**Apply to:** `LoanReportsQueries` cost and distance calculations
- Source currency → USD via `fiatRates[currency]` division.
- USD → main currency via `fiatRates[mainCurrency]` multiplication.

## No Analog Found

None — every file has a close analog in the existing codebase.

## Metadata

**Analog search scope:** `src/Valt.App/Modules/*`, `src/Valt.Infra/Modules/*`, `src/Valt.UI/Views/Main/Tabs/Reports`, `tests/Valt.Tests/Reports`, `tests/Valt.Tests/UI/Screens`
**Files scanned:** 30+
**Pattern extraction date:** 2026-08-11
