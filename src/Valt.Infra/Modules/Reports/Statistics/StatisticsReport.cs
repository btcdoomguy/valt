using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Reports.MonthlyTotals;

namespace Valt.Infra.Modules.Reports.Statistics;

internal class StatisticsReport : IStatisticsReport
{
    private readonly IClock _clock;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;

    public StatisticsReport(IClock clock, IMonthlyTotalsReport monthlyTotalsReport)
    {
        _clock = clock;
        _monthlyTotalsReport = monthlyTotalsReport;
    }

    public async Task<StatisticsData> GetAsync(
        FiatCurrency currency,
        decimal currentWealthInFiat,
        IReportDataProvider provider,
        IReadOnlySet<string>? excludedCategoryIds = null)
    {
        var currentDate = _clock.GetCurrentLocalDate();

        // Calculate date ranges for both periods
        var currentPeriodEnd = new DateOnly(currentDate.Year, currentDate.Month, 1).AddDays(-1);
        var currentPeriodStart = new DateOnly(currentPeriodEnd.AddMonths(-11).Year, currentPeriodEnd.AddMonths(-11).Month, 1);

        var previousPeriodEnd = currentPeriodStart.AddDays(-1);
        var previousPeriodStart = new DateOnly(previousPeriodEnd.AddMonths(-11).Year, previousPeriodEnd.AddMonths(-11).Month, 1);

        // Calculate fiat medians using existing MonthlyTotalsReport
        var medianExpenses = await CalculateMedianExpensesForPeriodAsync(currency, provider, currentDate, excludedCategoryIds);
        var previousMedianExpenses = await CalculateMedianExpensesForPeriodAsync(currency, provider, currentPeriodStart, excludedCategoryIds);

        // Calculate sat medians directly from transactions (optimized single pass)
        var (currentSatMedian, previousSatMedian) = CalculateSatMediansForBothPeriods(
            provider, currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, excludedCategoryIds);

        var (coverageMonths, coverageFormatted) = CalculateWealthCoverage(currentWealthInFiat, medianExpenses);

        // Calculate fiat evolution
        var hasPreviousPeriod = previousMedianExpenses > 0;
        decimal? evolution = null;
        if (hasPreviousPeriod && medianExpenses > 0)
        {
            evolution = Math.Round((medianExpenses - previousMedianExpenses) / previousMedianExpenses * 100, 2);
        }

        // Calculate sat evolution
        var hasSatData = currentSatMedian > 0;
        var hasPreviousSatPeriod = previousSatMedian > 0;
        decimal? satEvolution = null;
        if (hasSatData && hasPreviousSatPeriod)
        {
            satEvolution = Math.Round((decimal)(currentSatMedian - previousSatMedian) / previousSatMedian * 100, 2);
        }

        return new StatisticsData
        {
            MedianMonthlyExpenses = FiatValue.New(medianExpenses),
            Currency = currency,
            WealthCoverageMonths = coverageMonths,
            WealthCoverageFormatted = coverageFormatted,
            HasMedianMonthlyExpensesPreviousPeriod = hasPreviousPeriod,
            MedianMonthlyExpensesPreviousPeriod = hasPreviousPeriod ? FiatValue.New(previousMedianExpenses) : null,
            MedianMonthlyExpensesEvolution = evolution,
            HasMedianMonthlyExpensesSats = hasSatData,
            MedianMonthlyExpensesSats = hasSatData ? currentSatMedian : null,
            MedianMonthlyExpensesPreviousPeriodSats = hasPreviousSatPeriod ? previousSatMedian : null,
            MedianMonthlyExpensesSatsEvolution = satEvolution
        };
    }

    /// <summary>
    /// Calculates sat medians for both current and previous periods in a single pass over transactions.
    /// This is more efficient than processing transactions twice.
    /// Includes both fiat transactions (using AutoSatAmount) and bitcoin transactions (using FromSatAmount).
    /// </summary>
    private static (long currentMedian, long previousMedian) CalculateSatMediansForBothPeriods(
        IReportDataProvider provider,
        DateOnly currentPeriodStart,
        DateOnly currentPeriodEnd,
        DateOnly previousPeriodStart,
        DateOnly previousPeriodEnd,
        IReadOnlySet<string>? excludedCategoryIds = null)
    {
        // Pre-allocate dictionaries for monthly sat totals
        var currentPeriodMonthlyExpenses = new Dictionary<DateOnly, long>();
        var previousPeriodMonthlyExpenses = new Dictionary<DateOnly, long>();

        // Single pass over all transactions
        foreach (var transaction in provider.AllTransactions)
        {
            // Get the sat amount for this expense, returns null if not applicable
            var satExpense = GetSatExpenseAmount(transaction, excludedCategoryIds);
            if (!satExpense.HasValue)
                continue;

            var transactionDate = DateOnly.FromDateTime(transaction.Date.ToUniversalTime());
            var monthKey = new DateOnly(transactionDate.Year, transactionDate.Month, 1);

            // Check if transaction falls in current period
            if (transactionDate >= currentPeriodStart && transactionDate <= currentPeriodEnd)
            {
                if (!currentPeriodMonthlyExpenses.TryGetValue(monthKey, out var currentTotal))
                    currentTotal = 0;
                currentPeriodMonthlyExpenses[monthKey] = currentTotal + satExpense.Value;
            }
            // Check if transaction falls in previous period
            else if (transactionDate >= previousPeriodStart && transactionDate <= previousPeriodEnd)
            {
                if (!previousPeriodMonthlyExpenses.TryGetValue(monthKey, out var prevTotal))
                    prevTotal = 0;
                previousPeriodMonthlyExpenses[monthKey] = prevTotal + satExpense.Value;
            }
        }

        var currentMedian = CalculateLongMedian(currentPeriodMonthlyExpenses.Values.ToList());
        var previousMedian = CalculateLongMedian(previousPeriodMonthlyExpenses.Values.ToList());

        return (currentMedian, previousMedian);
    }

