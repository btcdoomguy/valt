using System.Globalization;
using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Infra.Modules.Reports.WealthOverview;

internal class WealthOverviewReport : IWealthOverviewReport
{
    private const int MaxDataPoints = 12;
    private readonly IClock _clock;
    private readonly ILogger<WealthOverviewReport> _logger;

    public WealthOverviewReport(IClock clock, ILogger<WealthOverviewReport> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public Task<WealthOverviewData> GetAsync(WealthOverviewPeriod period, FiatCurrency currency, IReportDataProvider provider)
    {
        if (provider.AllTransactions.Count == 0)
        {
            return Task.FromResult(new WealthOverviewData
            {
                MainCurrency = currency,
                Period = period,
                Items = Array.Empty<WealthOverviewData.Item>()
            });
        }

        var calculator = new Calculator(currency, provider, _clock.GetCurrentLocalDate(), period);

        try
        {
            return Task.FromResult(calculator.Calculate());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calculating wealth overview");
            throw;
        }
    }

    private class Calculator
    {
        private const decimal SatoshisPerBitcoin = 100_000_000m;

        private readonly FiatCurrency _currency;
        private readonly IReportDataProvider _provider;
        private readonly DateOnly _today;
        private readonly WealthOverviewPeriod _period;

        public Calculator(
            FiatCurrency currency,
            IReportDataProvider provider,
            DateOnly today,
            WealthOverviewPeriod period)
        {
            _currency = currency;
            _provider = provider;
            _today = today;
            _period = period;
        }

        public WealthOverviewData Calculate()
        {
            var periodEndDates = GeneratePeriodEndDates();
            var items = new List<WealthOverviewData.Item>();

            foreach (var periodEnd in periodEndDates)
            {
                // Skip periods that end before the earliest transaction
                if (periodEnd < _provider.MinTransactionDate)
                    continue;

                var (fiatTotal, btcTotal) = CalculateWealthAtDate(periodEnd);
                var label = GetLabelForPeriod(periodEnd);

                items.Add(new WealthOverviewData.Item(periodEnd, label, fiatTotal, btcTotal));
            }

            return new WealthOverviewData
            {
                MainCurrency = _currency,
                Period = _period,
                Items = items
            };
        }

        private List<DateOnly> GeneratePeriodEndDates()
        {
            var dates = new List<DateOnly>();

            for (var i = MaxDataPoints - 1; i >= 0; i--)
            {
                var periodEnd = _period switch
                {
                    WealthOverviewPeriod.Daily => _today.AddDays(-i),
                    WealthOverviewPeriod.Weekly => GetSaturdayEndingWeek(_today, i),
                    WealthOverviewPeriod.Monthly => GetEndOfMonth(_today, i),
                    WealthOverviewPeriod.Yearly => GetEndOfYear(_today, i),
                    _ => throw new ArgumentOutOfRangeException()
                };

                dates.Add(periodEnd);
            }

            return dates;
        }

        private static DateOnly GetSaturdayEndingWeek(DateOnly baseDate, int weeksBack)
        {
            var dayOfWeek = (int)baseDate.DayOfWeek;
            // Find upcoming Saturday (or today if Saturday)
            // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
            var daysUntilSaturday = (6 - dayOfWeek + 7) % 7; // 0 if already Saturday
            var thisSaturday = baseDate.AddDays(daysUntilSaturday);
            return thisSaturday.AddDays(-7 * weeksBack);
        }

        private static DateOnly GetEndOfMonth(DateOnly baseDate, int monthsBack)
        {
            var targetMonth = baseDate.AddMonths(-monthsBack);
            return new DateOnly(targetMonth.Year, targetMonth.Month, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month));
        }

        private static DateOnly GetEndOfYear(DateOnly baseDate, int yearsBack)
        {
            return new DateOnly(baseDate.Year - yearsBack, 12, 31);
        }

        private string GetLabelForPeriod(DateOnly periodEnd)
        {
            return _period switch
            {
                WealthOverviewPeriod.Daily => periodEnd.ToString("MMM dd", CultureInfo.InvariantCulture),
                WealthOverviewPeriod.Weekly => periodEnd.ToString("MMM dd", CultureInfo.InvariantCulture),
                WealthOverviewPeriod.Monthly => periodEnd.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                WealthOverviewPeriod.Yearly => periodEnd.Year.ToString(),
                _ => periodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        }

        private (decimal fiatTotal, decimal btcTotal) CalculateWealthAtDate(DateOnly targetDate)
        {
            // For incomplete periods (future period end), use today's date for calculation
            var calculationDate = targetDate > _today ? _today : targetDate;

            var accountBalances = new Dictionary<ObjectId, decimal>();
            var bitcoinTotal = 0m;

            // Initialize all accounts with their initial balances
            foreach (var account in _provider.Accounts.Values)
            {
                InitializeAccountIfNeeded(account, accountBalances, ref bitcoinTotal);
            }

            var startDate = _provider.MinTransactionDate.AddDays(-1);
            var currentDate = startDate;

            while (currentDate <= calculationDate)
            {
                currentDate = currentDate.AddDays(1);

                _provider.AccountsByDate.TryGetValue(currentDate, out var accountsForDate);
                _provider.TransactionsByDate.TryGetValue(currentDate, out var transactionsForDate);

                if (accountsForDate is not null && transactionsForDate is not null)
                {
                    foreach (var accountId in accountsForDate)
                    {
                        var account = _provider.Accounts[accountId];

                        var fromTransactions = transactionsForDate.Where(x => x.FromAccountId == accountId);
                        var toTransactions = transactionsForDate.Where(x => x.ToAccountId == accountId);

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            UpdateBitcoinAccountBalance(accountBalances, ref bitcoinTotal, accountId, fromTransactions, toTransactions);
                        }
                        else
                        {
                            UpdateFiatAccountBalance(accountBalances, accountId, fromTransactions, toTransactions);
                        }
                    }
                }
            }

            // Calculate totals at calculation date
            var fiatTotal = CalculateFiatTotalAtDate(calculationDate, accountBalances);
            var btcTotal = bitcoinTotal;

            return (Math.Round(fiatTotal, 2), btcTotal);
        }

        private void InitializeAccountIfNeeded(
            AccountEntity account,
            Dictionary<ObjectId, decimal> accountBalances,
            ref decimal bitcoinTotal)
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
                    bitcoinTotal += initialBalance;
                }
            }
        }

        private static void UpdateBitcoinAccountBalance(
            Dictionary<ObjectId, decimal> accountBalances,
            ref decimal bitcoinTotal,
            ObjectId accountId,
            IEnumerable<Budget.Transactions.TransactionEntity> fromTransactions,
            IEnumerable<Budget.Transactions.TransactionEntity> toTransactions)
        {
            var balanceChange = fromTransactions.Sum(x => x.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin) +
                                toTransactions.Sum(x => x.ToSatAmount.GetValueOrDefault() / SatoshisPerBitcoin);

            accountBalances[accountId] += balanceChange;
            bitcoinTotal += balanceChange;
        }

        private static void UpdateFiatAccountBalance(
            Dictionary<ObjectId, decimal> accountBalances,
            ObjectId accountId,
            IEnumerable<Budget.Transactions.TransactionEntity> fromTransactions,
            IEnumerable<Budget.Transactions.TransactionEntity> toTransactions)
        {
            var balanceChange = fromTransactions.Sum(x => x.FromFiatAmount.GetValueOrDefault()) +
                                toTransactions.Sum(x => x.ToFiatAmount.GetValueOrDefault());

            accountBalances[accountId] += balanceChange;
        }

        private decimal CalculateFiatTotalAtDate(DateOnly date, Dictionary<ObjectId, decimal> accountBalances)
        {
            var fiatTotal = 0m;

            foreach (var account in _provider.Accounts.Values)
            {
                var accountId = account.Id;
                if (!accountBalances.TryGetValue(accountId, out var balance))
                    continue;

                if (account.AccountEntityType == AccountEntityType.Bitcoin)
                {
                    var usdBitcoinPrice = _provider.GetUsdBitcoinPriceAt(date);
                    fiatTotal += _provider.GetFiatRateAt(date, _currency) * (balance * usdBitcoinPrice);
                }
                else
                {
                    var accountCurrency = FiatCurrency.GetFromCode(account.Currency!);
                    if (account.Currency == _currency.Code)
                    {
                        fiatTotal += balance;
                    }
                    else
                    {
                        var accountRateToUsd = _provider.GetFiatRateAt(date, accountCurrency);
                        var convertedBalance = balance / accountRateToUsd;
                        fiatTotal += _provider.GetFiatRateAt(date, _currency) * convertedBalance;
                    }
                }
            }

            return fiatTotal;
        }
    }
}
