# Reports Module

Financial analysis and dashboarding for wealth tracking, expense analysis, and statistics.

## Infrastructure Layer (Valt.Infra/Modules/Reports/)

### Data Provider System

**IReportDataProvider** - Central abstraction for report data access

Pre-indexed, frozen collections for efficient lookups:
- `Accounts` - Account entities by ObjectId
- `Categories` - Category entities by ObjectId
- `TransactionsByDate` - Grouped transactions indexed by date
- `BtcRates` / `FiatRates` - Historical prices indexed by date

**Key Methods:**
- `GetFiatRateAt(DateOnly, FiatCurrency)` - Exchange rates with binary search
- `GetUsdBitcoinPriceAt(DateOnly)` - Historical BTC price

**Performance Features:**
- Frozen dictionaries and immutable arrays
- Binary search for date lookups
- Cutoff date: 2010-01-01

### Report Types

#### MonthlyTotalsReport

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

#### ExpensesByCategoryReport

**Interface:** `IExpensesByCategoryReport`
**Output:** `ExpensesByCategoryData`

Breaks down spending by transaction category.

**Features:**
- Only counts debit transactions (spending)
- Multi-currency conversion: source -> USD -> target
- Supports account/category filtering
- Hierarchical categories displayed as "Parent >> Child"

#### AllTimeHighReport

**Interface:** `IAllTimeHighReport`
**Output:** `AllTimeHighData`

Tracks portfolio's all-time high value.

**Output:**
```csharp
record AllTimeHighData(DateOnly Date, FiatCurrency Currency, FiatValue Value,
    decimal DeclineFromAth) {
    public DateOnly? MaxDrawdownDate { get; init; }
    public decimal? MaxDrawdownPercent { get; init; }
}
```

**Algorithm:**
1. Scans all days from first transaction to yesterday
2. Accumulates account balances with transactions
3. Calculates total wealth each day in target currency
4. Tracks ATH and maximum drawdown

#### StatisticsReport

**Interface:** `IStatisticsReport`
**Output:** `StatisticsData`

Calculates median monthly expenses and wealth coverage.

**Output:**
- `MedianMonthlyExpenses` - Last 12 months median
- `WealthCoverageMonths` - How many months wealth covers
- `WealthCoverageFormatted` - e.g., "1y 3m"
- Previous period comparison with evolution percentage
- Satoshi-based metrics for BTC expenses

## Spending Analytics

v0.7 adds spending-analytics queries in the Application layer and exposes them through the Reports tab and the MCP server.

### App Layer (`Valt.App/Modules/SpendingAnalytics`)

#### Queries

**`GetBurnRateQuery`** — Returns current-month burn-rate metrics.

- `CurrentWealthInFiat` (`decimal`) — total wealth in the main fiat currency, used to compute wealth coverage.
- `CategoryIds` (`string[]`) — optional excluded-category filter.
- `AccountIds` (`string[]`) — optional account filter.

**`GetFixedVsVariableQuery`** — Returns a month-by-month fixed vs variable expense breakdown.

- `From` / `To` (`DateOnly`) — date range.
- `CategoryIds` (`string[]`) — optional included-category filter.
- `AccountIds` (`string[]`) — optional account filter.

#### DTOs

**`BurnRateDataDto`**

- `HasData` (`bool`) — false when there are no expense transactions in the current month.
- `SpentSoFar` (`decimal`) — total expenses from the start of the current month through yesterday.
- `AvgDailySpend` (`decimal`) — `SpentSoFar / DayOfMonth`.
- `ProjectedMonthEnd` (`decimal?`) — projected full-month spend; only populated on or after day 5 to avoid early-month noise.
- `MedianMonthlyExpenses` (`decimal`) — 12-month median of monthly expenses.
- `VsMedianPercent` (`decimal?`) — percent difference between projected month-end and the median.
- `DayOfMonth` (`int`) — current day of month.
- `PrimaryCurrency` (`string`) — main fiat currency code.

**`FixedVsVariableDataDto`**

