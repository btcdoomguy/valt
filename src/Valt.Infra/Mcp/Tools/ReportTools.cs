using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.App.Modules.BtcDenominatedMetrics.Queries;
using Valt.App.Modules.LoanReports.DTOs;
using Valt.App.Modules.LoanReports.Queries;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Modules.Reports.MaxBtcStack;
using Valt.Infra.Modules.Reports.ExpensesByCategory;
using Valt.Infra.Modules.Reports.IncomeByCategory;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Infra.Modules.Reports.WealthOverview;

namespace Valt.Infra.Mcp.Tools;

/// <summary>
/// MCP tools for financial reports.
/// </summary>
[McpServerToolType]
public class ReportTools
{
    /// <summary>
    /// Gets monthly totals report showing income, expenses, and bitcoin transactions.
    /// </summary>
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
            Items = data.Items.Select(i => new MonthlyTotalsItemDto
            {
                MonthYear = i.MonthYear.ToString("yyyy-MM"),
                FiatTotal = i.FiatTotal,
                BtcTotal = i.BtcTotal,
                Income = i.Income,
                Expenses = i.Expenses,
                BitcoinPurchased = i.BitcoinPurchased,
                BitcoinSold = i.BitcoinSold,
                BitcoinIncome = i.BitcoinIncome,
                BitcoinExpenses = i.BitcoinExpenses,
                AllIncomeInFiat = i.AllIncomeInFiat,
                AllExpensesInFiat = i.AllExpensesInFiat
            }).ToList(),
            Totals = new MonthlyTotalsSummaryDto
            {
                Income = data.Total.Income,
                Expenses = data.Total.Expenses,
                BitcoinPurchased = data.Total.BitcoinPurchased,
                BitcoinSold = data.Total.BitcoinSold,
                BitcoinIncome = data.Total.BitcoinIncome,
                BitcoinExpenses = data.Total.BitcoinExpenses,
                AllIncomeInFiat = data.Total.AllIncomeInFiat,
                AllExpensesInFiat = data.Total.AllExpensesInFiat
            }
        };
    }

    /// <summary>
    /// Gets wealth overview showing total wealth over time.
    /// </summary>
    [McpServerTool, Description("Get wealth overview showing fiat and BTC totals over time periods (daily, weekly, monthly, yearly)")]
    public static async Task<WealthOverviewResultDto> GetWealthOverview(
        IReportDataProviderFactory providerFactory,
        IWealthOverviewReport report,
        [Description("Period type: 0=Daily, 1=Weekly, 2=Monthly, 3=Yearly")] int period,
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode,
        [Description("Maximum number of data points to show (default: 12)")] int maxDataPoints = 12)
    {
        var provider = await providerFactory.CreateAsync();
        var currency = FiatCurrency.GetFromCode(currencyCode);
        var wealthPeriod = (WealthOverviewPeriod)period;

        var data = await report.GetAsync(wealthPeriod, currency, provider, maxDataPoints);

        return new WealthOverviewResultDto
        {
            Currency = data.MainCurrency.Code,
            Period = data.Period.ToString(),
            Items = data.Items.Select(i => new WealthOverviewItemDto
            {
                PeriodEnd = i.PeriodEnd.ToString("yyyy-MM-dd"),
                Label = i.Label,
                FiatTotal = i.FiatTotal,
                BtcTotal = i.BtcTotal
            }).ToList()
        };
    }

    /// <summary>
    /// Gets expenses breakdown by category.
    /// </summary>
    [McpServerTool, Description("Get expenses breakdown by category for a date range")]
    public static async Task<CategoryBreakdownResultDto> GetExpensesByCategory(
        IReportDataProviderFactory providerFactory,
        IExpensesByCategoryReport report,
        [Description("Start date of the range (format: yyyy-MM-dd)")] string startDate,
        [Description("End date of the range (format: yyyy-MM-dd)")] string endDate,
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode,
        [Description("Optional filter by account IDs (comma-separated)")] string? accountIds = null,
        [Description("Optional filter by category IDs (comma-separated)")] string? categoryIds = null)
    {
        var provider = await providerFactory.CreateAsync();
        var currency = FiatCurrency.GetFromCode(currencyCode);
        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);
        var baseDate = end;
        var range = new DateOnlyRange(start, end);

        var filter = new IExpensesByCategoryReport.Filter(
            ParseAccountIds(accountIds),
            ParseCategoryIds(categoryIds));

        var data = await report.GetAsync(baseDate, range, currency, filter, provider);

        return new CategoryBreakdownResultDto
        {
            Currency = data.MainCurrency.Code,
            Items = data.Items.Select(i => new CategoryBreakdownItemDto
            {
                CategoryId = i.CategoryId.Value,
                CategoryName = i.CategoryName,
                FiatTotal = i.FiatTotal
            }).ToList()
        };
    }

    /// <summary>
    /// Gets income breakdown by category.
    /// </summary>
    [McpServerTool, Description("Get income breakdown by category for a date range")]
    public static async Task<CategoryBreakdownResultDto> GetIncomeByCategory(
        IReportDataProviderFactory providerFactory,
        IIncomeByCategoryReport report,
        [Description("Start date of the range (format: yyyy-MM-dd)")] string startDate,
        [Description("End date of the range (format: yyyy-MM-dd)")] string endDate,
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode,
        [Description("Optional filter by account IDs (comma-separated)")] string? accountIds = null,
        [Description("Optional filter by category IDs (comma-separated)")] string? categoryIds = null)
    {
        var provider = await providerFactory.CreateAsync();
        var currency = FiatCurrency.GetFromCode(currencyCode);
        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);
        var baseDate = end;
        var range = new DateOnlyRange(start, end);

        var filter = new IIncomeByCategoryReport.Filter(
            ParseAccountIds(accountIds),
            ParseCategoryIds(categoryIds));

        var data = await report.GetAsync(baseDate, range, currency, filter, provider);

        return new CategoryBreakdownResultDto
        {
            Currency = data.MainCurrency.Code,
            Items = data.Items.Select(i => new CategoryBreakdownItemDto
            {
                CategoryId = i.CategoryId.Value,
                CategoryName = i.CategoryName,
                FiatTotal = i.FiatTotal
            }).ToList()
        };
    }

    /// <summary>
    /// Gets all-time high wealth data and current decline percentage.
    /// </summary>
    [McpServerTool, Description("Get all-time high wealth data including ATH value, date, and current decline percentage")]
    public static async Task<AllTimeHighResultDto> GetAllTimeHigh(
        IReportDataProviderFactory providerFactory,
        IAllTimeHighReport report,
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode)
    {
        var provider = await providerFactory.CreateAsync();
        var currency = FiatCurrency.GetFromCode(currencyCode);

        var data = await report.GetAsync(currency, provider);

        return new AllTimeHighResultDto
        {
            Currency = data.Currency.Code,
            AthDate = data.Date.ToString("yyyy-MM-dd"),
            AthValue = data.Value.Value,
            DeclineFromAthPercent = data.DeclineFromAth,
            MaxDrawdownDate = data.MaxDrawdownDate?.ToString("yyyy-MM-dd"),
            MaxDrawdownPercent = data.MaxDrawdownPercent,
            HasAccountsWithoutTransactions = data.HasAccountsWithoutTransactions,
            DaysUnderWater = data.DaysUnderWater
        };
    }

    /// <summary>
    /// Gets maximum BTC stack ever held and decline from that peak.
    /// </summary>
    [McpServerTool, Description("Get the maximum BTC stack ever held (lifetime high), the date it occurred, and decline from that peak")]
    public static async Task<MaxBtcStackResultDto> GetMaxBtcStack(
        IReportDataProviderFactory providerFactory,
        IMaxBtcStackReport report,
        [Description("Current BTC stack in satoshis")] long currentStackInSats)
    {
        var provider = await providerFactory.CreateAsync();

        var data = await report.GetAsync(currentStackInSats, provider);

        return new MaxBtcStackResultDto
        {
            MaxStackInSats = data.MaxStackInSats,
            MaxStackInBtc = data.MaxStackInSats / 100_000_000m,
            MaxStackDate = data.Date.ToString("yyyy-MM-dd"),
            CurrentStackInSats = data.CurrentStackInSats,
            CurrentStackInBtc = data.CurrentStackInSats / 100_000_000m,
            DeclineFromMaxPercent = data.DeclineFromMaxPercent,
            HasAccountsWithoutTransactions = data.HasAccountsWithoutTransactions
        };
    }

    /// <summary>
    /// Gets financial statistics including median expenses and wealth coverage.
    /// </summary>
    [McpServerTool, Description("Get financial statistics including median monthly expenses and wealth coverage in months")]
    public static async Task<StatisticsResultDto> GetStatistics(
        IReportDataProviderFactory providerFactory,
        IStatisticsReport report,
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode,
        [Description("Current total wealth in the specified fiat currency")] decimal currentWealthInFiat)
    {
        var provider = await providerFactory.CreateAsync();
        var currency = FiatCurrency.GetFromCode(currencyCode);

        var data = await report.GetAsync(currency, currentWealthInFiat, provider);

        return new StatisticsResultDto
        {
            Currency = data.Currency.Code,
            MedianMonthlyExpenses = data.MedianMonthlyExpenses.Value,
            WealthCoverageMonths = data.WealthCoverageMonths,
            WealthCoverageFormatted = data.WealthCoverageFormatted,
            HasPreviousPeriodData = data.HasMedianMonthlyExpensesPreviousPeriod,
            MedianMonthlyExpensesPreviousPeriod = data.MedianMonthlyExpensesPreviousPeriod?.Value,
            MedianMonthlyExpensesEvolutionPercent = data.MedianMonthlyExpensesEvolution,
            HasSatsData = data.HasMedianMonthlyExpensesSats,
            MedianMonthlyExpensesSats = data.MedianMonthlyExpensesSats,
            MedianMonthlyExpensesSatsEvolutionPercent = data.MedianMonthlyExpensesSatsEvolution
        };
    }

    /// <summary>
    /// Gets spending analytics including burn rate and fixed vs variable expenses.
    /// </summary>
    [McpServerTool, Description("Get spending analytics including burn rate and fixed vs variable expenses for a date range")]
    public static async Task<SpendingAnalyticsResultDto> GetSpendingAnalytics(
        IQueryDispatcher dispatcher,
        [Description("Start date of the range (format: yyyy-MM-dd)")] string startDate,
        [Description("End date of the range (format: yyyy-MM-dd)")] string endDate,
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode,
        [Description("Current total wealth in the specified fiat currency")] decimal currentWealthInFiat,
        [Description("Optional filter by account IDs (comma-separated)")] string? accountIds = null,
        [Description("Optional filter by category IDs (comma-separated)")] string? categoryIds = null)
    {
        var from = DateOnly.Parse(startDate);
        var to = DateOnly.Parse(endDate);
        _ = FiatCurrency.GetFromCode(currencyCode);

        var burnRateTask = dispatcher.DispatchAsync(new GetBurnRateQuery
        {
            CurrentWealthInFiat = currentWealthInFiat,
            AccountIds = ParseOptionalIds(accountIds),
            CategoryIds = ParseOptionalIds(categoryIds)
        });

        var fixedVsVariableTask = dispatcher.DispatchAsync(new GetFixedVsVariableQuery
        {
            From = from,
            To = to,
            AccountIds = ParseOptionalIds(accountIds),
            CategoryIds = ParseOptionalIds(categoryIds)
        });

        await Task.WhenAll(burnRateTask, fixedVsVariableTask);

        var burnRate = await burnRateTask;
        var fixedVsVariable = await fixedVsVariableTask;

        return new SpendingAnalyticsResultDto
        {
            Currency = currencyCode,
            BurnRate = new BurnRateResultDto
            {
                HasData = burnRate.HasData,
                SpentSoFar = burnRate.SpentSoFar,
                AvgDailySpend = burnRate.AvgDailySpend,
                ProjectedMonthEnd = burnRate.ProjectedMonthEnd,
                MedianMonthlyExpenses = burnRate.MedianMonthlyExpenses,
                VsMedianPercent = burnRate.VsMedianPercent,
                DayOfMonth = burnRate.DayOfMonth,
                PrimaryCurrency = burnRate.PrimaryCurrency
            },
            FixedVsVariable = new FixedVsVariableResultDto
            {
                Months = fixedVsVariable.Months.Select(m => new FixedVsVariableMonthResultDto
                {
                    Month = m.Month.ToString("yyyy-MM-dd"),
                    FixedTotal = m.FixedTotal,
                    VariableTotal = m.VariableTotal
                }).ToList(),
                HasNoFixedExpenses = fixedVsVariable.HasNoFixedExpenses,
                PrimaryCurrency = fixedVsVariable.PrimaryCurrency
            }
        };
    }

    private static string[] ParseOptionalIds(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return [];

        return ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

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

    [McpServerTool, Description("Get BTC-denominated metrics including sats earned, sats spent, and stack velocity per month over a date range")]
    public static async Task<BtcDenominatedMetricsResultDto> GetBtcDenominatedMetrics(
        IQueryDispatcher dispatcher,
        [Description("Start date of the range (format: yyyy-MM-dd)")] string startDate,
        [Description("End date of the range (format: yyyy-MM-dd)")] string endDate,
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode,
        [Description("Optional filter by account IDs (comma-separated)")] string? accountIds = null,
        [Description("Optional filter by category IDs (comma-separated)")] string? categoryIds = null)
    {
        var from = DateOnly.Parse(startDate);
        var to = DateOnly.Parse(endDate);
        _ = FiatCurrency.GetFromCode(currencyCode);

        var data = await dispatcher.DispatchAsync(new GetBtcDenominatedMetricsQuery
        {
            From = from,
            To = to,
            AccountIds = ParseOptionalIds(accountIds),
            CategoryIds = ParseOptionalIds(categoryIds)
        });

        return new BtcDenominatedMetricsResultDto
        {
            PrimaryCurrency = data.PrimaryCurrency,
            IsEmpty = data.Months.Count == 0,
            Months = data.Months.Select(m => new BtcDenominatedMetricsMonthResultDto
            {
                Month = m.Month.ToString("yyyy-MM-dd"),
                SatsEarned = m.SatsEarned,
                SatsSpent = m.SatsSpent,
                StackVelocity = m.StackVelocity
            }).ToList()
        };
    }

    /// <summary>
    /// Gets loan reports including monthly interest, fees, and liquidation-price distance trends.
    /// </summary>
    [McpServerTool, Description("Get loan reports including monthly interest/fees and liquidation-price distance trends over a date range")]
    public static async Task<LoanReportsResultDto> GetLoanReports(
        IQueryDispatcher dispatcher,
        [Description("Start date of the range (format: yyyy-MM-dd)")] string startDate,
        [Description("End date of the range (format: yyyy-MM-dd)")] string endDate,
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode,
        [Description("Optional custom BTC price in USD to use for liquidation-distance calculations")] decimal? customBtcPriceUsd = null)
    {
        var from = DateOnly.Parse(startDate);
        var to = DateOnly.Parse(endDate);
        _ = FiatCurrency.GetFromCode(currencyCode);

        var data = await dispatcher.DispatchAsync(new GetLoanReportsQuery
        {
            From = from,
            To = to,
            CustomBtcPriceUsd = customBtcPriceUsd
        });

        return new LoanReportsResultDto
        {
            PrimaryCurrency = data.PrimaryCurrency,
            HasActiveLoans = data.HasActiveLoans,
            CostMonths = data.CostMonths.Select(m => new LoanCostMonthResultDto
            {
                Month = m.Month.ToString("yyyy-MM-dd"),
                CombinedCost = m.CombinedCost,
                Interest = m.Interest,
                Fees = m.Fees
            }).ToList(),
            DistanceMonths = data.DistanceMonths.Select(m => new LiquidationDistanceMonthResultDto
            {
                Month = m.Month.ToString("yyyy-MM-dd"),
                DistanceToLiquidation = m.DistanceToLiquidation,
                ClosestLoanName = m.ClosestLoanName
            }).ToList()
        };
    }

    /// <summary>
    /// Gets wealth-performance metrics: days under water via the all-time-high report
    /// and a monthly wealth-overview series.
    /// </summary>
    /// <remarks>
    /// v0.7 scope note: net-worth CAGR, fiat vs BTC allocation percentage, and best/worst
    /// months are not implemented in the shipped codebase and are intentionally omitted.
    /// </remarks>
    [McpServerTool, Description("Get wealth performance metrics including days under water (ATH) and monthly wealth overview")]
    public static async Task<WealthPerformanceMetricsResultDto> GetWealthPerformanceMetrics(
        IReportDataProviderFactory providerFactory,
        IAllTimeHighReport allTimeHighReport,
        IWealthOverviewReport wealthOverviewReport,
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode,
        [Description("Maximum number of wealth-overview data points (default: 12)")] int maxDataPoints = 12)
    {
        var provider = await providerFactory.CreateAsync();
        var currency = FiatCurrency.GetFromCode(currencyCode);

        var allTimeHigh = await allTimeHighReport.GetAsync(currency, provider);
        var wealthOverview = await wealthOverviewReport.GetAsync(WealthOverviewPeriod.Monthly, currency, provider, maxDataPoints);

        return new WealthPerformanceMetricsResultDto
        {
            Currency = currencyCode,
            AllTimeHigh = new AllTimeHighSectionDto
            {
                DaysUnderWater = allTimeHigh.DaysUnderWater,
                AthValue = allTimeHigh.Value.Value,
                AthDate = allTimeHigh.Date.ToString("yyyy-MM-dd"),
                DeclineFromAthPercent = allTimeHigh.DeclineFromAth
            },
            WealthOverview = new WealthOverviewSectionDto
            {
                Currency = wealthOverview.MainCurrency.Code,
                Period = wealthOverview.Period.ToString(),
                Items = wealthOverview.Items.Select(i => new WealthOverviewItemResultDto
                {
                    PeriodEnd = i.PeriodEnd.ToString("yyyy-MM-dd"),
                    Label = i.Label,
                    FiatTotal = i.FiatTotal,
                    BtcTotal = i.BtcTotal
                }).ToList()
            }
        };
    }

    #region DTOs

public class SpendingAnalyticsResultDto
{
    public required string Currency { get; init; }
    public required BurnRateResultDto BurnRate { get; init; }
    public required FixedVsVariableResultDto FixedVsVariable { get; init; }
}

public class BurnRateResultDto
{
    public required bool HasData { get; init; }
    public required decimal SpentSoFar { get; init; }
    public required decimal AvgDailySpend { get; init; }
    public decimal? ProjectedMonthEnd { get; init; }
    public required decimal MedianMonthlyExpenses { get; init; }
    public decimal? VsMedianPercent { get; init; }
    public required int DayOfMonth { get; init; }
    public required string PrimaryCurrency { get; init; }
}

public class FixedVsVariableResultDto
{
    public required IReadOnlyList<FixedVsVariableMonthResultDto> Months { get; init; }
    public required bool HasNoFixedExpenses { get; init; }
    public required string PrimaryCurrency { get; init; }
}

public class FixedVsVariableMonthResultDto
{
    public required string Month { get; init; }
    public required decimal FixedTotal { get; init; }
    public required decimal VariableTotal { get; init; }
}

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
    public required decimal BtcTotal { get; init; }
    public required decimal Income { get; init; }
    public required decimal Expenses { get; init; }
    public required decimal BitcoinPurchased { get; init; }
    public required decimal BitcoinSold { get; init; }
    public required decimal BitcoinIncome { get; init; }
    public required decimal BitcoinExpenses { get; init; }
    public required decimal AllIncomeInFiat { get; init; }
    public required decimal AllExpensesInFiat { get; init; }
}

