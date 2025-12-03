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
            maxDate = _clock.GetCurrentLocalDate().AddDays(-1).ToValtDateTime();
        }

        var btcRates = _priceDatabase.GetBitcoinData().Find(x => x.Date >= minDate && x.Date <= maxDate)
            .ToImmutableList();
        var fiatRates = _priceDatabase.GetFiatData().Find(x => x.Date >= minDate && x.Date <= maxDate)
            .ToImmutableList();

        var calculator = new Calculator(currency, monthYear, accounts, transactions, btcRates, fiatRates, minDate,
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
        private readonly DateOnly _monthYear;
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
            DateOnly monthYear,
            ImmutableList<AccountEntity> accounts,
            ImmutableList<TransactionEntity> transactions,
            ImmutableList<BitcoinDataEntity> btcRates,
            ImmutableList<FiatDataEntity> fiatRates,
            DateTime startDate,
            DateTime endDate,
            DateOnlyRange displayRange)
        {
            _currency = currency;
            _monthYear = monthYear;
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

            var accountCurrentScanDateTotals = new Dictionary<ObjectId, decimal>();
            var bitcoinCurrentScanDateTotals = 0m;

            var currentScanDate = _startDate.AddDays(-1);
            while (currentScanDate < _endDate)
            {
                currentScanDate = currentScanDate.AddDays(1);
                if (_accountsOfDate.TryGetValue(currentScanDate, out var accountsOfDate))
                {
                    foreach (var accountId in accountsOfDate)
                    {
                        var account = _accounts[accountId];

                        if (!accountCurrentScanDateTotals.ContainsKey(accountId))
                        {
                            accountCurrentScanDateTotals[accountId] =
                                account.AccountEntityType == AccountEntityType.Fiat
                                    ? account.InitialAmount
                                    : account.InitialAmount / 100_000_000m;

                            if (account.AccountEntityType == AccountEntityType.Bitcoin)
                                bitcoinCurrentScanDateTotals += accountCurrentScanDateTotals[accountId];
                        }

                        var transactionsOfDate = _transactionsByDate[currentScanDate];

                        var fromAccount = transactionsOfDate.Where(x => x.FromAccountId == accountId);
                        var toAccount = transactionsOfDate.Where(x => x.ToAccountId == accountId);

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            var change = fromAccount.Sum(x => x.FromSatAmount.GetValueOrDefault() / 100_000_000m);
                            change += toAccount.Sum(x => x.ToSatAmount.GetValueOrDefault() / 100_000_000m);

                            accountCurrentScanDateTotals[accountId] += change;
                            bitcoinCurrentScanDateTotals += change;
                        }
                        else
                        {
                            var change = fromAccount.Sum(x => x.FromFiatAmount.GetValueOrDefault());
                            change += toAccount.Sum(x => x.ToFiatAmount.GetValueOrDefault());

                            accountCurrentScanDateTotals[accountId] += change;
                        }
                    }
                }

                //only calculate totals for last day of month

                if (currentScanDate.Day == DateTime.DaysInMonth(currentScanDate.Year, currentScanDate.Month) ||
                    currentScanDate == _endDate)
                {
                    var monthlyValueChange = 0m;
                    foreach (var account in _accounts.Values)
                    {
                        if (!accountCurrentScanDateTotals.ContainsKey(account.Id))
                            continue;

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            //convert it to dollar, then convert back to main fiat currency
                            var valueOnUsd =
                                accountCurrentScanDateTotals[account.Id] * GetUsdBitcoinPriceAt(currentScanDate);

                            monthlyValueChange +=
                                GetFiatRateAt(currentScanDate, _currency) * valueOnUsd;
                        }
                        else
                        {
                            if (account.Currency == _currency.Code)
                            {
                                monthlyValueChange += accountCurrentScanDateTotals[account.Id];
                            }
                            else
                            {
                                //convert it to dollar, then convert back to main fiat currency
                                var valueOnUsd =
                                    accountCurrentScanDateTotals[account.Id] / GetFiatRateAt(currentScanDate,
                                        FiatCurrency.GetFromCode(account.Currency!));

                                monthlyValueChange +=
                                    GetFiatRateAt(currentScanDate, _currency) * valueOnUsd;
                            }
                        }
                    }

                    var currentMonthYear = new DateOnly(currentScanDate.Year, currentScanDate.Month, 1);

                    monthlyTotals[currentMonthYear] =
                        new MonthlyTotal(Math.Round(monthlyValueChange, 2), bitcoinCurrentScanDateTotals);
                }
            }

            var lastYear = new DateOnly(_monthYear.Year - 1, 12, 1);

            var resultSet = monthlyTotals.Select(monthlyTotal =>
            {
                var previousMonth = monthlyTotal.Key.AddMonths(-1);

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

                if (monthlyTotals.TryGetValue(lastYear, out var lastYearData))
                {
                    btcYearlyChange = FinancialCalculator.CalculateImprovementPercentage(lastYearData.Bitcoin, monthlyTotal.Value.Bitcoin);
                    fiatYearlyChange = FinancialCalculator.CalculateImprovementPercentage(lastYearData.Fiat, monthlyTotal.Value.Fiat);
                }

                return new MonthlyTotalsData.Item()
                {
                    MonthYear = monthlyTotal.Key,
                    BtcTotal = monthlyTotal.Value.Bitcoin,
                    BtcMonthlyChange = btcMonthlyChange,
                    BtcYearlyChange = btcYearlyChange,
                    FiatTotal = monthlyTotal.Value.Fiat,
                    FiatMonthlyChange = fiatMonthlyChange,
                    FiatYearlyChange = fiatYearlyChange
                };
            }).ToList();
            
            //grab only the data of the display range filter
            
            resultSet = resultSet.Where(x => x.MonthYear >= _displayRange.Start && x.MonthYear <= _displayRange.End).ToList();

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

    private record MonthlyTotal(decimal Fiat, decimal Bitcoin);
}