- `Months` (`IReadOnlyList<FixedVsVariableMonthDto>`) — one entry per month in the requested range.
- `HasNoFixedExpenses` (`bool`) — true when the user has no fixed expenses registered at all.
- `PrimaryCurrency` (`string`) — main fiat currency code.

**`FixedVsVariableMonthDto`**

- `Month` (`DateOnly`) — first day of the month.
- `FixedTotal` (`decimal`) — expenses linked to paid fixed-expense records.
- `VariableTotal` (`decimal`) — all other debit transactions.

### UI Layer

#### Burn Rate Dashboard Card

**`BurnRatePanelViewModel`** (`Valt.UI/Views/Main/Tabs/Reports/Panels/`)

- Dashboard card rendered through `DashboardDataUserControl`.
- Always visible; shows an empty state when `HasData` is false.
- Rows: spent so far, average daily spend, projected month-end, median month, vs median.
- Uses the category filter set by `SetCategoryFilter`.

#### Fixed vs Variable Chart

**`FixedVsVariableChartData`** (`Valt.UI/Views/Main/Tabs/Reports/`)

- Stacked column chart with fixed (blue) and variable (orange) series.
- Emits one month per row for every month in the requested date range.
- Displays a "no fixed expenses registered" hint when `HasNoFixedExpenses` is true, or a "no expense data for this period" hint when the series are all zero.

### MCP Tool

**`ReportTools.GetSpendingAnalytics`** (`Valt.Infra/Mcp/Tools/ReportTools.cs`)

Returns a combined `SpendingAnalyticsResultDto` containing both burn-rate and fixed-vs-variable sub-sections.

Parameters:

- `startDate` / `endDate` (`string`, `yyyy-MM-dd`) — date range passed to fixed-vs-variable.
- `currencyCode` (`string`) — currency code for consistency with other report tools; the underlying queries use the main fiat currency from settings.
- `currentWealthInFiat` (`decimal`) — passed to burn-rate.
- `accountIds` / `categoryIds` (`string?`, comma-separated) — optional filters applied to both queries.

Result structure:

```csharp
SpendingAnalyticsResultDto
  Currency: string
  BurnRate: BurnRateResultDto
    HasData, SpentSoFar, AvgDailySpend, ProjectedMonthEnd,
    MedianMonthlyExpenses, VsMedianPercent, DayOfMonth, PrimaryCurrency
  FixedVsVariable: FixedVsVariableResultDto
    Months: FixedVsVariableMonthResultDto[]
    HasNoFixedExpenses, PrimaryCurrency
```

> Note: Savings rate is not exposed by this MCP tool because the shipped v0.7 codebase does not include a dedicated savings-rate query or UI panel.

## BTC-Denominated Metrics

v0.7 adds a BTC-denominated view of income and spending so users can see how many sats they earned, spent, and stacked each month.

### App Layer (`Valt.App/Modules/BtcDenominatedMetrics`)

#### Queries

**`GetBtcDenominatedMetricsQuery`** — Returns month-by-month sats earned, sats spent, and stack velocity.

- `From` / `To` (`DateOnly`) — date range.
- `CategoryIds` (`string[]`) — optional included-category filter.
- `AccountIds` (`string[]`) — optional account filter.

#### DTOs

**`BtcDenominatedMetricsDataDto`**

- `Months` (`IReadOnlyList<BtcDenominatedMetricsMonthDto>`) — one entry per month in the requested range.
- `PrimaryCurrency` (`string`) — main fiat currency code.

**`BtcDenominatedMetricsMonthDto`**

- `Month` (`DateOnly`) — first day of the month.
- `SatsEarned` (`long`) — income denominated in satoshis.
- `SatsSpent` (`long`) — expenses denominated in satoshis.
- `StackVelocity` (`long`) — net sats accumulated (`SatsEarned - SatsSpent`).

**Algorithm:**
1. Loads transactions in the requested date range, filtered by accounts and categories.
2. For each income or expense transaction, converts the fiat amount to satoshis using the BTC price on the transaction date.
3. Aggregates per month: credit transactions become `SatsEarned`, debit transactions become `SatsSpent`.
4. Computes `StackVelocity` as earned minus spent.
5. Emits a zero-value month for every month in the range so line charts have no gaps.