public class MonthlyTotalsSummaryDto
{
    public required decimal Income { get; init; }
    public required decimal Expenses { get; init; }
    public required decimal BitcoinPurchased { get; init; }
    public required decimal BitcoinSold { get; init; }
    public required decimal BitcoinIncome { get; init; }
    public required decimal BitcoinExpenses { get; init; }
    public required decimal AllIncomeInFiat { get; init; }
    public required decimal AllExpensesInFiat { get; init; }
}

public class WealthOverviewResultDto
{
    public required string Currency { get; init; }
    public required string Period { get; init; }
    public required IReadOnlyList<WealthOverviewItemDto> Items { get; init; }
}

public class WealthOverviewItemDto
{
    public required string PeriodEnd { get; init; }
    public required string Label { get; init; }
    public required decimal FiatTotal { get; init; }
    public required decimal BtcTotal { get; init; }
}

public class CategoryBreakdownResultDto
{
    public required string Currency { get; init; }
    public required IReadOnlyList<CategoryBreakdownItemDto> Items { get; init; }
}

public class CategoryBreakdownItemDto
{
    public required string CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required decimal FiatTotal { get; init; }
}

public class AllTimeHighResultDto
{
    public required string Currency { get; init; }
    public required string AthDate { get; init; }
    public required decimal AthValue { get; init; }
    public required decimal DeclineFromAthPercent { get; init; }
    public string? MaxDrawdownDate { get; init; }
    public decimal? MaxDrawdownPercent { get; init; }
    public required bool HasAccountsWithoutTransactions { get; init; }
    public required int DaysUnderWater { get; init; }
}

