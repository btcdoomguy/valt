using System.Collections.Frozen;
using System.Collections.Immutable;
using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Infra.Modules.Reports.MonthlyTotals;

public class MonthlyTotalsReport : IMonthlyTotalsReport
{
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalDatabase _localDatabase;
    private readonly IClock _clock;
    private readonly ILogger<MonthlyTotalsReport> _logger;

    public MonthlyTotalsReport(IPriceDatabase priceDatabase, ILocalDatabase localDatabase, IClock clock,
        ILogger<MonthlyTotalsReport> logger)
    {
        _priceDatabase = priceDatabase;
        _localDatabase = localDatabase;
        _clock = clock;
        _logger = logger;
    }

    public async Task<MonthlyTotalsData> GetAsync(DateOnly baseDate, DateOnlyRange displayRange, FiatCurrency currency)
    {
        var monthYear = new DateOnly(baseDate.Year, baseDate.Month, 1);
        //loads all in-memory
        var accounts = _localDatabase.GetAccounts().FindAll().ToImmutableList();
        var transactions = _localDatabase.GetTransactions().FindAll().ToImmutableList();

        if (transactions.Count == 0)
            throw new ApplicationException("No transactions found");

        var minDate = transactions.Min(x => x.Date);
        var maxDate = new DateOnly(monthYear.Year, monthYear.Month, 1).AddMonths(1).AddDays(-1).ToValtDateTime();

        if (_clock.GetCurrentLocalDate().ToValtDateTime() < maxDate)
        {
            //limit to the last day before closing the rates
            maxDate = _clock.GetCurrentLocalDate().ToValtDateTime();
        }

        var btcRates = _priceDatabase.GetBitcoinData().Find(x => x.Date >= minDate && x.Date <= maxDate)
            .ToImmutableList();
        var fiatRates = _priceDatabase.GetFiatData().Find(x => x.Date >= minDate && x.Date <= maxDate)
            .ToImmutableList();

        var calculator = new Calculator(currency, accounts, transactions, btcRates, fiatRates, minDate,
            maxDate, displayRange);

        try
        {
            return await calculator.CalculateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calculating monthly totals");
            throw;
        }
    }

    private class Calculator
    {
        private readonly FiatCurrency _currency;
        private readonly FrozenDictionary<ObjectId, AccountEntity> _accounts;
        private readonly ImmutableList<TransactionEntity> _transactions;
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;
        private readonly DateOnlyRange _displayRange;
        private readonly FrozenDictionary<DateTime, BitcoinDataEntity> _btcRates;
        private readonly FrozenDictionary<DateTime, ImmutableList<FiatDataEntity>> _fiatRates;

        private readonly FrozenDictionary<DateTime, ImmutableList<TransactionEntity>> _transactionsByDate;
        private readonly FrozenDictionary<DateTime, ImmutableList<ObjectId>> _accountsOfDate;

        public Calculator(FiatCurrency currency,
            ImmutableList<AccountEntity> accounts,
            ImmutableList<TransactionEntity> transactions,
            ImmutableList<BitcoinDataEntity> btcRates,
            ImmutableList<FiatDataEntity> fiatRates,
            DateTime startDate,
            DateTime endDate,
            DateOnlyRange displayRange)
        {
            _currency = currency;
            _accounts = accounts.ToFrozenDictionary(x => x.Id);
            _transactions = transactions;
            _startDate = startDate;
            _endDate = endDate;
            _displayRange = displayRange;
            _btcRates = btcRates.ToFrozenDictionary(x => x.Date);
            _fiatRates = fiatRates.GroupBy(x => x.Date).ToFrozenDictionary(x => x.Key, x => x.ToImmutableList());

            //prepare some useful indexes
            _transactionsByDate =
                _transactions
                    .GroupBy(x => x.Date)
                    .ToFrozenDictionary(x => x.Key, x => x.ToImmutableList());
            _accountsOfDate = _transactionsByDate.ToFrozenDictionary(x => x.Key,
                x => x.Value
                    .SelectMany(y => new[] { y.FromAccountId, y.ToAccountId ?? ObjectId.Empty })
                    .Where(y => y != ObjectId.Empty)
                    .Distinct().ToImmutableList());
        }

