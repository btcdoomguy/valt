# Phase 43: MCP, Localization, Documentation & Verification - Pattern Map

**Mapped:** 2026-08-12
**Files analyzed:** 10
**Analogs found:** 10 / 10

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Valt.Infra/Mcp/Tools/ReportTools.cs` | mcp-tool | request-response | `src/Valt.Infra/Mcp/Tools/ReportTools.cs` | exact |
| `src/Valt.Infra/Mcp/Server/McpServerService.cs` | service | config/registration | `src/Valt.Infra/Mcp/Server/McpServerService.cs` | exact |
| `src/Valt.UI/Lang/language.resx` | resource | static | `src/Valt.UI/Lang/language.resx` | exact |
| `src/Valt.UI/Lang/language.pt-BR.resx` | resource | static | `src/Valt.UI/Lang/language.pt-BR.resx` | exact |
| `src/Valt.UI/Lang/language.es.resx` | resource | static | `src/Valt.UI/Lang/language.es.resx` | exact |
| `src/Valt.UI/Lang/language.Designer.cs` | resource/utility | static | `src/Valt.UI/Lang/language.Designer.cs` | exact |
| `.claude/docs/reports.md` | doc | static | `.claude/docs/reports.md` | exact |
| `tests/Valt.Tests/Infra/Mcp/Tools/ReportToolsTests.cs` (new/implied) | test | request-response | `tests/Valt.Tests/Infra/Mcp/Tools/AssetToolsLoanStateTests.cs` | role-match |
| `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` (modify/implied) | test | event-driven / request-response | `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` | exact |

## Pattern Assignments

### `src/Valt.Infra/Mcp/Tools/ReportTools.cs` (mcp-tool, request-response)

**Analog:** `src/Valt.Infra/Mcp/Tools/ReportTools.cs`

**Tool class decoration** (lines 17-21):
```csharp
/// <summary>
/// MCP tools for financial reports.
/// </summary>
[McpServerToolType]
public class ReportTools
{
```

**Existing report tool pattern** (lines 26-72, `GetMonthlyTotals`):
```csharp
[McpServerTool, Description("Get monthly totals report with income, expenses, and bitcoin transactions over a date range")]
public static async Task<MonthlyTotalsResultDto> GetMonthlyTotals(
    IReportDataProviderFactory providerFactory,
    IMonthlyTotalsReport report,
    [Description("Start date of the range (format: yyyy-MM-dd)")] string startDate,
    [Description("End date of the range (format: yyyy-MM-dd)")] string endDate,
    [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode)
{
    var provider = await providerFactory.CreateAsync();
    var currency = FiatCurrency.GetFromCode(currencyCode);
    var start = DateOnly.Parse(startDate);
    var end = DateOnly.Parse(endDate);
    var baseDate = end;
    var range = new DateOnlyRange(start, end);

    var data = await report.GetAsync(baseDate, range, currency, provider);

    return new MonthlyTotalsResultDto
    {
        Currency = data.MainCurrency.Code,
        Items = data.Items.Select(i => new MonthlyTotalsItemDto { ... }).ToList(),
        Totals = new MonthlyTotalsSummaryDto { ... }
    };
}
```

**Query-dispatcher pattern for new App-layer queries** (from `src/Valt.Infra/Mcp/Tools/GoalTools.cs`, lines 25-35):
```csharp
[McpServerTool, Description("Get all financial goals, optionally filtered to goals containing a specific date")]
public static async Task<IReadOnlyList<GoalDTO>> GetGoals(
    IQueryDispatcher dispatcher,
    [Description("Optional filter date (format: yyyy-MM-dd)")] string? filterDate = null)
{
    DateOnly? parsedDate = string.IsNullOrWhiteSpace(filterDate)
        ? null
        : DateOnly.Parse(filterDate);

    return await dispatcher.DispatchAsync(new GetGoalsQuery { FilterDate = parsedDate });
}
```

**DTO region pattern** (lines 282-383):
```csharp
#region DTOs

public class MonthlyTotalsResultDto
{
    public required string Currency { get; init; }
    public required IReadOnlyList<MonthlyTotalsItemDto> Items { get; init; }
    public required MonthlyTotalsSummaryDto Totals { get; init; }
}

public class MonthlyTotalsItemDto
{
    public required string MonthYear { get; init; }
    public required decimal FiatTotal { get; init; }
    ...
}
...
#endregion
```

**Filter-parsing helpers** (lines 263-279):
```csharp
private static IEnumerable<AccountId> ParseAccountIds(string? ids)
{
    if (string.IsNullOrWhiteSpace(ids))
        return [];

    return ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(id => new AccountId(id));
}

private static IEnumerable<CategoryId> ParseCategoryIds(string? ids)
{
    if (string.IsNullOrWhiteSpace(ids))
        return [];

    return ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(id => new CategoryId(id));
}
```

**Application to new tools:**
- Add the four new tools (`GetSpendingAnalytics`, `GetBtcDenominatedMetrics`, `GetWealthPerformanceMetrics`, `GetLoanReports`) inside the existing `ReportTools` class.
- For the Spending/BTC/Loan categories, inject `IQueryDispatcher` and call the existing App-layer queries (`GetBurnRateQuery`, `GetFixedVsVariableQuery`, `GetBtcDenominatedMetricsQuery`, `GetLoanReportsQuery`).
- For Wealth Performance, keep using `IReportDataProviderFactory` plus existing reports (`IAllTimeHighReport`, `IWealthOverviewReport`, etc.) as the other tools do.
- Declare nested result DTOs in the `#region DTOs` block using `public class` with `required init` properties.

---

### `src/Valt.Infra/Mcp/Server/McpServerService.cs` (service, config/registration)

**Analog:** `src/Valt.Infra/Mcp/Server/McpServerService.cs`

**Forward services block** (lines 249-281):
```csharp
private void ForwardServicesFromMainApp(IServiceCollection services)
{
    // Database access
    services.AddSingleton(_localDatabase);

    // Command/Query dispatchers (needed by all CRUD tools)
    services.AddSingleton(_appServices.GetRequiredService<ICommandDispatcher>());
    services.AddSingleton(_appServices.GetRequiredService<IQueryDispatcher>());

    // Report services (needed by ReportTools)
    services.AddSingleton(_appServices.GetRequiredService<IReportDataProviderFactory>());
    services.AddSingleton(_appServices.GetRequiredService<IAllTimeHighReport>());
    services.AddSingleton(_appServices.GetRequiredService<IMaxBtcStackReport>());
    services.AddSingleton(_appServices.GetRequiredService<IExpensesByCategoryReport>());
    services.AddSingleton(_appServices.GetRequiredService<IIncomeByCategoryReport>());
    services.AddSingleton(_appServices.GetRequiredService<IMonthlyTotalsReport>());
    services.AddSingleton(_appServices.GetRequiredService<IStatisticsReport>());
    services.AddSingleton(_appServices.GetRequiredService<IWealthOverviewReport>());

    // Notification publisher (needed for MCP tools to notify UI of changes)
    services.AddSingleton(_appServices.GetRequiredService<INotificationPublisher>());

    // Indicator cache (needed by IndicatorTools)
    services.AddSingleton(_appServices.GetRequiredService<IIndicatorCache>());

    // Currency services (needed by CurrencyTools)
    services.AddSingleton(_appServices.GetRequiredService<IConfigurationManager>());
    services.AddSingleton(_appServices.GetRequiredService<CurrencySettings>());
    services.AddSingleton(_appServices.GetRequiredService<ICurrencyConversionService>());
    services.AddSingleton(_appServices.GetRequiredService<ILocalHistoricalPriceProvider>());
    services.AddSingleton(_appServices.GetRequiredService<IBitcoinPriceProvider>());
    services.AddSingleton(_appServices.GetRequiredService<IFiatPriceProviderSelector>());
}
```

**Application:**
- `IQueryDispatcher` is already forwarded; if new tools are written against `IQueryDispatcher`, no change is needed here.
- If a new tool directly consumes a report/query interface that is not yet forwarded, add another `services.AddSingleton(_appServices.GetRequiredService<IYourNewReport>());` line in the same block.

---

### `src/Valt.UI/Lang/language.resx` (resource, static)

**Analog:** `src/Valt.UI/Lang/language.resx`

**Entry structure** (lines 26-28 and 2949-2975):
```xml
<data name="OkButton" xml:space="preserve">
    <value>OK</value>
</data>
...
<data name="Reports_BurnRate_Title" xml:space="preserve">
    <value>Burn rate</value>
</data>
<data name="Reports_BurnRate_SpentSoFar" xml:space="preserve">
    <value>Spent so far (this month)</value>
</data>
```

**Application:**
- Add English source entries for every new user-facing string introduced in Phases 39-42 that does not already exist.
- Keep the same key naming conventions already in use (the codebase has two conventions: dot-separated like `Reports.AllTimeHigh.DaysUnderWater` and underscore-separated like `Reports_BurnRate_Title`). Match the convention used by nearby keys.

---

### `src/Valt.UI/Lang/language.pt-BR.resx` & `src/Valt.UI/Lang/language.es.resx` (resource, static)

**Analog:** `src/Valt.UI/Lang/language.pt-BR.resx` (lines 2952-2973) and `src/Valt.UI/Lang/language.es.resx` (lines 2952-2976)

**Translation pattern** (pt-BR example):
```xml
<data name="Reports_BurnRate_Title" xml:space="preserve">
    <value>Taxa de queima</value>
</data>
<data name="Reports_BurnRate_SpentSoFar" xml:space="preserve">
    <value>Gasto até agora (este mês)</value>
</data>
```

**Application:**
- Ensure both `language.pt-BR.resx` and `language.es.resx` contain the exact same keys as `language.resx`.
- Use proper translations where available; otherwise fall back to the English value as a placeholder (per D-05).

---

### `src/Valt.UI/Lang/language.Designer.cs` (resource/utility, static)

**Analog:** `src/Valt.UI/Lang/language.Designer.cs`

**Generated property pattern - underscore keys** (lines 5778-5794):
```csharp
public static string Reports_BurnRate_Title {
    get {
        return ResourceManager.GetString("Reports_BurnRate_Title", resourceCulture);
    }
}

public static string Reports_BurnRate_SpentSoFar {
    get {
        return ResourceManager.GetString("Reports_BurnRate_SpentSoFar", resourceCulture);
    }
}
```

**Generated property pattern - dot-separated keys** (lines 1668-1672):
```csharp
public static string Reports_AllTimeHigh_DaysUnderWater {
    get {
        return ResourceManager.GetString("Reports.AllTimeHigh.DaysUnderWater", resourceCulture);
    }
}
```

**Application:**
- Add a `public static string` property for every new key. The property name uses underscores for readability; the string passed to `ResourceManager.GetString` must exactly match the `name` attribute in the `.resx` file.
- If using a design-time resx generator, run it; otherwise manually add the properties and keep them alphabetically grouped with the other report strings.

---

### `.claude/docs/reports.md` (doc, static)

**Analog:** `.claude/docs/reports.md`

**Module section pattern** (lines 26-49, MonthlyTotalsReport example):
```markdown
### MonthlyTotalsReport

**Interface:** `IMonthlyTotalsReport`
**Output:** `MonthlyTotalsData`

Calculates monthly wealth totals and transaction flows.

**Metrics per month:**
- `FiatTotal` / `BtcTotal` - Wealth in currency
- `Income` / `Expenses` - Fiat-based flows
- `BitcoinIncome` / `BitcoinExpenses` - BTC flows
- `BitcoinPurchased` / `BitcoinSold` - Conversion totals
- Monthly and yearly percentage changes

**Algorithm:**
1. Iterates day-by-day through date range
2. Tracks per-account balances from initial amounts
3. Applies transaction changes daily
4. Converts to target currency using historical rates
5. Builds items with percentage comparisons
```

**UI layer pattern** (lines 98-118):
```markdown
## UI Layer (Valt.UI/Views/Main/Tabs/Reports/)

### ReportsViewModel

Main orchestrator for report tab.

**Observable Properties:**
- Dashboard data: `WealthData`, `AllTimeHighData`, `BtcStackData`, `StatisticsData`
- Chart data: `MonthlyTotalsChartData`, `ExpensesByCategoryChartData`
- Filter selections: accounts, categories, date ranges

**Data Loading Strategy:**
- Caches `IReportDataProvider` for tab lifetime
- `Initialize()` loads data when tab becomes active
...
```

**DI registration pattern** (lines 178-186):
```markdown
## DI Registration

```csharp
services.AddSingleton<IAllTimeHighReport, AllTimeHighReport>();
services.AddSingleton<IExpensesByCategoryReport, ExpensesByCategoryReport>();
services.AddSingleton<IMonthlyTotalsReport, MonthlyTotalsReport>();
services.AddSingleton<IStatisticsReport, StatisticsReport>();
services.AddSingleton<IReportDataProviderFactory, ReportDataProviderFactory>();
```
```

**Application:**
- Add new sub-sections under `## Reports Module` for Spending Analytics, BTC-Denominated Metrics, Wealth Performance, and Loan Reports.
- For each category document: interface/output contracts, metrics list, algorithm summary, UI panel(s) that expose it, and the MCP tool name that makes it AI-accessible.
- Update the file-structure block at the end to include the new App-layer modules (`Valt.App/Modules/SpendingAnalytics`, `BtcDenominatedMetrics`, `LoanReports`) and new UI panel ViewModels.

---

### `tests/Valt.Tests/Infra/Mcp/Tools/ReportToolsTests.cs` (test, request-response)

**Analog:** `tests/Valt.Tests/Infra/Mcp/Tools/AssetToolsLoanStateTests.cs`

**MCP tool test setup pattern** (lines 16-40):
```csharp
[TestFixture]
public class AssetToolsLoanStateTests : IntegrationTest
{
    private ICommandDispatcher _commandDispatcher = null!;
    private IQueryDispatcher _queryDispatcher = null!;
    private IAssetRepository _assetRepository = null!;
    private INotificationPublisher _notificationPublisher = null!;

    [OneTimeSetUp]
    public void AddApplicationLayer()
    {
        _serviceCollection.AddValtApp();
        RebuildServiceProvider();
    }

    [SetUp]
    public new void SetUp()
    {
        _notificationPublisher = Substitute.For<INotificationPublisher>();
        ReplaceService(_notificationPublisher);

        _commandDispatcher = _serviceProvider.GetRequiredService<ICommandDispatcher>();
        _queryDispatcher = _serviceProvider.GetRequiredService<IQueryDispatcher>();
        _assetRepository = _serviceProvider.GetRequiredService<IAssetRepository>();
    }
```

**Tool invocation pattern** (lines 51-68):
```csharp
var addResult = await AssetTools.AddLoanStateUpdate(
    _commandDispatcher,
    _notificationPublisher,
    asset.Id.Value,
    today,
    20_000m,
    ...);

Assert.That(addResult, Does.Not.StartWith("Error:"));

var latest = await AssetTools.GetLatestLoanState(_queryDispatcher, asset.Id.Value);
Assert.That(latest, Is.Not.Null);
```

**Application:**
- Create a new `[TestFixture]` inheriting from `IntegrationTest`.
- Call `_serviceCollection.AddValtApp()` and `RebuildServiceProvider()` in `OneTimeSetUp`.
- Seed accounts/transactions using the existing builders, then call the new static `ReportTools` methods and assert on the shape of the returned DTOs.

---

### `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` (test, event-driven/request-response)

**Analog:** `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs`

**ViewModel test setup pattern** (lines 46-149):
```csharp
[TestFixture]
public class ReportsViewModelTests
{
    private IQueryDispatcher _queryDispatcher = null!;
    private IAllTimeHighReport _allTimeHighReport = null!;
    private IWealthOverviewReport _wealthOverviewReport = null!;
    private IReportDataProviderFactory _reportDataProviderFactory = null!;
    ...

    [SetUp]
    public void SetUp()
    {
        WeakReferenceMessenger.Default.Reset();

        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _allTimeHighReport = Substitute.For<IAllTimeHighReport>();
        _wealthOverviewReport = Substitute.For<IWealthOverviewReport>();
        _reportDataProviderFactory = Substitute.For<IReportDataProviderFactory>();
        ...

        ConfigureDefaultQueryDispatcherBehavior();
    }

    private void ConfigureDefaultQueryDispatcherBehavior()
    {
        _queryDispatcher.DispatchAsync(Arg.Any<GetFixedVsVariableQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<FixedVsVariableDataDto>(new InvalidOperationException(...)));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanReportsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LoanReportsDataDto { ... }));
    }
```

**Application:**
- If adding panel-level end-to-end coverage, use the existing substitute-based setup and configure return values for `GetBurnRateQuery`, `GetFixedVsVariableQuery`, `GetBtcDenominatedMetricsQuery`, and `GetLoanReportsQuery`.
- Instantiate `ReportsViewModel` and assert that the new observable properties (`IsFixedVsVariableLoading`, `IsBtcMetricsEmpty`, `IsLoanReportsVisible`, etc.) transition to the expected states.

## Shared Patterns

### MCP Tool Surface
**Source:** `src/Valt.Infra/Mcp/Tools/ReportTools.cs` (lines 20-21, 26-72)
**Apply to:** `ReportTools.cs`
- Decorate the class with `[McpServerToolType]`.
- Decorate each static method with `[McpServerTool]` and `[Description(...)]`.
- Accept dependencies as method parameters (DI injects them from the forwarded container).
- Parse dates with `DateOnly.Parse`, currency with `FiatCurrency.GetFromCode`, and comma-separated IDs with the existing `ParseAccountIds`/`ParseCategoryIds` helpers.
- Map domain/App DTOs to dedicated MCP DTOs with `required init` properties.

### App-Layer Query Consumption from MCP Tools
**Source:** `src/Valt.Infra/Mcp/Tools/GoalTools.cs` (lines 25-35)
**Apply to:** `ReportTools.cs` for Spending, BTC-denominated, and Loan tools
- Inject `IQueryDispatcher` as a method parameter.
- Build the corresponding query record and call `await dispatcher.DispatchAsync(new GetXQuery { ... })`.
- The query dispatcher is already forwarded in `McpServerService.ForwardServicesFromMainApp()` (line 255-256).

### DI Service Forwarding
**Source:** `src/Valt.Infra/Mcp/Server/McpServerService.cs` (lines 249-281)
**Apply to:** `McpServerService.cs`
- Any report/query interface consumed directly by an MCP tool (not through `IQueryDispatcher`) must be explicitly forwarded from the main DI container to the MCP host.

### Localization Tri-File Update
**Source:** `src/Valt.UI/Lang/language.resx` and `language.Designer.cs`
**Apply to:** All new user-facing strings
- Add the key to `language.resx` with the English source string.
- Add the same key to `language.pt-BR.resx` and `language.es.resx` (translated or English fallback).
- Add a `public static string` property to `language.Designer.cs` that reads the key via `ResourceManager.GetString(...)`.

### Documentation Update
**Source:** `.claude/docs/reports.md`
**Apply to:** `.claude/docs/reports.md`
- For each new report category add: interface/output, metrics list, algorithm, UI panel, and MCP tool.
- Update the file-structure tree to include new App-layer modules and UI panel ViewModels.

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| None | - | - | All files map to an existing pattern in the codebase. |

## Metadata

**Analog search scope:** `src/Valt.Infra/Mcp/Tools/`, `src/Valt.Infra/Mcp/Server/`, `src/Valt.UI/Lang/`, `.claude/docs/`, `tests/Valt.Tests/Infra/Mcp/Tools/`, `tests/Valt.Tests/UI/Screens/`
**Files scanned:** 15
**Pattern extraction date:** 2026-08-12