    /// <summary>
    /// Gets the sat expense amount for a transaction if it qualifies as an expense (debit, not transfer).
    /// Returns null if the transaction is not an expense or doesn't have sat data.
    /// </summary>
    private static long? GetSatExpenseAmount(TransactionEntity transaction, IReadOnlySet<string>? excludedCategoryIds = null)
    {
        // Check if category should be excluded
        if (excludedCategoryIds is not null && transaction.CategoryId is not null &&
            excludedCategoryIds.Contains(transaction.CategoryId.ToString()))
            return null;

        // Check if it's a transfer (has "To" amount) - transfers are not expenses
        var hasToAmount = (transaction.ToFiatAmount.HasValue && transaction.ToFiatAmount.Value != 0) ||
                          (transaction.ToSatAmount.HasValue && transaction.ToSatAmount.Value != 0);
        if (hasToAmount)
            return null;

        // Handle fiat transactions - use AutoSatAmount if available
        if (transaction.Type == TransactionEntityType.Fiat)
        {
            // Check if it's a debit (negative FromFiatAmount means expense)
            if (!transaction.FromFiatAmount.HasValue || transaction.FromFiatAmount.Value >= 0)
                return null;

            // Use AutoSatAmount if available
            if (!transaction.SatAmount.HasValue)
                return null;

            // SatAmount is negative for expenses, return absolute value
            return Math.Abs(transaction.SatAmount.Value);
        }

        // Handle bitcoin transactions - use FromSatAmount directly
        if (transaction.Type == TransactionEntityType.Bitcoin)
        {
            // Check if it's a debit (negative FromSatAmount means expense)
            if (!transaction.FromSatAmount.HasValue || transaction.FromSatAmount.Value >= 0)
                return null;

            // Return absolute value of the expense
            return Math.Abs(transaction.FromSatAmount.Value);
        }

        return null;
    }

    private async Task<decimal> CalculateMedianExpensesForPeriodAsync(
        FiatCurrency currency,
        IReportDataProvider provider,
        DateOnly referenceDate,
        IReadOnlySet<string>? excludedCategoryIds = null)
    {
        if (provider.AllTransactions.Count == 0)
        {
            return 0m;
        }

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
    }

    private static long CalculateLongMedian(List<long> values)
    {
        if (values.Count == 0)
            return 0;

        values.Sort();
        var count = values.Count;

        if (count % 2 == 0)
        {
            // Even number of elements - average the two middle values
            var mid1 = values[count / 2 - 1];
            var mid2 = values[count / 2];
            return (mid1 + mid2) / 2;
        }

        // Odd number of elements - return the middle value
        return values[count / 2];
    }

    private static decimal CalculateMedian(List<decimal> sortedValues)
    {
        var count = sortedValues.Count;
        if (count == 0)
            return 0m;

        if (count % 2 == 0)
        {
            // Even number of elements - average the two middle values
            var mid1 = sortedValues[count / 2 - 1];
            var mid2 = sortedValues[count / 2];
            return Math.Round((mid1 + mid2) / 2, 2);
        }

        // Odd number of elements - return the middle value
        return Math.Round(sortedValues[count / 2], 2);
    }

    private static (int months, string formatted) CalculateWealthCoverage(decimal currentWealth, decimal medianExpenses)
    {
        if (currentWealth <= 0 || medianExpenses <= 0)
        {
            return (0, "0");
        }

        var coverageMonths = (int)Math.Floor(currentWealth / medianExpenses);

        if (coverageMonths < 12)
        {
            return (coverageMonths, $"{coverageMonths}");
        }

        var years = coverageMonths / 12;
        var remainingMonths = coverageMonths % 12;

        if (remainingMonths == 0)
        {
            return (coverageMonths, $"{years}y");
        }

        return (coverageMonths, $"{years}y {remainingMonths}m");
    }
}