### UI Layer

**"Sats earned & spent" chart** — `BtcDenominatedMetricsChartData`

- Stacked column chart with earned (green) and spent (red) series.
- X-axis shows month labels; Y-axis formats values as sat counts.
- Refreshes from `GetBtcDenominatedMetricsQuery` results.

**"Stack velocity" chart** — `StackVelocityChartData`

- Line chart showing `StackVelocity` per month.
- Uses the same query result as the earned/spent chart.
- Both charts share the same date-range filter and category/account filters.

### MCP Tool

**`ReportTools.GetBtcDenominatedMetrics`** (`src/Valt.Infra/Mcp/Tools/ReportTools.cs`)

Parameters:

- `startDate` / `endDate` (`string`, `yyyy-MM-dd`) — date range.
- `currencyCode` (`string`) — currency code for consistency with other report tools; the underlying query uses the main fiat currency from settings.
- `accountIds` / `categoryIds` (`string?`, comma-separated) — optional filters.

Result structure:

```csharp
BtcDenominatedMetricsResultDto
  PrimaryCurrency: string
  IsEmpty: bool
  Months: BtcDenominatedMetricsMonthResultDto[]
    Month, SatsEarned, SatsSpent, StackVelocity
```

> Conversion rule: fiat income/expenses are converted to sats using the BTC price on the transaction date.

## Loan Reports

v0.7 adds loan-specific analytics for BTC-backed loans: monthly interest and fees paid, and the trend of liquidation-price distance over time.

### App Layer (`Valt.App/Modules/LoanReports`)

#### Queries

**`GetLoanReportsQuery`** — Returns monthly loan cost and liquidation-distance data.

- `From` / `To` (`DateOnly`) — date range.
- `CustomBtcPriceUsd` (`decimal?`) — optional custom BTC price used for liquidation-distance calculations.

#### DTOs

**`LoanReportsDataDto`**

- `PrimaryCurrency` (`string`) — main fiat currency code.
- `HasActiveLoans` (`bool`) — true when the user has at least one active BTC-backed loan.
- `CostMonths` (`IReadOnlyList<LoanCostMonthDto>`) — one entry per month.
- `DistanceMonths` (`IReadOnlyList<LiquidationDistanceMonthDto>`) — one entry per month.

**`LoanCostMonthDto`**

- `Month` (`DateOnly`) — first day of the month.
- `CombinedCost` (`decimal`) — total interest + fees across all active loans.
- `Interest` (`decimal`) — interest portion.
- `Fees` (`decimal`) — fees portion.

**`LiquidationDistanceMonthDto`**

- `Month` (`DateOnly`) — first day of the month.
- `DistanceToLiquidation` (`decimal`) — percentage distance to the closest liquidation price.
- `ClosestLoanName` (`string`) — name of the loan with the smallest distance.

**Algorithm:**
1. Identifies active BTC-backed loans from the asset portfolio.
2. For each month in the range, walks the loan-state timeline (latest snapshot on or before each day) to accumulate interest and fees.
3. Converts each loan's contribution individually to the main currency before accumulating, preventing compounding re-conversion when multiple loans use different currencies.
4. Computes liquidation distance as the percentage gap between the current BTC price and the closest loan's liquidation price; color-codes the line by risk.

### UI Layer

**"Loans & Leverage Reports" section**

- Appears only when `HasActiveLoans` is true; hidden otherwise.
- **Cost chart** — `LoanCostChartData`: stacked column chart showing combined interest + fees per month.
- **Liquidation-distance chart** — `LiquidationDistanceChartData`: line chart showing distance-to-liquidation percentage per month, color-coded by risk.

### MCP Tool

**`ReportTools.GetLoanReports`** (`src/Valt.Infra/Mcp/Tools/ReportTools.cs`)

Parameters:

- `startDate` / `endDate` (`string`, `yyyy-MM-dd`) — date range.
- `currencyCode` (`string`) — main fiat currency code.
- `customBtcPriceUsd` (`decimal?`) — optional custom BTC price for liquidation-distance calculations.