        public async Task<MonthlyTotalsData> CalculateAsync()
        {
            var monthlyTotals = new Dictionary<DateOnly, MonthlyTotal>();

            var accountTotals = new Dictionary<ObjectId, decimal>();
            var bitcoinCurrentScanDateTotals = 0m;

            var accountIncomeTotals = new Dictionary<ObjectId, decimal>();
            var accountExpenseTotals = new Dictionary<ObjectId, decimal>();
            var accountPurchaseTotals = new Dictionary<ObjectId, decimal>();
            var accountSoldTotals = new Dictionary<ObjectId, decimal>();

            var currentScanDate = _startDate.AddDays(-1);
            while (currentScanDate < _endDate)
            {
                currentScanDate = currentScanDate.AddDays(1);
                if (_accountsOfDate.TryGetValue(currentScanDate, out var accountsOfDate))
                {
                    foreach (var accountId in accountsOfDate)
                    {
                        var account = _accounts[accountId];

                        if (!accountTotals.ContainsKey(accountId))
                        {
                            accountTotals[accountId] =
                                account.AccountEntityType == AccountEntityType.Fiat
                                    ? account.InitialAmount
                                    : account.InitialAmount / 100_000_000m;

                            if (account.AccountEntityType == AccountEntityType.Bitcoin)
                                bitcoinCurrentScanDateTotals += accountTotals[accountId];
                        }

                        if (!accountIncomeTotals.ContainsKey(accountId))
                            accountIncomeTotals[accountId] = 0;
                        if (!accountExpenseTotals.ContainsKey(accountId))
                            accountExpenseTotals[accountId] = 0;
                        if (!accountPurchaseTotals.ContainsKey(accountId))
                            accountPurchaseTotals[accountId] = 0;
                        if (!accountSoldTotals.ContainsKey(accountId))
                            accountSoldTotals[accountId] = 0;

                        var transactionsOfDate = _transactionsByDate[currentScanDate];

                        var fromAccount = transactionsOfDate.Where(x => x.FromAccountId == accountId);
                        var toAccount = transactionsOfDate.Where(x => x.ToAccountId == accountId);

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            var change = fromAccount.Sum(x => x.FromSatAmount.GetValueOrDefault() / 100_000_000m);
                            change += toAccount.Sum(x => x.ToSatAmount.GetValueOrDefault() / 100_000_000m);

                            accountTotals[accountId] += change;
                            bitcoinCurrentScanDateTotals += change;

                            var incomeValue = fromAccount
                                .Where(x => x.Type == TransactionEntityType.Bitcoin && x.FromSatAmount > 0)
                                .Sum(x => x.FromSatAmount.GetValueOrDefault() / 100_000_000m);
                            var expenseValue = fromAccount
                                .Where(x => x.Type == TransactionEntityType.Bitcoin && x.FromSatAmount < 0)
                                .Sum(x => x.FromSatAmount.GetValueOrDefault() / 100_000_000m);
                            var purchaseValue = toAccount
                                .Where(x => x.Type == TransactionEntityType.FiatToBitcoin && x.ToSatAmount > 0)
                                .Sum(x => x.ToSatAmount.GetValueOrDefault() / 100_000_000m);
                            var soldValue = fromAccount
                                .Where(x => x.Type == TransactionEntityType.BitcoinToFiat && x.FromSatAmount < 0)
                                .Sum(x => x.FromSatAmount.GetValueOrDefault() / 100_000_000m);
                            
                            accountIncomeTotals[accountId] += incomeValue;
                            accountExpenseTotals[accountId] += expenseValue;
                            accountPurchaseTotals[accountId] += purchaseValue;
                            accountSoldTotals[accountId] += soldValue;
                        }
                        else
                        {
                            var change = fromAccount.Sum(x => x.FromFiatAmount.GetValueOrDefault());
                            change += toAccount.Sum(x => x.ToFiatAmount.GetValueOrDefault());
                            accountTotals[accountId] += change;

                            var incomeValue = fromAccount
                                .Where(x => x.Type == TransactionEntityType.Fiat && x.FromFiatAmount > 0)
                                .Sum(x => x.FromFiatAmount.GetValueOrDefault());
                            var expenseValue = fromAccount
                                .Where(x => x.Type == TransactionEntityType.Fiat && x.FromFiatAmount < 0)
                                .Sum(x => x.FromFiatAmount.GetValueOrDefault());
                            
                            accountIncomeTotals[accountId] += incomeValue;
                            accountExpenseTotals[accountId] += expenseValue;
                        }
                    }
                }

                //only calculate totals for last day of month

                if (currentScanDate.Day == DateTime.DaysInMonth(currentScanDate.Year, currentScanDate.Month) ||
                    currentScanDate == _endDate)
                {
                    var monthlyValueChange = 0m;
                    var monthlyIncomeChange = 0m;
                    var monthlyExpenseChange = 0m;
                    var monthlyBitcoinIncomeChange = 0m;
                    var monthlyBitcoinExpenseChange = 0m;
                    var monthlyBitcoinPurchaseChange = 0m;
                    var monthlyBitcoinSoldChange = 0m;
                    foreach (var account in _accounts.Values)
                    {
                        if (!accountTotals.ContainsKey(account.Id))
                            continue;

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            //convert it to dollar, then convert back to main fiat currency
                            monthlyValueChange +=
                                GetFiatRateAt(currentScanDate, _currency) * (accountTotals[account.Id] *
                                                                             GetUsdBitcoinPriceAt(currentScanDate));

                            monthlyBitcoinIncomeChange += accountIncomeTotals[account.Id];
                            monthlyBitcoinExpenseChange += accountExpenseTotals[account.Id];
                            monthlyBitcoinPurchaseChange += accountPurchaseTotals[account.Id];
                            monthlyBitcoinSoldChange += accountSoldTotals[account.Id];
                        }
                        else
                        {
                            if (account.Currency == _currency.Code)
                            {
                                monthlyValueChange += accountTotals[account.Id];
                                monthlyIncomeChange += accountIncomeTotals[account.Id];
                                monthlyExpenseChange += accountExpenseTotals[account.Id];
                            }
                            else
                            {
                                //convert it to dollar, then convert back to main fiat currency
                                var rate = GetFiatRateAt(currentScanDate,
                                    FiatCurrency.GetFromCode(account.Currency!));
                                monthlyValueChange +=
                                    GetFiatRateAt(currentScanDate, _currency) *
                                    (accountTotals[account.Id] / rate);

                                monthlyIncomeChange += GetFiatRateAt(currentScanDate, _currency) *
                                                       (accountIncomeTotals[account.Id] / rate);
                                monthlyExpenseChange += GetFiatRateAt(currentScanDate, _currency) *
                                                        (accountExpenseTotals[account.Id] / rate);
                            }
                        }

                        accountIncomeTotals[account.Id] = 0;
                        accountExpenseTotals[account.Id] = 0;
                        accountPurchaseTotals[account.Id] = 0;
                        accountSoldTotals[account.Id] = 0;
                    }

                    var currentMonthYear = new DateOnly(currentScanDate.Year, currentScanDate.Month, 1);

                    monthlyTotals[currentMonthYear] =
                        new MonthlyTotal(Math.Round(monthlyValueChange, 2), bitcoinCurrentScanDateTotals,
                            Math.Round(monthlyIncomeChange, 2), Math.Round(monthlyExpenseChange, 2),
                            monthlyBitcoinIncomeChange, monthlyBitcoinExpenseChange,
                            monthlyBitcoinPurchaseChange, monthlyBitcoinSoldChange
                        );
                }
            }

