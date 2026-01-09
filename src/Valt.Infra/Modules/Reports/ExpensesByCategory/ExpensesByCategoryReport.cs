using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Infra.Modules.Reports.ExpensesByCategory;

internal class ExpensesByCategoryReport : IExpensesByCategoryReport
{
    private readonly ILogger<ExpensesByCategoryReport> _logger;

    public ExpensesByCategoryReport(ILogger<ExpensesByCategoryReport> logger)
    {
        _logger = logger;
    }

    public Task<ExpensesByCategoryData> GetAsync(DateOnly baseDate, DateOnlyRange displayRange, FiatCurrency currency, IExpensesByCategoryReport.Filter filter, IReportDataProvider provider)
    {
        if (provider.AllTransactions.Count == 0)
        {
            // Return empty result instead of throwing
            return Task.FromResult(new ExpensesByCategoryData
            {
                MainCurrency = currency,
                Items = new List<ExpensesByCategoryData.Item>()
            });
        }

        var calculator = new Calculator(currency, provider, displayRange.Start, displayRange.End, filter);

        try
        {
            return Task.FromResult(calculator.Calculate());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calculating expenses by category");
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
        private readonly IExpensesByCategoryReport.Filter _filter;

        public Calculator(
            FiatCurrency currency,
            IReportDataProvider provider,
            DateOnly startDate,
            DateOnly endDate,
            IExpensesByCategoryReport.Filter filter)
        {
            _currency = currency;
            _provider = provider;
            _startDate = startDate;
            _endDate = endDate;
            _filter = filter;
        }

        public ExpensesByCategoryData Calculate()
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
                    if (!_filter.AccountIds.Contains(new AccountId(accountId.ToString())))
                        continue;

                    var account = _provider.Accounts[accountId];

                    var transactions = transactionsForDate.Where(x => x.FromAccountId == accountId && (x.Type == TransactionEntityType.Fiat || x.Type == TransactionEntityType.Bitcoin));

                    foreach (var transaction in transactions)
                    {
                        if (!_filter.CategoryIds.Contains(new CategoryId(transaction.CategoryId.ToString())))
                            continue;

                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            //only spending
                            if (transaction.FromSatAmount > 0)
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
                            //only spending
                            if (transaction.FromFiatAmount > 0)
                                continue;

                            if (!categoryFiatTotals.ContainsKey(transaction.CategoryId))
                                categoryFiatTotals[transaction.CategoryId] = 0;

                            var accountCurrency = FiatCurrency.GetFromCode(account.Currency!);
                            var accountRateToUsd = _provider.GetFiatRateAt(currentDate, accountCurrency);

                            if (account.Currency == _currency.Code)
                            {
                                categoryFiatTotals[transaction.CategoryId] += transaction.FromFiatAmount.GetValueOrDefault();
                            }
                            else
                            {
                                var convertedBalance = transaction.FromFiatAmount.GetValueOrDefault() / accountRateToUsd;
                                categoryFiatTotals[transaction.CategoryId] += convertedBalance;
                            }
                        }
                    }
                }
            }

            return new ExpensesByCategoryData()
            {
                MainCurrency = _currency,
                Items = categoryFiatTotals.Select(x => new ExpensesByCategoryData.Item()
                {
                    CategoryId = new CategoryId(x.Key.ToString()),
                    Icon = Icon.RestoreFromId(_provider.Categories[x.Key].Icon!),
                    CategoryName = BuildCategoryName(x.Key),
                    FiatTotal = x.Value * -1
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