Result structure:

```csharp
LoanReportsResultDto
  PrimaryCurrency: string
  HasActiveLoans: bool
  CostMonths: LoanCostMonthResultDto[]
    Month, CombinedCost, Interest, Fees
  DistanceMonths: LiquidationDistanceMonthResultDto[]
    Month, DistanceToLiquidation, ClosestLoanName
```

## Wealth Performance

v0.7 exposes wealth-performance metrics through existing AllTimeHigh and WealthOverview reports, plus the new "days under water" metric on the All Time High dashboard panel.

### Reused Reports

**`IAllTimeHighReport`** → `AllTimeHighData`

Tracks the portfolio's all-time-high wealth value and related drawdown metrics. In v0.7 the data object was extended with:

- `DaysUnderWater` (`int`) — number of whole days between the report end date (yesterday) and the ATH date. Zero when the ATH occurred yesterday; positive otherwise.
- `MaxDrawdownDate` / `MaxDrawdownPercent` — the deepest drawdown from ATH and when it occurred.

**`IWealthOverviewReport`** → `WealthOverviewData`

Returns total wealth over time in fiat and BTC for a selected period (daily, weekly, monthly, yearly). Each item contains:

- `PeriodEnd` (`DateOnly`) — end of the period.
- `Label` (`string`) — human-readable period label.
- `FiatTotal` (`decimal`) — total wealth in the main fiat currency.
- `BtcTotal` (`decimal`) — total wealth in BTC.

### UI Layer

**All Time High dashboard panel**

- Implemented v0.7 metric: `Reports_AllTimeHigh_DaysUnderWater` row added to the existing All Time High dashboard panel (`ReportsViewModel.FetchAllTimeHighDataAsync`).
- The row displays the number of days since the portfolio's all-time high.

**Wealth Overview chart**

- Existing `WealthOverviewChartData` continues to render the wealth-over-time series; no new v0.7 chart was added for this category.

### Deferred Metrics

The following wealth-performance metrics are **not implemented** in the shipped v0.7 codebase and belong to the v2 backlog:

- **WLT-01** — Net worth CAGR / compound growth in fiat and BTC terms.
- **WLT-02** — Fiat vs BTC allocation percentage over time.
- **WLT-03** — Best and worst months ranked by wealth delta.

> These metrics are intentionally omitted from the MCP tool surface and the Reports tab UI until they are implemented.

### MCP Tool

**`ReportTools.GetWealthPerformanceMetrics`** (`src/Valt.Infra/Mcp/Tools/ReportTools.cs`)

Returns a combined `WealthPerformanceMetricsResultDto` with nested `AllTimeHigh` and `WealthOverview` sections. It reuses the existing reports rather than introducing new queries.

Parameters:

- `currencyCode` (`string`) — main fiat currency code.
- `maxDataPoints` (`int`, default: 12) — number of wealth-overview items to return.

Result structure:

```csharp
WealthPerformanceMetricsResultDto
  Currency: string
  AllTimeHigh: AllTimeHighSectionDto
    DaysUnderWater, AthValue, AthDate, DeclineFromAthPercent
  WealthOverview: WealthOverviewSectionDto
    Currency, Period, Items: WealthOverviewItemResultDto[]
      PeriodEnd, Label, FiatTotal, BtcTotal
```

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
- `UnloadData()` clears cache when inactive (memory optimization)
- Debounced filter updates (300ms)

**Report Fetching:**
- `FetchAllReportsAsync()` - Parallel fetch via `Task.WhenAll`
- Individual fetch methods for each report type

### Chart Components

#### MonthlyTotalsChartData

Dual-axis line chart showing wealth trends.

**Features:**
- Dual Y-axes: Fiat (left, blue), Bitcoin (right, orange)
- Smooth interpolation with geometric points
- Semi-transparent fill areas
- Month labels on X-axis

