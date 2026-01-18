using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;
using Valt.Infra.Settings;
using Valt.UI.State.Events;

namespace Valt.UI.State;

public partial class AccountsTotalState : ObservableObject, IRecipient<RatesUpdated>,
    IRecipient<AccountSummariesDTO>, IDisposable
{
    private readonly Lock _calculationLock = new();
    
    private readonly CurrencySettings _currencySettings;
    private readonly RatesState _ratesState;
    private readonly ILogger<AccountsTotalState> _logger;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentWealth))]
    private AccountSummariesDTO? _accountSummaries;

    public Wealth CurrentWealth => CalculateCurrentWealth();

    public AccountsTotalState(CurrencySettings currencySettings, RatesState ratesState, ILogger<AccountsTotalState> logger)
    {
        _currencySettings = currencySettings;
        _ratesState = ratesState;
        _logger = logger;
        WeakReferenceMessenger.Default.Register<RatesUpdated>(this);
        WeakReferenceMessenger.Default.Register<AccountSummariesDTO>(this);
    }

    public void Receive(AccountSummariesDTO message)
    {
        AccountSummaries = message;
    }
    
    public void Receive(RatesUpdated message)
    {
        OnPropertyChanged(nameof(CurrentWealth));
    }

    // Note: This calculation should have unit tests covering currency conversions and edge cases
    private Wealth CalculateCurrentWealth()
    {
        lock (_calculationLock)
        {
            if (_ratesState.BitcoinPrice is null || _ratesState.FiatRates is null || AccountSummaries is null)
            {
                _logger.LogWarning("[AccountsTotalState] Bitcoin price, fiat rates or account summaries not set");
                return Wealth.Empty;
            }

            try
            {
                var wealthInSats = AccountSummaries.Items.Where(x => x.IsBtcAccount).Sum(x => x.SatsTotal!.Value);
                var wealthInMainFiatCurrency = 0m;

                var allWealthPricedInSats = wealthInSats;

                var fiatAccounts = AccountSummaries.Items.Where(x => !x.IsBtcAccount).ToList();
                foreach (var fiatAccount in fiatAccounts)
                {
                    if (fiatAccount.Currency is null)
                        continue;

                    if (fiatAccount.Currency == FiatCurrency.Usd.Code)
                    {
                        wealthInMainFiatCurrency +=
                            _ratesState.FiatRates[_currencySettings.MainFiatCurrency] * fiatAccount.FiatTotal.GetValueOrDefault();
                        allWealthPricedInSats += BtcPriceCalculator
                            .CalculateBtcAmountOfFiat(fiatAccount.FiatTotal.GetValueOrDefault(), 1, _ratesState.BitcoinPrice.Value);
                    }
                    else
                    {
                        if (!_ratesState.FiatRates.ContainsKey(fiatAccount.Currency))
                            throw new ApplicationException(
                                $"Error calculating total in sats. Fiat rate not found for {fiatAccount.Currency}.");

                        allWealthPricedInSats += BtcPriceCalculator
                            .CalculateBtcAmountOfFiat(fiatAccount.FiatTotal.GetValueOrDefault(),
                                _ratesState.FiatRates[fiatAccount.Currency], _ratesState.BitcoinPrice.Value);

                        if (fiatAccount.Currency == _currencySettings.MainFiatCurrency)
                        {
                            wealthInMainFiatCurrency += fiatAccount.FiatTotal.GetValueOrDefault();
                        }
                        else
                        {
                            //convert it to dollar, then convert back to main fiat currency
                            var fiatConvertedToUsd =
                                fiatAccount.FiatTotal.GetValueOrDefault() / _ratesState.FiatRates[fiatAccount.Currency];
                            wealthInMainFiatCurrency +=
                                _ratesState.FiatRates[_currencySettings.MainFiatCurrency] * fiatConvertedToUsd;
                        }
                    }
                }

                var allWealthPricedInBtc = BtcValue.ParseSats(allWealthPricedInSats);

                var currentWealthInFiat =
                    Math.Round(
                        allWealthPricedInBtc.Btc * _ratesState.BitcoinPrice.Value * _ratesState.FiatRates[_currencySettings.MainFiatCurrency],
                        2);

                var wealthInBtcRatio = 0m;
                if (allWealthPricedInSats > 0)
                    wealthInBtcRatio = Math.Round((wealthInSats / (decimal)allWealthPricedInSats) * 100m, 2);

                return new Wealth(currentWealthInFiat, wealthInMainFiatCurrency, wealthInSats, allWealthPricedInSats,
                    wealthInBtcRatio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AccountsTotalState] Error calculating current wealth");
                return Wealth.Empty;
            }
        }
    }

    public record Wealth(
        decimal AllWealthInMainFiatCurrency,
        decimal WealthInMainFiatCurrency,
        long WealthInSats,
        long AllWealthInSats,
        decimal WealthInBtcRatio)
    {
        public static Wealth Empty => new(0, 0, 0, 0, 0);
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<RatesUpdated>(this);
        WeakReferenceMessenger.Default.Unregister<AccountSummariesDTO>(this);
    }
}