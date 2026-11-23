using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Infra.Modules.Reports;

internal static class CategoryBreakdownCalculator
{
    public static IReadOnlyList<TItem> Calculate<TItem>(
        FiatCurrency currency,
        IReportDataProvider provider,
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<AccountId> filterAccountIds,
        IEnumerable<CategoryId> filterCategoryIds,
        Func<decimal, bool> valuePredicate,
        decimal valueMultiplier,
        Func<CategoryId, Icon, string, decimal, TItem> itemFactory,
        Func<TItem, string> sortKeySelector)
    {
        if (provider.AllTransactions.Count == 0)
            return [];

        var categoryFiatTotals = new Dictionary<ObjectId, decimal>();
        var accountIdSet = filterAccountIds.Select(id => new ObjectId(id.Value)).ToHashSet();
        var categoryIdSet = filterCategoryIds.Select(id => new ObjectId(id.Value)).ToHashSet();

        var currentDate = startDate.AddDays(-1);
        while (currentDate < endDate)
        {
            currentDate = currentDate.AddDays(1);

            if (!provider.AccountsByDate.TryGetValue(currentDate, out var accountsForDate))
                continue;

            if (!provider.TransactionsByDate.TryGetValue(currentDate, out var transactionsForDate))
                continue;

            foreach (var accountId in accountsForDate)
            {
                if (accountIdSet.Count > 0 && !accountIdSet.Contains(accountId))
                    continue;

                var account = provider.Accounts[accountId];

                var transactions = transactionsForDate.Where(x =>
                    x.FromAccountId == accountId &&
                    (x.Type == TransactionEntityType.Fiat || x.Type == TransactionEntityType.Bitcoin));

                foreach (var transaction in transactions)
                {
                    if (transaction.IsExcluded)
                        continue;

                    if (categoryIdSet.Count > 0 && !categoryIdSet.Contains(transaction.CategoryId))
                        continue;

                    if (account.AccountEntityType == AccountEntityType.Bitcoin)
                    {
                        if (!categoryFiatTotals.ContainsKey(transaction.CategoryId))
                            categoryFiatTotals[transaction.CategoryId] = 0;

                        var satsAmount = (decimal)transaction.FromSatAmount.GetValueOrDefault();
                        categoryFiatTotals[transaction.CategoryId] += HistoricalRateConverter.ConvertToFiat(
                            satsAmount,
                            "SATS",
                            currency.Code,
                            currentDate,
                            provider);
                    }
                    else
                    {
                        if (!categoryFiatTotals.ContainsKey(transaction.CategoryId))
                            categoryFiatTotals[transaction.CategoryId] = 0;

                        if (account.Currency == currency.Code)
                        {
                            categoryFiatTotals[transaction.CategoryId] += transaction.FromFiatAmount.GetValueOrDefault();
                        }
                        else
                        {
                            var accountCurrency = FiatCurrency.GetFromCode(account.Currency!);
                            var sourceRateToUsd = provider.GetFiatRateAt(currentDate, accountCurrency);
                            var targetRateFromUsd = provider.GetFiatRateAt(currentDate, currency);

                            if (sourceRateToUsd == 0 || targetRateFromUsd == 0)
                                continue;

                            var convertedBalance = HistoricalRateConverter.ConvertToFiat(
                                transaction.FromFiatAmount.GetValueOrDefault(),
                                account.Currency,
                                currency.Code,
                                currentDate,
                                provider);
                            categoryFiatTotals[transaction.CategoryId] += convertedBalance;
                        }
                    }
                }
            }
        }

        return categoryFiatTotals
            .Where(x => valuePredicate(x.Value))
            .Select(x => itemFactory(
                new CategoryId(x.Key.ToString()),
                Icon.RestoreFromId(provider.Categories[x.Key].Icon!),
                BuildCategoryName(provider, x.Key),
                x.Value * valueMultiplier))
            .OrderBy(sortKeySelector)
            .ToList();
    }

    private static string BuildCategoryName(IReportDataProvider provider, ObjectId id)
    {
        var category = provider.Categories[id];
        return category.ParentId is not null
            ? $"{provider.Categories[category.ParentId].Name} >> {category.Name}"
            : $"{category.Name}";
    }
}
