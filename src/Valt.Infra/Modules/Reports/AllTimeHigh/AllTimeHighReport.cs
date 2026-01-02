using System.Collections.Frozen;
using System.Collections.Immutable;
using LiteDB;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Infra.Modules.Reports.AllTimeHigh;

internal class AllTimeHighReport : IAllTimeHighReport
{
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalDatabase _localDatabase;
    private readonly IClock _clock;

    public AllTimeHighReport(IPriceDatabase priceDatabase, ILocalDatabase localDatabase, IClock clock)
    {
        _priceDatabase = priceDatabase;
        _localDatabase = localDatabase;
        _clock = clock;
    }

    public async Task<AllTimeHighData> GetAsync(FiatCurrency currency)
    {
        //discard the current day because rates are not closed yet
        var dateUntil = _clock.GetCurrentLocalDate().AddDays(-1);
        //loads all in-memory
        var accounts = _localDatabase.GetAccounts().FindAll().ToImmutableList();
        var transactions = _localDatabase.GetTransactions().FindAll().ToImmutableList();
        
        if (transactions.Count == 0)
            throw new ApplicationException("No transactions found");

        var minDate = transactions.Min(x => x.Date);
        var maxDate = dateUntil.ToValtDateTime();

        var btcRates = _priceDatabase.GetBitcoinData().Find(x => x.Date >= minDate && x.Date <= maxDate)
            .ToImmutableList();
        var fiatRates = _priceDatabase.GetFiatData().Find(x => x.Date >= minDate && x.Date <= maxDate)
            .ToImmutableList();

        var calculator = new Calculator(currency, accounts, transactions, btcRates, fiatRates, minDate, maxDate);

        return await calculator.CalculateAsync();
    }

    private class Calculator
    {
        private readonly FiatCurrency _currency;
        private readonly FrozenDictionary<ObjectId, AccountEntity> _accounts;
        private readonly ImmutableList<TransactionEntity> _transactions;
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;
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
            DateTime endDate)
        {
            _currency = currency;
            _accounts = accounts.ToFrozenDictionary(x => x.Id);
            _transactions = transactions;
            _startDate = startDate;
            _endDate = endDate;
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

        public async Task<AllTimeHighData> CalculateAsync()
        {
            var allTimeHighCurrentDate = DateTime.MinValue;
            var allTimeHighCurrentFiatValue = decimal.Zero;
            var lastDayFiatValue = decimal.Zero;

            // Track max drawdown after ATH
            var maxDrawdownDate = DateTime.MinValue;
            var maxDrawdownValue = decimal.MaxValue;

            var accountCurrentScanDateTotals = new Dictionary<ObjectId, decimal>();

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
                        }

                        var transactionsOfDate = _transactionsByDate[currentScanDate];

                        var fromAccount = transactionsOfDate.Where(x => x.FromAccountId == accountId);
                        var toAccount = transactionsOfDate.Where(x => x.ToAccountId == accountId);

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            var change = fromAccount.Sum(x => x.FromSatAmount.GetValueOrDefault() / 100_000_000m);
                            change += toAccount.Sum(x => x.ToSatAmount.GetValueOrDefault() / 100_000_000m);

                            accountCurrentScanDateTotals[accountId] += change;
                        }
                        else
                        {
                            var change = fromAccount.Sum(x => x.FromFiatAmount.GetValueOrDefault());
                            change += toAccount.Sum(x => x.ToFiatAmount.GetValueOrDefault());

                            accountCurrentScanDateTotals[accountId] += change;
                        }
                    }
                }

                var dateTotal = 0m;
                foreach (var account in _accounts.Values)
                {
                    if (!accountCurrentScanDateTotals.ContainsKey(account.Id))
                        continue;

                    if (account.AccountEntityType == AccountEntityType.Bitcoin)
                    {
                        //convert it to dollar, then convert back to main fiat currency
                        var valueOnUsd =
                            accountCurrentScanDateTotals[account.Id] * GetUsdBitcoinPriceAt(currentScanDate);

                        dateTotal +=
                            GetFiatRateAt(currentScanDate, _currency) * valueOnUsd;
                    }
                    else
                    {
                        if (account.Currency == _currency.Code)
                        {
                            dateTotal += accountCurrentScanDateTotals[account.Id];
                        }
                        else
                        {
                            //convert it to dollar, then convert back to main fiat currency
                            var valueOnUsd =
                                accountCurrentScanDateTotals[account.Id] / GetFiatRateAt(currentScanDate,
                                    FiatCurrency.GetFromCode(account.Currency!));

                            dateTotal +=
                                GetFiatRateAt(currentScanDate, _currency) * valueOnUsd;
                        }
                    }
                }

                if (currentScanDate == _endDate)
                    lastDayFiatValue = dateTotal;

                if (dateTotal > allTimeHighCurrentFiatValue)
                {
                    // New ATH found - reset max drawdown tracking
                    allTimeHighCurrentFiatValue = dateTotal;
                    allTimeHighCurrentDate = currentScanDate;
                    maxDrawdownValue = decimal.MaxValue;
                    maxDrawdownDate = DateTime.MinValue;
                }
                else if (dateTotal < maxDrawdownValue)
                {
                    // New low after ATH - update max drawdown
                    maxDrawdownValue = dateTotal;
                    maxDrawdownDate = currentScanDate;
                }
            }

            var hasAccountsWithoutTransactions = Enumerable.Any(_accounts.Values, account => !accountCurrentScanDateTotals.ContainsKey(account.Id));

            var declineFromAth = Math.Round((Math.Round(lastDayFiatValue / allTimeHighCurrentFiatValue - 1, 4) * 100), 2);

            // Calculate max drawdown percentage (if there was a drawdown after ATH)
            DateOnly? maxDrawdownDateResult = null;
            decimal? maxDrawdownPercent = null;

            if (maxDrawdownDate != DateTime.MinValue && maxDrawdownValue < allTimeHighCurrentFiatValue)
            {
                maxDrawdownDateResult = DateOnly.FromDateTime(maxDrawdownDate);
                maxDrawdownPercent = Math.Round((Math.Round(maxDrawdownValue / allTimeHighCurrentFiatValue - 1, 4) * 100), 2);
            }

            return new AllTimeHighData(DateOnly.FromDateTime(allTimeHighCurrentDate), _currency,
                allTimeHighCurrentFiatValue, declineFromAth)
            {
                HasAccountsWithoutTransactions = hasAccountsWithoutTransactions,
                MaxDrawdownDate = maxDrawdownDateResult,
                MaxDrawdownPercent = maxDrawdownPercent
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
                
                var entry = rates?.FirstOrDefault(x => x.Currency == currencyCode);

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
}