            var resultSet = monthlyTotals.Select(monthlyTotal =>
            {
                var previousMonth = monthlyTotal.Key.AddMonths(-1);
                var previousYear = new DateOnly(monthlyTotal.Key.Year - 1, 12, 1);

                var btcMonthlyChange = 0m;
                var fiatMonthlyChange = 0m;
                var btcYearlyChange = 0m;
                var fiatYearlyChange = 0m;

                if (monthlyTotals.TryGetValue(previousMonth, out var previousMonthData))
                {
                    btcMonthlyChange =
                        FinancialCalculator.CalculateImprovementPercentage(previousMonthData.Bitcoin,
                            monthlyTotal.Value.Bitcoin);
                    fiatMonthlyChange =
                        FinancialCalculator.CalculateImprovementPercentage(previousMonthData.Fiat,
                            monthlyTotal.Value.Fiat);
                }

                if (monthlyTotals.TryGetValue(previousYear, out var lastYearData))
                {
                    btcYearlyChange =
                        FinancialCalculator.CalculateImprovementPercentage(lastYearData.Bitcoin,
                            monthlyTotal.Value.Bitcoin);
                    fiatYearlyChange =
                        FinancialCalculator.CalculateImprovementPercentage(lastYearData.Fiat, monthlyTotal.Value.Fiat);
                }

                return new MonthlyTotalsData.Item()
                {
                    MonthYear = monthlyTotal.Key,
                    BtcTotal = monthlyTotal.Value.Bitcoin,
                    BtcMonthlyChange = btcMonthlyChange,
                    BtcYearlyChange = btcYearlyChange,
                    FiatTotal = monthlyTotal.Value.Fiat,
                    FiatMonthlyChange = fiatMonthlyChange,
                    FiatYearlyChange = fiatYearlyChange,
                    BitcoinExpenses = monthlyTotal.Value.BitcoinExpense,
                    BitcoinIncome = monthlyTotal.Value.BitcoinIncome,
                    BitcoinPurchased = monthlyTotal.Value.BitcoinPurchased,
                    BitcoinSold = monthlyTotal.Value.BitcoinSold,
                    Income = monthlyTotal.Value.Income,
                    Expenses = monthlyTotal.Value.Expenses,
                };
            }).ToList();

