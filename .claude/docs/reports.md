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
└── Models/
    └── MonthlyReportItemViewModel.cs
```