public class MaxBtcStackResultDto
{
    public required long MaxStackInSats { get; init; }
    public required decimal MaxStackInBtc { get; init; }
    public required string MaxStackDate { get; init; }
    public required long CurrentStackInSats { get; init; }
    public required decimal CurrentStackInBtc { get; init; }
    public required decimal DeclineFromMaxPercent { get; init; }
    public required bool HasAccountsWithoutTransactions { get; init; }
}

public class StatisticsResultDto
{
    public required string Currency { get; init; }
    public required decimal MedianMonthlyExpenses { get; init; }
    public required int WealthCoverageMonths { get; init; }
    public required string WealthCoverageFormatted { get; init; }
    public required bool HasPreviousPeriodData { get; init; }
    public decimal? MedianMonthlyExpensesPreviousPeriod { get; init; }
    public decimal? MedianMonthlyExpensesEvolutionPercent { get; init; }
    public required bool HasSatsData { get; init; }
    public long? MedianMonthlyExpensesSats { get; init; }
    public decimal? MedianMonthlyExpensesSatsEvolutionPercent { get; init; }
}

public class BtcDenominatedMetricsResultDto
{
    public required string PrimaryCurrency { get; init; }
    public required bool IsEmpty { get; init; }
    public required IReadOnlyList<BtcDenominatedMetricsMonthResultDto> Months { get; init; }
}