            //grab only the data of the display range filter

            resultSet = resultSet.Where(x => x.MonthYear >= _displayRange.Start && x.MonthYear <= _displayRange.End)
                .ToList();

            return new MonthlyTotalsData()
            {
                MainCurrency = _currency,
                Items = resultSet
            };
        }

        private decimal GetFiatRateAt(DateTime date, FiatCurrency currency)
        {
            if (currency == FiatCurrency.Usd)
                return 1;

            var scanDate = date.Date;
            var currencyCode = currency.Code;

            while (scanDate > date.AddDays(-5)) //considers 5 days to grab because of holidays and weekends
            {
                _ = _fiatRates.TryGetValue(scanDate, out var rates);

                var entry = rates?.SingleOrDefault(x => x.Currency == currencyCode);

                if (entry is not null)
                    return entry.Price;

                scanDate = scanDate.AddDays(-1);
            }

            throw new ApplicationException($"Could not find fiat rate for {currencyCode} on {date}");
        }

        private decimal GetUsdBitcoinPriceAt(DateTime date)
        {
            var scanDate = date; //considers 5 days to grab because of possible empty days

            while (scanDate > date.AddDays(-5))
            {
                if (_btcRates.TryGetValue(scanDate, out var btcRates))
                    return btcRates.Price;

                scanDate = scanDate.AddDays(-1);
            }

            throw new ApplicationException($"Could not find btc rate on {date}");
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
        decimal BitcoinSold);
}