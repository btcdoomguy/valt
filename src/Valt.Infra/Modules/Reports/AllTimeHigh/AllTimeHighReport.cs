using LiteDB;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Infra.Modules.Reports.AllTimeHigh;

internal class AllTimeHighReport : IAllTimeHighReport
{
    private readonly IClock _clock;

    public AllTimeHighReport(IClock clock)
    {
        _clock = clock;
    }

    public Task<AllTimeHighData> GetAsync(FiatCurrency currency, IReportDataProvider provider)
    {
        if (provider.AllTransactions.Count == 0)
            throw new ApplicationException("No transactions found");

        // Discard the current day because rates are not closed yet
        var maxDate = _clock.GetCurrentLocalDate().AddDays(-1);

        var calculator = new Calculator(currency, provider, provider.MinTransactionDate, maxDate);

        return calculator.CalculateAsync();
    }

    private class Calculator
    {
        private const decimal SatoshisPerBitcoin = 100_000_000m;

        private readonly FiatCurrency _currency;
        private readonly IReportDataProvider _provider;
        private readonly DateOnly _startDate;
        private readonly DateOnly _endDate;

        public Calculator(
            FiatCurrency currency,
            IReportDataProvider provider,
            DateOnly startDate,
            DateOnly endDate)
        {
            _currency = currency;
            _provider = provider;
            _startDate = startDate;
            _endDate = endDate;
        }

        public Task<AllTimeHighData> CalculateAsync()
        {
            var allTimeHighCurrentDate = DateOnly.MinValue;
            var allTimeHighCurrentFiatValue = decimal.Zero;
            var lastDayFiatValue = decimal.Zero;

            // Track max drawdown after ATH
            var maxDrawdownDate = DateOnly.MinValue;
            var maxDrawdownValue = decimal.MaxValue;

            var accountCurrentScanDateTotals = new Dictionary<ObjectId, decimal>();

            var currentScanDate = _startDate.AddDays(-1);
            while (currentScanDate < _endDate)
            {
                currentScanDate = currentScanDate.AddDays(1);
                if (_provider.AccountsByDate.TryGetValue(currentScanDate, out var accountsOfDate))
                {
                    foreach (var accountId in accountsOfDate)
                    {
                        var account = _provider.Accounts[accountId];

                        if (!accountCurrentScanDateTotals.ContainsKey(accountId))
                        {
                            accountCurrentScanDateTotals[accountId] =
                                account.AccountEntityType == AccountEntityType.Fiat
                                    ? account.InitialAmount
                                    : account.InitialAmount / SatoshisPerBitcoin;
                        }

                        var transactionsOfDate = _provider.TransactionsByDate[currentScanDate];

                        var fromAccount = transactionsOfDate.Where(x => x.FromAccountId == accountId);
                        var toAccount = transactionsOfDate.Where(x => x.ToAccountId == accountId);

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            var change = fromAccount.Sum(x => x.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin);
                            change += toAccount.Sum(x => x.ToSatAmount.GetValueOrDefault() / SatoshisPerBitcoin);

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
                foreach (var account in _provider.Accounts.Values)
                {
                    if (!accountCurrentScanDateTotals.ContainsKey(account.Id))
                        continue;

                    if (account.AccountEntityType == AccountEntityType.Bitcoin)
                    {
                        // Convert to USD, then to target fiat currency
                        var valueOnUsd =
                            accountCurrentScanDateTotals[account.Id] * _provider.GetUsdBitcoinPriceAt(currentScanDate);

                        dateTotal +=
                            _provider.GetFiatRateAt(currentScanDate, _currency) * valueOnUsd;
                    }
                    else
                    {
                        if (account.Currency == _currency.Code)
                        {
                            dateTotal += accountCurrentScanDateTotals[account.Id];
                        }
                        else
                        {
                            // Convert to USD, then to target fiat currency
                            var valueOnUsd =
                                accountCurrentScanDateTotals[account.Id] / _provider.GetFiatRateAt(currentScanDate,
                                    FiatCurrency.GetFromCode(account.Currency!));

                            dateTotal +=
                                _provider.GetFiatRateAt(currentScanDate, _currency) * valueOnUsd;
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
                    maxDrawdownDate = DateOnly.MinValue;
                }
                else if (dateTotal < maxDrawdownValue)
                {
                    // New low after ATH - update max drawdown
                    maxDrawdownValue = dateTotal;
                    maxDrawdownDate = currentScanDate;
                }
            }

            var hasAccountsWithoutTransactions = _provider.Accounts.Values.Any(account => !accountCurrentScanDateTotals.ContainsKey(account.Id));

            var declineFromAth = Math.Round((Math.Round(lastDayFiatValue / allTimeHighCurrentFiatValue - 1, 4) * 100), 2);

            // Calculate max drawdown percentage (if there was a drawdown after ATH)
            DateOnly? maxDrawdownDateResult = null;
            decimal? maxDrawdownPercent = null;

            if (maxDrawdownDate != DateOnly.MinValue && maxDrawdownValue < allTimeHighCurrentFiatValue)
            {
                maxDrawdownDateResult = maxDrawdownDate;
                maxDrawdownPercent = Math.Round((Math.Round(maxDrawdownValue / allTimeHighCurrentFiatValue - 1, 4) * 100), 2);
            }

            return Task.FromResult(new AllTimeHighData(allTimeHighCurrentDate, _currency,
                allTimeHighCurrentFiatValue, declineFromAth)
            {
                HasAccountsWithoutTransactions = hasAccountsWithoutTransactions,
                MaxDrawdownDate = maxDrawdownDateResult,
                MaxDrawdownPercent = maxDrawdownPercent
            });
        }
    }
}
