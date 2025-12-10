using System.Collections.Frozen;
using System.Collections.Immutable;
using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Reports.MonthlyTotals;

namespace Valt.Infra.Modules.Reports.ExpensesByCategory;

internal class ExpensesByCategoryReport : IExpensesByCategoryReport
{
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalDatabase _localDatabase;
    private readonly IClock _clock;
    private readonly ILogger<ExpensesByCategoryReport> _logger;

    public ExpensesByCategoryReport(IPriceDatabase priceDatabase, ILocalDatabase localDatabase,
        IClock clock,
        ILogger<ExpensesByCategoryReport> logger)
    {
        _priceDatabase = priceDatabase;
        _localDatabase = localDatabase;
        _clock = clock;
        _logger = logger;
    }
    
    public Task<ExpensesByCategoryData> GetAsync(DateOnly baseDate, DateOnlyRange displayRange, FiatCurrency currency)
    {
        var accounts = _localDatabase.GetAccounts().FindAll().ToImmutableList();
        var categories = _localDatabase.GetCategories().FindAll().ToImmutableList();
        
        var minDate = displayRange.Start.ToValtDateTime();
        var maxDate = displayRange.End.ToValtDateTime();
        
        var transactions = _localDatabase.GetTransactions().Find(x => x.Date >= minDate && x.Date <= maxDate).ToImmutableList();
        
        if (transactions.Count == 0)
        {
            throw new ApplicationException("No transactions found");
        }
        
        var btcRates = _priceDatabase.GetBitcoinData().Find(x => x.Date >= minDate.AddDays(-5) && x.Date <= maxDate)
            .ToImmutableList();
        var fiatRates = _priceDatabase.GetFiatData().Find(x => x.Date >= minDate.AddDays(-5) && x.Date <= maxDate)
            .ToImmutableList();
        
        var calculator = new Calculator(currency, accounts, categories, transactions, btcRates, fiatRates, minDate, maxDate);

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
        private const int MaxDaysToScanForRate = 5;

        private readonly FiatCurrency _currency;
        private readonly FrozenDictionary<ObjectId, AccountEntity> _accounts;
        private readonly FrozenDictionary<ObjectId, CategoryEntity> _categories;
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;
        private readonly FrozenDictionary<DateTime, BitcoinDataEntity> _btcRates;
        private readonly FrozenDictionary<DateTime, ImmutableList<FiatDataEntity>> _fiatRates;
        private readonly FrozenDictionary<DateTime, ImmutableList<TransactionEntity>> _transactionsByDate;
        private readonly FrozenDictionary<DateTime, ImmutableList<ObjectId>> _accountsByDate;

        public Calculator(
            FiatCurrency currency,
            ImmutableList<AccountEntity> accounts,
            ImmutableList<CategoryEntity> categories,
            ImmutableList<TransactionEntity> transactions,
            ImmutableList<BitcoinDataEntity> btcRates,
            ImmutableList<FiatDataEntity> fiatRates,
            DateTime startDate,
            DateTime endDate)
        {
            _currency = currency;
            _categories = categories.ToFrozenDictionary(x => x.Id);
            _accounts = accounts.ToFrozenDictionary(x => x.Id);
            _startDate = startDate;
            _endDate = endDate;
            _btcRates = btcRates.ToFrozenDictionary(x => x.Date);
            _fiatRates = fiatRates.GroupBy(x => x.Date).ToFrozenDictionary(x => x.Key, x => x.ToImmutableList());

            _transactionsByDate = transactions.GroupBy(x => x.Date)
                .ToFrozenDictionary(x => x.Key, x => x.ToImmutableList());
            _accountsByDate = _transactionsByDate.ToFrozenDictionary(
                x => x.Key,
                x => x.Value.SelectMany(y => new[] { y.FromAccountId, y.ToAccountId ?? ObjectId.Empty })
                    .Where(y => y != ObjectId.Empty)
                    .Distinct()
                    .ToImmutableList());
        }

        public ExpensesByCategoryData Calculate()
        {
            var categoryFiatTotals = new Dictionary<ObjectId, decimal>();

            var currentDate = _startDate.AddDays(-1);
            while (currentDate < _endDate)
            {
                currentDate = currentDate.AddDays(1);

                if (!_accountsByDate.TryGetValue(currentDate, out var accountsForDate))
                {
                    continue;
                }

                if (!_transactionsByDate.TryGetValue(currentDate, out var transactionsForDate))
                {
                    continue;
                }

                foreach (var accountId in accountsForDate)
                {
                    var account = _accounts[accountId];

                    var transactions = transactionsForDate.Where(x => x.FromAccountId == accountId && (x.Type == TransactionEntityType.Fiat || x.Type == TransactionEntityType.Bitcoin));
                    
                    foreach (var transaction in transactions)
                    {
                        if (account.AccountEntityType == AccountEntityType.Bitcoin)
                        {
                            //only spending
                            if (transaction.FromSatAmount > 0)
                                continue;
                            
                            if (!categoryFiatTotals.ContainsKey(transaction.CategoryId))
                                categoryFiatTotals[transaction.CategoryId] = 0;
                            
                            var usdBitcoinPrice = GetUsdBitcoinPriceAt(currentDate);
                            var bitcoin = transaction.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin;
                            
                            categoryFiatTotals[transaction.CategoryId] += GetFiatRateAt(currentDate, _currency) *
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
                            var accountRateToUsd = GetFiatRateAt(currentDate, accountCurrency);
                            
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
                    CategoryName = _categories[x.Key].Name,
                    FiatTotal = x.Value * -1
                }).ToList()
            };
        }
        
        private decimal GetFiatRateAt(DateTime date, FiatCurrency currency)
        {
            if (currency == FiatCurrency.Usd)
            {
                return 1;
            }

            var scanDate = date.Date;
            var currencyCode = currency.Code;

            for (var i = 0; i < MaxDaysToScanForRate; i++)
            {
                if (_fiatRates.TryGetValue(scanDate, out var rates))
                {
                    var entry = rates.SingleOrDefault(x => x.Currency == currencyCode);
                    if (entry is not null)
                    {
                        return entry.Price;
                    }
                }

                scanDate = scanDate.AddDays(-1);
            }

            throw new ApplicationException($"Could not find fiat rate for {currencyCode} on {date}");
        }

        private decimal GetUsdBitcoinPriceAt(DateTime date)
        {
            var scanDate = date;

            for (var i = 0; i < MaxDaysToScanForRate; i++)
            {
                if (_btcRates.TryGetValue(scanDate, out var btcRate))
                {
                    return btcRate.Price;
                }

                scanDate = scanDate.AddDays(-1);
            }

            throw new ApplicationException($"Could not find BTC rate on {date}");
        }
    }
}