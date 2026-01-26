using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Infra.Modules.Reports.IncomeByCategory;

internal class IncomeByCategoryReport : IIncomeByCategoryReport
{
    private readonly ILogger<IncomeByCategoryReport> _logger;

    public IncomeByCategoryReport(ILogger<IncomeByCategoryReport> logger)
    {
        _logger = logger;
    }

    public Task<IncomeByCategoryData> GetAsync(DateOnly baseDate, DateOnlyRange displayRange, FiatCurrency currency, IIncomeByCategoryReport.Filter filter, IReportDataProvider provider)
    {
        if (provider.AllTransactions.Count == 0)
        {
            // Return empty result instead of throwing
            return Task.FromResult(new IncomeByCategoryData
            {
                MainCurrency = currency,
                Items = new List<IncomeByCategoryData.Item>()
            });
        }

        var calculator = new Calculator(currency, provider, displayRange.Start, displayRange.End, filter);

        try
        {
            return Task.FromResult(calculator.Calculate());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calculating income by category");
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
        private readonly IIncomeByCategoryReport.Filter _filter;

        public Calculator(
            FiatCurrency currency,
            IReportDataProvider provider,
            DateOnly startDate,
            DateOnly endDate,
            IIncomeByCategoryReport.Filter filter)
        {
            _currency = currency;
            _provider = provider;
            _startDate = startDate;
            _endDate = endDate;
            _filter = filter;
        }

        public IncomeByCategoryData Calculate()
        {
            var categoryFiatTotals = new Dictionary<ObjectId, decimal>();

            var currentDate = _startDate.AddDays(-1);
            while (currentDate < _endDate)
            {
                currentDate = currentDate.AddDays(1);

                if (!_provider.AccountsByDate.TryGetValue(currentDate, out var accountsForDate))
                {
                    continue;
                }

                if (!_provider.TransactionsByDate.TryGetValue(currentDate, out var transactionsForDate))
                {
                    continue;
                }

                foreach (var accountId in accountsForDate)
                {
                    // If filter has account IDs, only include matching accounts; otherwise include all
                    if (_filter.AccountIds.Any() && !_filter.AccountIds.Contains(new AccountId(accountId.ToString())))
                        continue;

                    var account = _provider.Accounts[accountId];

                    var transactions = transactionsForDate.Where(x => x.FromAccountId == accountId && (x.Type == TransactionEntityType.Fiat || x.Type == TransactionEntityType.Bitcoin));

                    foreach (var transaction in transactions)
                    {
                        // If filter has category IDs, only include matching categories; otherwise include all
                        if (_filter.CategoryIds.Any() && !_filter.CategoryIds.Contains(new CategoryId(transaction.CategoryId.ToString())))
                            continue;

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            //only income (positive amounts)
                            if (transaction.FromSatAmount < 0)
                                continue;

                            if (!categoryFiatTotals.ContainsKey(transaction.CategoryId))
                                categoryFiatTotals[transaction.CategoryId] = 0;

                            var usdBitcoinPrice = _provider.GetUsdBitcoinPriceAt(currentDate);
                            var bitcoin = transaction.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin;

                            categoryFiatTotals[transaction.CategoryId] += _provider.GetFiatRateAt(currentDate, _currency) *
                                                                           (bitcoin * usdBitcoinPrice);
                        }
                        else
                        {
                            //only income (positive amounts)
                            if (transaction.FromFiatAmount < 0)
                                continue;

                            if (!categoryFiatTotals.ContainsKey(transaction.CategoryId))
                                categoryFiatTotals[transaction.CategoryId] = 0;

                            if (account.Currency == _currency.Code)
                            {
                                categoryFiatTotals[transaction.CategoryId] += transaction.FromFiatAmount.GetValueOrDefault();
                            }
                            else
                            {
                                // Convert: source currency -> USD -> target currency
                                var accountCurrency = FiatCurrency.GetFromCode(account.Currency!);
                                var sourceRateToUsd = _provider.GetFiatRateAt(currentDate, accountCurrency);
                                var targetRateFromUsd = _provider.GetFiatRateAt(currentDate, _currency);

                                if (sourceRateToUsd == 0 || targetRateFromUsd == 0)
                                    continue; // Skip if no rate available

                                var convertedBalance = targetRateFromUsd * (transaction.FromFiatAmount.GetValueOrDefault() / sourceRateToUsd);
                                categoryFiatTotals[transaction.CategoryId] += convertedBalance;
                            }
                        }
                    }
                }
            }

            return new IncomeByCategoryData()
            {
                MainCurrency = _currency,
                Items = categoryFiatTotals.Select(x => new IncomeByCategoryData.Item()
                {
                    CategoryId = new CategoryId(x.Key.ToString()),
                    Icon = Icon.RestoreFromId(_provider.Categories[x.Key].Icon!),
                    CategoryName = BuildCategoryName(x.Key),
                    FiatTotal = x.Value
                }).OrderBy(x => x.CategoryName).ToList()
            };
        }

        private string BuildCategoryName(ObjectId id)
        {
            var category = _provider.Categories[id];

            return category.ParentId is not null ? $"{_provider.Categories[category.ParentId].Name} >> {category.Name}" : $"{category.Name}";
        }
    }
}
