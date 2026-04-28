using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetAssetSummary;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.Modules.Assets.Services;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Infra.Settings;
using Valt.UI.State.Events;

namespace Valt.UI.State;

public partial class AccountsTotalState : ObservableObject, IRecipient<RatesUpdated>,
    IRecipient<AccountSummariesDTO>, IRecipient<AssetSummaryUpdatedMessage>, IRecipient<AssetPricesUpdated>, IDisposable
{
    private readonly Lock _calculationLock = new();
    private CancellationTokenSource? _refreshDebounceCts;

    private readonly CurrencySettings _currencySettings;
    private readonly RatesState _ratesState;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ILogger<AccountsTotalState> _logger;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentWealth))]
    private AccountSummariesDTO? _accountSummaries;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentWealth))]
    private AssetSummaryDTO? _assetSummary;

    public Wealth CurrentWealth => CalculateCurrentWealth();

    public AccountsTotalState(CurrencySettings currencySettings, RatesState ratesState, IQueryDispatcher queryDispatcher, ILogger<AccountsTotalState> logger)
    {
        _currencySettings = currencySettings;
        _ratesState = ratesState;
        _queryDispatcher = queryDispatcher;
        _logger = logger;
        WeakReferenceMessenger.Default.Register<RatesUpdated>(this);
        WeakReferenceMessenger.Default.Register<AccountSummariesDTO>(this);
        WeakReferenceMessenger.Default.Register<AssetSummaryUpdatedMessage>(this);
        WeakReferenceMessenger.Default.Register<AssetPricesUpdated>(this);
    }

    public void Receive(AccountSummariesDTO message)
    {
        AccountSummaries = message;
    }

    public void Receive(RatesUpdated message)
    {
        // Debounce rapid successive rates updates to avoid redundant DB queries.
        // If multiple messages arrive within 100ms, only the last one triggers a refresh.
        var previousCts = Interlocked.Exchange(ref _refreshDebounceCts, new CancellationTokenSource());
        previousCts?.Cancel();
        previousCts?.Dispose();

        var currentCts = _refreshDebounceCts;
        _ = DebouncedRefreshAndNotifyAsync(currentCts.Token);
    }

    private async Task DebouncedRefreshAndNotifyAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(100, ct);
            await RefreshAndNotifyAsync();
        }
        catch (OperationCanceledException)
        {
            // Debounce was superseded by a newer message — safe to ignore.
        }
    }

    public void Receive(AssetSummaryUpdatedMessage message)
    {
        _ = RefreshAndNotifyAsync();
    }

    public void Receive(AssetPricesUpdated message)
    {
        _ = RefreshAndNotifyAsync();
    }

    private async Task RefreshAndNotifyAsync()
    {
        try
        {
            await RefreshAssetSummaryAsync();
            OnPropertyChanged(nameof(CurrentWealth));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AccountsTotalState] Error refreshing and notifying");
        }
    }

    /// <summary>
    /// Refreshes the asset summary from the database.
    /// Call this when assets have been modified.
    /// </summary>
    public async Task RefreshAssetSummaryAsync()
    {
        try
        {
            var btcPriceUsd = _ratesState.BitcoinPrice;
            var fiatRates = _ratesState.FiatRates;

            AssetSummary = await _queryDispatcher.DispatchAsync(new GetAssetSummaryQuery
            {
                MainCurrencyCode = _currencySettings.MainFiatCurrency,
                BtcPriceUsd = btcPriceUsd,
                FiatRates = fiatRates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AccountsTotalState] Error refreshing asset summary");
        }
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
                        if (!_ratesState.FiatRates.TryGetValue(fiatAccount.Currency, out var accountCurrencyRate))
                        {
                            _logger.LogWarning(
                                "[AccountsTotalState] Fiat rate not found for {Currency} ({AccountName}), skipping account in wealth calculation",
                                fiatAccount.Currency, fiatAccount.Name);
                            continue;
                        }

                        allWealthPricedInSats += BtcPriceCalculator
                            .CalculateBtcAmountOfFiat(fiatAccount.FiatTotal.GetValueOrDefault(),
                                accountCurrencyRate, _ratesState.BitcoinPrice.Value);

                        if (fiatAccount.Currency == _currencySettings.MainFiatCurrency)
                        {
                            wealthInMainFiatCurrency += fiatAccount.FiatTotal.GetValueOrDefault();
                        }
                        else
                        {
                            //convert it to dollar, then convert back to main fiat currency
                            var fiatConvertedToUsd =
                                fiatAccount.FiatTotal.GetValueOrDefault() / accountCurrencyRate;
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

                // Include assets in net worth calculation
                var assetsWealthInMainFiatCurrency = AssetSummary?.TotalValueInMainCurrency ?? 0m;
                var assetsWealthInSats = AssetSummary?.TotalValueInSats ?? 0L;

                // Net worth = accounts + assets
                var netWorthInMainFiatCurrency = currentWealthInFiat + assetsWealthInMainFiatCurrency;
                var netWorthInSats = allWealthPricedInSats + assetsWealthInSats;

                // USD equivalent (0 when main currency is already USD)
                var netWorthInUsd = 0m;
                if (_currencySettings.MainFiatCurrency != FiatCurrency.Usd.Code
                    && _ratesState.FiatRates.TryGetValue(_currencySettings.MainFiatCurrency, out var mainRate)
                    && mainRate != 0)
                {
                    netWorthInUsd = Math.Round(netWorthInMainFiatCurrency / mainRate, 2);
                }

                return new Wealth(
                    currentWealthInFiat,
                    wealthInMainFiatCurrency,
                    wealthInSats,
                    allWealthPricedInSats,
                    wealthInBtcRatio,
                    assetsWealthInMainFiatCurrency,
                    assetsWealthInSats,
                    netWorthInMainFiatCurrency,
                    netWorthInSats,
                    netWorthInUsd);
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
        decimal WealthInBtcRatio,
        decimal AssetsWealthInMainFiatCurrency,
        long AssetsWealthInSats,
        decimal NetWorthInMainFiatCurrency,
        long NetWorthInSats,
        decimal NetWorthInUsd)
    {
        public static Wealth Empty => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    public void Reset()
    {
        AccountSummaries = null;
        AssetSummary = null;
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<RatesUpdated>(this);
        WeakReferenceMessenger.Default.Unregister<AccountSummariesDTO>(this);
        WeakReferenceMessenger.Default.Unregister<AssetSummaryUpdatedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<AssetPricesUpdated>(this);

        _refreshDebounceCts?.Cancel();
        _refreshDebounceCts?.Dispose();
    }
}
