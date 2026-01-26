using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
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
        [Description("Currency code (e.g., 'USD', 'BRL')")] string currencyCode)
    {
        var provider = await providerFactory.CreateAsync();
        var currency = FiatCurrency.GetFromCode(currencyCode);
        var wealthPeriod = (WealthOverviewPeriod)period;

        var data = await report.GetAsync(wealthPeriod, currency, provider);

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
}

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

#endregion