public class BtcDenominatedMetricsMonthResultDto
{
    public required string Month { get; init; }
    public required long SatsEarned { get; init; }
    public required long SatsSpent { get; init; }
    public required long StackVelocity { get; init; }
}

public class LoanReportsResultDto
{
    public required string PrimaryCurrency { get; init; }
    public required bool HasActiveLoans { get; init; }
    public required IReadOnlyList<LoanCostMonthResultDto> CostMonths { get; init; }
    public required IReadOnlyList<LiquidationDistanceMonthResultDto> DistanceMonths { get; init; }
}

public class LoanCostMonthResultDto
{
    public required string Month { get; init; }
    public required decimal CombinedCost { get; init; }
    public required decimal Interest { get; init; }
    public required decimal Fees { get; init; }
}

public class LiquidationDistanceMonthResultDto
{
    public required string Month { get; init; }
    public required decimal DistanceToLiquidation { get; init; }
    public required string ClosestLoanName { get; init; }
}

public class WealthPerformanceMetricsResultDto
{
    public required string Currency { get; init; }
    public required AllTimeHighSectionDto AllTimeHigh { get; init; }
    public required WealthOverviewSectionDto WealthOverview { get; init; }
}

public class AllTimeHighSectionDto
{
    public required int DaysUnderWater { get; init; }
    public required decimal AthValue { get; init; }
    public required string AthDate { get; init; }
    public required decimal DeclineFromAthPercent { get; init; }
}

public class WealthOverviewSectionDto
{
    public required string Currency { get; init; }
    public required string Period { get; init; }
    public required IReadOnlyList<WealthOverviewItemResultDto> Items { get; init; }
}

public class WealthOverviewItemResultDto
{
    public required string PeriodEnd { get; init; }
    public required string Label { get; init; }
    public required decimal FiatTotal { get; init; }
    public required decimal BtcTotal { get; init; }
}

#endregion
}
