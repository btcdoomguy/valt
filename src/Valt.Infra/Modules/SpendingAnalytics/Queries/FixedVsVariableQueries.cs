using LiteDB;
using Valt.App.Modules.SpendingAnalytics.Contracts;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.SpendingAnalytics.Queries;

public class FixedVsVariableQueries : IFixedVsVariableQueries
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IReportDataProviderFactory _reportDataProviderFactory;
    private readonly CurrencySettings _currencySettings;

    public FixedVsVariableQueries(
        ILocalDatabase localDatabase,
        IReportDataProviderFactory reportDataProviderFactory,
        CurrencySettings currencySettings)
    {
        _localDatabase = localDatabase;
        _reportDataProviderFactory = reportDataProviderFactory;
        _currencySettings = currencySettings;
    }

    public async Task<FixedVsVariableDataDto> GetFixedVsVariableAsync(GetFixedVsVariableQuery query)
    {
        var primaryCurrency = _currencySettings.MainFiatCurrency;

        // D-10: when the user has no fixed expenses registered, return the empty-state flag
        if (_localDatabase.GetFixedExpenses().Count() == 0)
        {
            return new FixedVsVariableDataDto
            {
                Months = new List<FixedVsVariableMonthDto>(),
                HasNoFixedExpenses = true,
                PrimaryCurrency = primaryCurrency
            };
        }

        // D-06: collect only Paid records that are bound to an actual transaction
        var paidTransactionIds = _localDatabase.GetFixedExpenseRecords()
            .Query()
            .Where(r => r.FixedExpenseRecordStateId == (int)FixedExpenseRecordState.Paid && r.Transaction != null)
            .ToList()
            .Select(r => r.Transaction!.Id)
            .ToHashSet();

        // Load accounts and apply optional account filter
        var allAccountsList = _localDatabase.GetAccounts().FindAll().ToList();
        var selectedAccountIds = query.AccountIds.Length > 0
            ? query.AccountIds.Select(id => new ObjectId(id)).ToList()
            : allAccountsList.Select(a => a.Id).ToList();
        var accountDict = allAccountsList.Where(a => selectedAccountIds.Contains(a.Id)).ToDictionary(a => a.Id);

        var provider = await _reportDataProviderFactory.CreateAsync();

        // Build transaction query with LiteDB-side filtering (mirrors SpendingEvolutionQueries)
        var transactionQuery = _localDatabase.GetTransactions().Query();

        var fromDate = query.From.ToValtDateTime();
        var toDate = query.To.ToValtDateTime();
        transactionQuery = transactionQuery.Where(x => x.Date >= fromDate && x.Date <= toDate);

        if (query.CategoryIds.Length > 0)
        {
            var categoryObjectIds = query.CategoryIds.Select(id => new ObjectId(id)).ToList();
            transactionQuery = transactionQuery.Where(x => categoryObjectIds.Contains(x.CategoryId));
        }

        if (selectedAccountIds.Count < allAccountsList.Count)
        {
            transactionQuery = transactionQuery.Where(x => selectedAccountIds.Contains(x.FromAccountId));
        }

        transactionQuery = transactionQuery.Where(x =>
            (x.FromFiatAmount.HasValue && x.FromFiatAmount.Value < 0) ||
            (x.SatAmount.HasValue && x.FromSatAmount.HasValue && x.FromSatAmount.Value < 0));

        var transactions = transactionQuery.ToList();

        // Aggregate per month, splitting fixed vs variable
        var monthlyData = AggregateByMonth(transactions, accountDict, paidTransactionIds, primaryCurrency, provider);

        // Emit every month in the requested range so the chart axis stays consistent
        // even when a month has no transactions (matches BtcDenominatedMetricsQueries).
        var startMonth = new DateOnly(query.From.Year, query.From.Month, 1);
        var endMonth = new DateOnly(query.To.Year, query.To.Month, 1);
        var sortedMonths = new List<FixedVsVariableMonthDto>();
        for (var month = startMonth; month <= endMonth; month = month.AddMonths(1))
        {
            var key = (month.Year, month.Month);
            var (fixedTotal, variableTotal) = monthlyData.TryGetValue(key, out var current)
                ? (current.FixedTotal, current.VariableTotal)
                : (0m, 0m);
            sortedMonths.Add(new FixedVsVariableMonthDto
            {
                Month = month,
                FixedTotal = fixedTotal,
                VariableTotal = variableTotal
            });
        }

        return new FixedVsVariableDataDto
        {
            Months = sortedMonths,
            HasNoFixedExpenses = false,
            PrimaryCurrency = primaryCurrency
        };
    }

    private static Dictionary<(int Year, int Month), (decimal FixedTotal, decimal VariableTotal)> AggregateByMonth(
        List<TransactionEntity> transactions,
        Dictionary<ObjectId, AccountEntity> accountDict,
        HashSet<ObjectId> paidTransactionIds,
        string primaryCurrency,
        IReportDataProvider provider)
    {
        var monthlyData = new Dictionary<(int Year, int Month), (decimal FixedTotal, decimal VariableTotal)>();

        foreach (var transaction in transactions)
        {
            if (!accountDict.TryGetValue(transaction.FromAccountId, out var account))
                continue;

            var yearMonth = (transaction.Date.Year, transaction.Date.Month);

            if (!monthlyData.ContainsKey(yearMonth))
            {
                monthlyData[yearMonth] = (0m, 0m);
            }

            var current = monthlyData[yearMonth];

            decimal amount;
            string? sourceCurrency;

            if (transaction.FromFiatAmount.HasValue)
            {
                amount = Math.Abs(transaction.FromFiatAmount.Value);
                sourceCurrency = account.Currency;
            }
            else if (transaction.FromSatAmount.HasValue)
            {
                amount = Math.Abs((decimal)transaction.FromSatAmount.Value);
                sourceCurrency = "SATS";
            }
            else
            {
                continue;
            }

            var transactionDate = DateOnly.FromDateTime(transaction.Date.ToUniversalTime());
            var converted = ConvertToPrimaryCurrency(amount, sourceCurrency, transactionDate, primaryCurrency, provider);

            if (paidTransactionIds.Contains(transaction.Id))
            {
                current.FixedTotal += converted;
            }
            else
            {
                current.VariableTotal += converted;
            }

            monthlyData[yearMonth] = current;
        }

        return monthlyData;
    }

    private static decimal ConvertToPrimaryCurrency(decimal amount, string? sourceCurrency, DateOnly date, string targetCurrency, IReportDataProvider provider)
    {
        if (string.IsNullOrEmpty(sourceCurrency) || sourceCurrency == targetCurrency)
            return amount;

        try
        {
            return HistoricalRateConverter.ConvertToFiat(amount, sourceCurrency, targetCurrency, date, provider);
        }
        catch
        {
            return amount;
        }
    }
}
