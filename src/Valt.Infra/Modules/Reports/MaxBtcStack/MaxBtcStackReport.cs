using LiteDB;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Infra.Modules.Reports.MaxBtcStack;

internal class MaxBtcStackReport : IMaxBtcStackReport
{
    private readonly IClock _clock;

    public MaxBtcStackReport(IClock clock)
    {
        _clock = clock;
    }

    public Task<MaxBtcStackData> GetAsync(long currentStackInSats, IReportDataProvider provider)
    {
        if (provider.AllTransactions.Count == 0)
            throw new ApplicationException("No transactions found");

        // Discard the current day because rates are not closed yet
        var maxDate = _clock.GetCurrentLocalDate().AddDays(-1);

        var startDate = provider.MinTransactionDate;

        var maxStackSats = 0L;
        var maxStackDate = DateOnly.MinValue;

        var accountCurrentScanDateTotals = new Dictionary<ObjectId, long>();

        // Pre-compute transaction groups per date for O(1) lookups
        var fromTransactionsByDateAndAccount = new Dictionary<(DateOnly, ObjectId), List<TransactionEntity>>();
        var toTransactionsByDateAndAccount = new Dictionary<(DateOnly, ObjectId), List<TransactionEntity>>();

        foreach (var (date, transactions) in provider.TransactionsByDate)
        {
            foreach (var tx in transactions)
            {
                var key = (date, tx.FromAccountId);
                if (!fromTransactionsByDateAndAccount.TryGetValue(key, out var fromList))
                {
                    fromList = new List<TransactionEntity>();
                    fromTransactionsByDateAndAccount[key] = fromList;
                }
                fromList.Add(tx);

                if (tx.ToAccountId is not null)
                {
                    var toKey = (date, tx.ToAccountId);
                    if (!toTransactionsByDateAndAccount.TryGetValue(toKey, out var toList))
                    {
                        toList = new List<TransactionEntity>();
                        toTransactionsByDateAndAccount[toKey] = toList;
                    }
                    toList.Add(tx);
                }
            }
        }

        var currentScanDate = startDate.AddDays(-1);
        while (currentScanDate < maxDate)
        {
            currentScanDate = currentScanDate.AddDays(1);

            if (provider.AccountsByDate.TryGetValue(currentScanDate, out var accountsOfDate))
            {
                foreach (var accountId in accountsOfDate)
                {
                    var account = provider.Accounts[accountId];

                    // Only track Bitcoin accounts
                    if (account.AccountEntityType != AccountEntityType.Bitcoin)
                        continue;

                    if (!accountCurrentScanDateTotals.ContainsKey(accountId))
                    {
                        accountCurrentScanDateTotals[accountId] = (long)account.InitialAmount;
                    }

                    fromTransactionsByDateAndAccount.TryGetValue((currentScanDate, accountId), out var fromAccount);
                    toTransactionsByDateAndAccount.TryGetValue((currentScanDate, accountId), out var toAccount);

                    var change = (fromAccount?.Sum(x => x.FromSatAmount.GetValueOrDefault()) ?? 0L);
                    change += (toAccount?.Sum(x => x.ToSatAmount.GetValueOrDefault()) ?? 0L);

                    accountCurrentScanDateTotals[accountId] += change;
                }
            }

            // Sum all BTC account totals for this day
            var dateTotal = 0L;
            foreach (var kvp in accountCurrentScanDateTotals)
            {
                dateTotal += kvp.Value;
            }

            if (dateTotal > maxStackSats)
            {
                maxStackSats = dateTotal;
                maxStackDate = currentScanDate;
            }
        }

        var hasAccountsWithoutTransactions = provider.Accounts.Values
            .Where(a => a.AccountEntityType == AccountEntityType.Bitcoin)
            .Any(account => !accountCurrentScanDateTotals.ContainsKey(account.Id));

        return Task.FromResult(new MaxBtcStackData(maxStackDate, maxStackSats, currentStackInSats)
        {
            HasAccountsWithoutTransactions = hasAccountsWithoutTransactions
        });
    }
}