**Colors:**
- Fiat: Blue shades (#0566e9, #559dff, #0951b2)
- Bitcoin: Orange shades (#ffa122, #ffcc88, #e98805)

#### ExpensesByCategoryChartData

Horizontal bar chart for category breakdown.

**Features:**
- Dynamic height based on category count
- Individual colors from category Icon
- Percentage tooltip
- Data labels with formatted currency

### Dashboard Components

**DashboardData** - Record with title and RowItem collection
**RowItem** - Key-value pair with optional tooltip

Used for: Wealth summary, ATH display, BTC stack metrics, Statistics

### MonthlyReportItemViewModel

Table row model for monthly data display.

**Formatting:**
- BTC values: "1.23456789 BTC"
- Fiat values: "$1,234.56"
- Percentage changes with +/- signs and color indicators

## Key Patterns

### Multi-Currency Handling
Conversion chain: Source Currency -> USD -> Target Currency

### Performance Optimizations
1. Data caching per tab lifetime
2. Frozen collections for thread safety
3. Binary search for date lookups
4. Pre-indexing during provider creation
5. Debounced filter changes

### Null Safety
- No transactions: empty collections or zero values
- Missing rate data: uses closest available date
- Dates before cutoff: uses earliest available rate

## DI Registration

```csharp
services.AddSingleton<IAllTimeHighReport, AllTimeHighReport>();
services.AddSingleton<IExpensesByCategoryReport, ExpensesByCategoryReport>();
services.AddSingleton<IMonthlyTotalsReport, MonthlyTotalsReport>();
services.AddSingleton<IStatisticsReport, StatisticsReport>();
services.AddSingleton<IReportDataProviderFactory, ReportDataProviderFactory>();
```

## File Structure

```
src/Valt.App/Modules/SpendingAnalytics/
├── Queries/
│   ├── GetBurnRateQuery.cs
│   ├── GetBurnRateHandler.cs
│   ├── GetFixedVsVariableQuery.cs
│   └── GetFixedVsVariableHandler.cs
├── DTOs/
│   ├── BurnRateDataDto.cs
│   └── FixedVsVariableDataDto.cs
└── Contracts/
    ├── IBurnRateQueries.cs
    └── IFixedVsVariableQueries.cs

src/Valt.App/Modules/BtcDenominatedMetrics/
├── Queries/
│   ├── GetBtcDenominatedMetricsQuery.cs
│   └── GetBtcDenominatedMetricsHandler.cs
├── DTOs/
│   └── BtcDenominatedMetricsDataDto.cs
└── Contracts/
    └── IBtcDenominatedMetricsQueries.cs

src/Valt.App/Modules/LoanReports/
├── Queries/
│   ├── GetLoanReportsQuery.cs
│   └── GetLoanReportsHandler.cs
├── DTOs/
│   └── LoanReportsDataDto.cs
└── Contracts/
    └── ILoanReportsQueries.cs

src/Valt.Infra/Modules/Reports/
├── IReportDataProvider.cs
├── ReportDataProvider.cs (+ Factory)
├── MonthlyTotals/
│   ├── IMonthlyTotalsReport.cs
│   ├── MonthlyTotalsReport.cs
│   └── MonthlyTotalsData.cs
├── ExpensesByCategory/
│   ├── IExpensesByCategoryReport.cs
│   ├── ExpensesByCategoryReport.cs
│   └── ExpensesByCategoryData.cs
├── AllTimeHigh/
│   ├── IAllTimeHighReport.cs
│   ├── AllTimeHighReport.cs
│   └── AllTimeHighData.cs
└── Statistics/
    ├── IStatisticsReport.cs
    ├── StatisticsReport.cs
    └── StatisticsData.cs

src/Valt.UI/Views/Main/Tabs/Reports/
├── ReportsViewModel.cs
├── ReportsView.axaml
├── DashboardData.cs
├── DashboardDataUserControl.axaml
├── MonthlyTotalsChartData.cs
├── ExpensesByCategoryChartData.cs
├── FixedVsVariableChartData.cs
├── BtcDenominatedMetricsChartData.cs
├── StackVelocityChartData.cs
├── LoanCostChartData.cs
├── LiquidationDistanceChartData.cs
├── Panels/
│   └── BurnRatePanelViewModel.cs
└── Models/
    └── MonthlyReportItemViewModel.cs
```