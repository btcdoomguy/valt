using System.Collections.Generic;
using System.Linq;
using Valt.Infra.Modules.Budget.Transactions.Messages;
using Valt.Infra.Settings;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Services;

/// <summary>
/// Refreshes AutoSat values on transaction view models when rates change.
/// </summary>
public interface IAutoSatRefreshService
{
    void RefreshAll(IEnumerable<TransactionViewModel> transactions);
    void RefreshBatch(IEnumerable<TransactionViewModel> transactions, IReadOnlyList<AutoSatAmountRefreshedTransaction> messages);
}

public class AutoSatRefreshService : IAutoSatRefreshService
{
    private readonly LiveRateState _liveRateState;
    private readonly CurrencySettings _currencySettings;

    public AutoSatRefreshService(LiveRateState liveRateState, CurrencySettings currencySettings)
    {
        _liveRateState = liveRateState;
        _currencySettings = currencySettings;
    }

    public void RefreshAll(IEnumerable<TransactionViewModel> transactions)
    {
        foreach (var transaction in transactions)
        {
            transaction.RefreshCurrentAutoSatValue(
                _liveRateState.UsdPrice,
                _liveRateState.BitcoinPrice,
                _currencySettings.MainFiatCurrency,
                _liveRateState.FiatRates);
        }
    }

    public void RefreshBatch(IEnumerable<TransactionViewModel> transactions, IReadOnlyList<AutoSatAmountRefreshedTransaction> messages)
    {
        var transactionList = transactions.ToList();

        foreach (var message in messages)
        {
            var transaction = transactionList.SingleOrDefault(x => x.Id == message.TransactionId.Value);
            if (transaction is null) continue;

            transaction.SetupAutoSatAmount(message.AutoSatAmount);
            transaction.RefreshCurrentAutoSatValue(
                _liveRateState.UsdPrice,
                _liveRateState.BitcoinPrice,
                _currencySettings.MainFiatCurrency,
                _liveRateState.FiatRates);
        }
    }
}
