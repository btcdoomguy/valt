using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Infra.Modules.Reports.MonthlyTotals;

internal class MonthlyTotalsReport : IMonthlyTotalsReport
{
    private readonly IClock _clock;
    private readonly ILogger<MonthlyTotalsReport> _logger;

    public MonthlyTotalsReport(IClock clock, ILogger<MonthlyTotalsReport> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public Task<MonthlyTotalsData> GetAsync(DateOnly baseDate, DateOnlyRange displayRange, FiatCurrency currency, IReportDataProvider provider)
    {
        if (provider.AllTransactions.Count == 0)
        {
            throw new ApplicationException("No transactions found");
        }

        var displayRangeEnd = displayRange.End;
        var currentDate = _clock.GetCurrentLocalDate();
        var maxDate = displayRangeEnd < currentDate ? displayRangeEnd : currentDate;

        var calculator = new Calculator(currency, provider, provider.MinTransactionDate, maxDate, displayRange);

        try
        {
            return Task.FromResult(calculator.Calculate());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calculating monthly totals");
            throw;
        }
    }

    private class Calculator
    {
        private const decimal SatoshisPerBitcoin = 100_000_000m;

        private readonly FiatCurrency _currency;
        private readonly IReportDataProvider _provider;
        private readonly DateOnly _startDate;
        private readonly DateOnly _endDate;
        private readonly DateOnlyRange _displayRange;

        public Calculator(
            FiatCurrency currency,
            IReportDataProvider provider,
            DateOnly startDate,
            DateOnly endDate,
            DateOnlyRange displayRange)
        {
            _currency = currency;
            _provider = provider;
            _startDate = startDate;
            _endDate = endDate;
            _displayRange = displayRange;
        }

        public MonthlyTotalsData Calculate()
        {
            var monthlyTotals = new Dictionary<DateOnly, MonthlyTotal>();
            var accountBalances = new Dictionary<ObjectId, decimal>();
            var bitcoinDailyTotal = 0m;
            var accountIncomes = new Dictionary<ObjectId, decimal>();
            var accountExpenses = new Dictionary<ObjectId, decimal>();
            var accountBitcoinPurchases = new Dictionary<ObjectId, decimal>();
            var accountBitcoinSales = new Dictionary<ObjectId, decimal>();

            var currentDate = _startDate.AddDays(-1);
            while (currentDate < _endDate)
            {
                currentDate = currentDate.AddDays(1);

                _provider.AccountsByDate.TryGetValue(currentDate, out var accountsForDate);
                _provider.TransactionsByDate.TryGetValue(currentDate, out var transactionsForDate);

                if (accountsForDate is not null && transactionsForDate is not null)
                {
                    foreach (var accountId in accountsForDate)
                    {
                        var account = _provider.Accounts[accountId];
                        InitializeAccountTotalsIfNeeded(account, accountBalances, ref bitcoinDailyTotal, accountIncomes,
                            accountExpenses, accountBitcoinPurchases, accountBitcoinSales);

                        var fromTransactions = transactionsForDate.Where(x => x.FromAccountId == accountId);
                        var toTransactions = transactionsForDate.Where(x => x.ToAccountId == accountId);

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            UpdateBitcoinAccountTotals(accountBalances, ref bitcoinDailyTotal, accountIncomes,
                                accountExpenses, accountBitcoinPurchases, accountBitcoinSales, accountId,
                                fromTransactions,
                                toTransactions);
                        }
                        else
                        {
                            UpdateFiatAccountTotals(accountBalances, accountIncomes, accountExpenses, accountId,
                                fromTransactions, toTransactions);
                        }
                    }
                }

                if (!IsEndOfMonth(currentDate) && currentDate != _endDate)
                    continue;

                var monthlyData = CalculateMonthlyData(currentDate, accountBalances, accountIncomes,
                    accountExpenses, accountBitcoinPurchases, accountBitcoinSales, ref bitcoinDailyTotal);
                var monthYear = new DateOnly(currentDate.Year, currentDate.Month, 1);
                monthlyTotals[monthYear] = monthlyData;

                ResetMonthlyChanges(accountIncomes, accountExpenses, accountBitcoinPurchases, accountBitcoinSales);
            }

            var resultItems = BuildResultItems(monthlyTotals);
            var filteredItems = resultItems
                .Where(x => x.MonthYear >= _displayRange.Start && x.MonthYear <= _displayRange.End).ToList();
            var totals = BuildTotals(filteredItems);

            return new MonthlyTotalsData
            {
                MainCurrency = _currency,
                Items = filteredItems,
                Total = totals
            };
        }


        private static void InitializeAccountTotalsIfNeeded(
            AccountEntity account,
            Dictionary<ObjectId, decimal> accountBalances,
            ref decimal bitcoinDailyTotal,
            Dictionary<ObjectId, decimal> accountIncomes,
            Dictionary<ObjectId, decimal> accountExpenses,
            Dictionary<ObjectId, decimal> accountBitcoinPurchases,
            Dictionary<ObjectId, decimal> accountBitcoinSales)
        {
            var accountId = account.Id;

            if (!accountBalances.ContainsKey(accountId))
            {
                var initialBalance = account.AccountEntityType == AccountEntityType.Fiat
                    ? account.InitialAmount
                    : account.InitialAmount / SatoshisPerBitcoin;

                accountBalances[accountId] = initialBalance;

                if (account.AccountEntityType == AccountEntityType.Bitcoin)
                {
                    bitcoinDailyTotal += initialBalance;
                }
            }

            accountIncomes.TryAdd(accountId, 0);
            accountExpenses.TryAdd(accountId, 0);
            accountBitcoinPurchases.TryAdd(accountId, 0);
            accountBitcoinSales.TryAdd(accountId, 0);
        }

        private static void UpdateBitcoinAccountTotals(
            Dictionary<ObjectId, decimal> accountBalances,
            ref decimal bitcoinDailyTotal,
            Dictionary<ObjectId, decimal> accountIncomes,
            Dictionary<ObjectId, decimal> accountExpenses,
            Dictionary<ObjectId, decimal> accountBitcoinPurchases,
            Dictionary<ObjectId, decimal> accountBitcoinSales,
            ObjectId accountId,
            IEnumerable<TransactionEntity> fromTransactions,
            IEnumerable<TransactionEntity> toTransactions)
        {
            // Single-pass aggregation for fromTransactions to avoid multiple iterations
            var fromBalanceChange = 0m;
            var income = 0m;
            var expense = 0m;
            var sale = 0m;

            foreach (var tx in fromTransactions)
            {
                var satAmount = tx.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin;
                fromBalanceChange += satAmount;

                if (tx.Type == TransactionEntityType.Bitcoin)
                {
                    if (tx.FromSatAmount > 0)
                        income += satAmount;
                    else if (tx.FromSatAmount < 0)
                        expense += satAmount;
                }
                else if (tx.Type == TransactionEntityType.BitcoinToFiat && tx.FromSatAmount < 0)
                {
                    sale += satAmount;
                }
            }

            // Single-pass aggregation for toTransactions
            var toBalanceChange = 0m;
            var purchase = 0m;

            foreach (var tx in toTransactions)
            {
                var satAmount = tx.ToSatAmount.GetValueOrDefault() / SatoshisPerBitcoin;
                toBalanceChange += satAmount;

                if (tx.Type == TransactionEntityType.FiatToBitcoin && tx.ToSatAmount > 0)
                {
                    purchase += satAmount;
                }
            }

            var balanceChange = fromBalanceChange + toBalanceChange;
            accountBalances[accountId] += balanceChange;
            bitcoinDailyTotal += balanceChange;

            accountIncomes[accountId] += income;
            accountExpenses[accountId] += expense;
            accountBitcoinPurchases[accountId] += purchase;
            accountBitcoinSales[accountId] += sale;
        }

        private static void UpdateFiatAccountTotals(
            Dictionary<ObjectId, decimal> accountBalances,
            Dictionary<ObjectId, decimal> accountIncomes,
            Dictionary<ObjectId, decimal> accountExpenses,
            ObjectId accountId,
            IEnumerable<TransactionEntity> fromTransactions,
            IEnumerable<TransactionEntity> toTransactions)
        {
            // Single-pass aggregation for fromTransactions
            var fromBalanceChange = 0m;
            var income = 0m;
            var expense = 0m;

            foreach (var tx in fromTransactions)
            {
                var fiatAmount = tx.FromFiatAmount.GetValueOrDefault();
                fromBalanceChange += fiatAmount;

                if (tx.Type == TransactionEntityType.Fiat)
                {
                    if (tx.FromFiatAmount > 0)
                        income += fiatAmount;
                    else if (tx.FromFiatAmount < 0)
                        expense += fiatAmount;
                }
            }

            // Single-pass for toTransactions
            var toBalanceChange = 0m;
            foreach (var tx in toTransactions)
            {
                toBalanceChange += tx.ToFiatAmount.GetValueOrDefault();
            }

            accountBalances[accountId] += fromBalanceChange + toBalanceChange;
            accountIncomes[accountId] += income;
            accountExpenses[accountId] += expense;
        }

        private MonthlyTotal CalculateMonthlyData(
            DateOnly currentDate,
            Dictionary<ObjectId, decimal> accountBalances,
            Dictionary<ObjectId, decimal> accountIncomes,
            Dictionary<ObjectId, decimal> accountExpenses,
            Dictionary<ObjectId, decimal> accountBitcoinPurchases,
            Dictionary<ObjectId, decimal> accountBitcoinSales,
            ref decimal bitcoinDailyTotal)
        {
            var fiatTotal = 0m;
            var incomeTotal = 0m;
            var expenseTotal = 0m;
            var bitcoinIncomeTotal = 0m;
            var bitcoinExpenseTotal = 0m;
            var bitcoinPurchaseTotal = 0m;
            var bitcoinSaleTotal = 0m;
            var allIncomeInFiat = 0m;
            var allExpensesInFiat = 0m;

            foreach (var account in _provider.Accounts.Values)
            {
                var accountId = account.Id;
                if (!accountBalances.ContainsKey(accountId))
                {
                    continue;
                }

                if (account.AccountEntityType == AccountEntityType.Bitcoin)
                {
                    var usdBitcoinPrice = _provider.GetUsdBitcoinPriceAt(currentDate);
                    fiatTotal += _provider.GetFiatRateAt(currentDate, _currency) * (accountBalances[accountId] * usdBitcoinPrice);

                    bitcoinIncomeTotal += accountIncomes[accountId];
                    bitcoinExpenseTotal += accountExpenses[accountId];
                    bitcoinPurchaseTotal += accountBitcoinPurchases[accountId];
                    bitcoinSaleTotal += accountBitcoinSales[accountId];
                    allIncomeInFiat += _provider.GetFiatRateAt(currentDate, _currency) * (accountIncomes[accountId] * usdBitcoinPrice);
                    allExpensesInFiat += _provider.GetFiatRateAt(currentDate, _currency) * (accountExpenses[accountId] * usdBitcoinPrice);
                }
                else
                {
                    var accountCurrency = FiatCurrency.GetFromCode(account.Currency!);
                    var accountRateToUsd = _provider.GetFiatRateAt(currentDate, accountCurrency);

                    if (account.Currency == _currency.Code)
                    {
                        fiatTotal += accountBalances[accountId];
                        incomeTotal += accountIncomes[accountId];
                        expenseTotal += accountExpenses[accountId];
                        allIncomeInFiat += accountIncomes[accountId];
                        allExpensesInFiat += accountExpenses[accountId];
                    }
                    else
                    {
                        var convertedBalance = accountBalances[accountId] / accountRateToUsd;
                        fiatTotal += _provider.GetFiatRateAt(currentDate, _currency) * convertedBalance;

                        incomeTotal += _provider.GetFiatRateAt(currentDate, _currency) *
                                       (accountIncomes[accountId] / accountRateToUsd);
                        expenseTotal += _provider.GetFiatRateAt(currentDate, _currency) *
                                        (accountExpenses[accountId] / accountRateToUsd);
                        allIncomeInFiat += _provider.GetFiatRateAt(currentDate, _currency) *
                                           (accountIncomes[accountId] / accountRateToUsd);
                        allExpensesInFiat += _provider.GetFiatRateAt(currentDate, _currency) *
                                             (accountExpenses[accountId] / accountRateToUsd);
                    }
                }
            }

            return new MonthlyTotal(
                Math.Round(fiatTotal, 2),
                bitcoinDailyTotal,
                Math.Round(incomeTotal, 2),
                Math.Round(expenseTotal, 2),
                bitcoinIncomeTotal,
                bitcoinExpenseTotal,
                bitcoinPurchaseTotal,
                bitcoinSaleTotal,
                allIncomeInFiat,
                allExpensesInFiat);
        }

        private static void ResetMonthlyChanges(
            Dictionary<ObjectId, decimal> accountIncomes,
            Dictionary<ObjectId, decimal> accountExpenses,
            Dictionary<ObjectId, decimal> accountBitcoinPurchases,
            Dictionary<ObjectId, decimal> accountBitcoinSales)
        {
            // Clear and recreate to avoid Keys.ToList() allocation
            // Using simple foreach on Keys is safe when we set values, not add/remove
            foreach (var key in accountIncomes.Keys)
            {
                accountIncomes[key] = 0;
            }

            foreach (var key in accountExpenses.Keys)
            {
                accountExpenses[key] = 0;
            }

            foreach (var key in accountBitcoinPurchases.Keys)
            {
                accountBitcoinPurchases[key] = 0;
            }

            foreach (var key in accountBitcoinSales.Keys)
            {
                accountBitcoinSales[key] = 0;
            }
        }

        private static List<MonthlyTotalsData.Item> BuildResultItems(Dictionary<DateOnly, MonthlyTotal> monthlyTotals)
        {
            return monthlyTotals.Select(monthly =>
            {
                var previousMonthKey = monthly.Key.AddMonths(-1);
                var previousYearKey = new DateOnly(monthly.Key.Year - 1, 12, 1);

                var btcMonthlyChange = monthlyTotals.TryGetValue(previousMonthKey, out var previousMonth)
                    ? FinancialCalculator.CalculateImprovementPercentage(previousMonth.Bitcoin, monthly.Value.Bitcoin)
                    : 0m;

                var fiatMonthlyChange = monthlyTotals.TryGetValue(previousMonthKey, out previousMonth)
                    ? FinancialCalculator.CalculateImprovementPercentage(previousMonth.Fiat, monthly.Value.Fiat)
                    : 0m;

                var btcYearlyChange = monthlyTotals.TryGetValue(previousYearKey, out var previousYear)
                    ? FinancialCalculator.CalculateImprovementPercentage(previousYear.Bitcoin, monthly.Value.Bitcoin)
                    : 0m;

                var fiatYearlyChange = monthlyTotals.TryGetValue(previousYearKey, out previousYear)
                    ? FinancialCalculator.CalculateImprovementPercentage(previousYear.Fiat, monthly.Value.Fiat)
                    : 0m;

                return new MonthlyTotalsData.Item
                {
                    MonthYear = monthly.Key,
                    BtcTotal = monthly.Value.Bitcoin,
                    BtcMonthlyChange = btcMonthlyChange,
                    BtcYearlyChange = btcYearlyChange,
                    FiatTotal = monthly.Value.Fiat,
                    FiatMonthlyChange = fiatMonthlyChange,
                    FiatYearlyChange = fiatYearlyChange,
                    BitcoinExpenses = monthly.Value.BitcoinExpense,
                    BitcoinIncome = monthly.Value.BitcoinIncome,
                    BitcoinPurchased = monthly.Value.BitcoinPurchased,
                    BitcoinSold = monthly.Value.BitcoinSold,
                    Income = monthly.Value.Income,
                    Expenses = monthly.Value.Expenses,
                    AllExpensesInFiat = monthly.Value.AllExpensesInFiat,
                    AllIncomeInFiat = monthly.Value.AllIncomeInFiat
                };
            }).ToList();
        }


        private MonthlyTotalsData.Totals BuildTotals(List<MonthlyTotalsData.Item> resultItems)
        {
            return new MonthlyTotalsData.Totals()
            {
                BitcoinExpenses = resultItems.Sum(x => x.BitcoinExpenses),
                BitcoinIncome = resultItems.Sum(x => x.BitcoinIncome),
                BitcoinPurchased = resultItems.Sum(x => x.BitcoinPurchased),
                BitcoinSold = resultItems.Sum(x => x.BitcoinSold),
                Income = resultItems.Sum(x => x.Income),
                Expenses = resultItems.Sum(x => x.Expenses),
                AllExpensesInFiat = resultItems.Sum(x => x.AllExpensesInFiat),
                AllIncomeInFiat = resultItems.Sum(x => x.AllIncomeInFiat)
            };
        }

        private static bool IsEndOfMonth(DateOnly date)
        {
            return date.Day == DateTime.DaysInMonth(date.Year, date.Month);
        }
    }

    private record MonthlyTotal(
        decimal Fiat,
        decimal Bitcoin,
        decimal Income,
        decimal Expenses,
        decimal BitcoinIncome,
        decimal BitcoinExpense,
        decimal BitcoinPurchased,
        decimal BitcoinSold,
        decimal AllIncomeInFiat,
        decimal AllExpensesInFiat);
}
