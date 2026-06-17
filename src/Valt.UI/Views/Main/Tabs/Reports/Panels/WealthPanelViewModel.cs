using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.Settings;
using Valt.UI.Lang;
using Valt.UI.State;
using static Valt.UI.State.AccountsTotalState;
using Valt.UI.UserControls;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// Panel displaying current wealth overview (total, stack, fiat, assets, ratio).
/// </summary>
public partial class WealthPanelViewModel : DashboardPanelViewModel
{
    private readonly AccountsTotalState _accountsTotalState;
    private readonly CustomBtcPriceState _customBtcPriceState;
    private readonly RatesState _ratesState;
    private readonly CurrencySettings _currencySettings;

    public WealthPanelViewModel(
        AccountsTotalState accountsTotalState,
        CustomBtcPriceState customBtcPriceState,
        RatesState ratesState,
        CurrencySettings currencySettings,
        ILogger<WealthPanelViewModel> logger)
        : base(logger)
    {
        _accountsTotalState = accountsTotalState;
        _customBtcPriceState = customBtcPriceState;
        _ratesState = ratesState;
        _currencySettings = currencySettings;
    }

    public override Task RefreshAsync()
    {
        Dispatcher.UIThread.Post(Refresh);
        return Task.CompletedTask;
    }

    public override void Refresh()
    {
        try
        {
            var wealth = CalculateWealthWithCustomPrice();
            var fiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);

            var rows = new ObservableCollection<RowItem>
            {
                new(language.Transactions_Total, FormatBtc(wealth.NetWorthInSats), TooltipContent.Text(language.Reports_Wealth_TotalInBtc_Tooltip)),
                new(language.Transactions_TotalInFiat, FormatFiat(wealth.NetWorthInMainFiatCurrency, fiatCurrency.Code)),
                new(language.Transactions_MyStack, FormatBtc(wealth.WealthInSats)),
                new(language.Transactions_MyOther, FormatFiat(wealth.WealthInMainFiatCurrency, fiatCurrency.Code))
            };

            // Add USD total row when main currency is not USD
            if (fiatCurrency.Code != FiatCurrency.Usd.Code)
            {
                var totalInUsd = FormatFiat(wealth.NetWorthInUsd, FiatCurrency.Usd.Code);
                rows.Insert(2, new RowItem(language.Reports_Wealth_TotalInUsd, totalInUsd));
            }

            // Separate totals from breakdown
            rows.Insert(fiatCurrency.Code != FiatCurrency.Usd.Code ? 3 : 2, RowItem.Separator());

            // Add assets line if there are any assets (positive or negative)
            if (wealth.AssetsWealthInMainFiatCurrency != 0 || wealth.AssetsWealthInSats != 0)
            {
                var assetsWealth = FormatFiat(wealth.AssetsWealthInMainFiatCurrency, fiatCurrency.Code);
                rows.Add(new RowItem(language.Reports_Wealth_MyAssets, assetsWealth));
            }

            var btcRatio = wealth.WealthInBtcRatio.ToString(CultureInfo.InvariantCulture) + "%";
            rows.Add(new RowItem(language.Transactions_Ratio, btcRatio));

            SetData(language.Reports_Wealth_Title, rows, "\uE84F");
        }
        catch (Exception ex)
        {
            SetError(language.Reports_Wealth_Title, ex, "\uE84F");
        }
    }

    private Wealth CalculateWealthWithCustomPrice()
    {
        var liveWealth = _accountsTotalState.CurrentWealth;
        if (!_customBtcPriceState.IsActive)
            return liveWealth;

        var customBtcPriceInMainFiat = GetCustomBtcPriceInMainFiat();
        var liveBtcPriceInMainFiat = GetCurrentBtcPriceInMainFiat();
        
        if (liveBtcPriceInMainFiat == 0)
            return liveWealth;

        var btcPortionInMainFiat = liveWealth.WealthInSats / 100_000_000m * customBtcPriceInMainFiat;
        var nonBtcWealth = liveWealth.WealthInMainFiatCurrency;
        var currentWealthInFiat = Math.Round(btcPortionInMainFiat + nonBtcWealth, 2);
        var netWorthInMainFiatCurrency = currentWealthInFiat + liveWealth.AssetsWealthInMainFiatCurrency;
        var allWealthPricedInSats = liveWealth.WealthInSats + (long)Math.Round(nonBtcWealth / customBtcPriceInMainFiat * 100_000_000m);
        var netWorthInSats = allWealthPricedInSats + liveWealth.AssetsWealthInSats;
        
        var wealthInBtcRatio = 0m;
        if (allWealthPricedInSats > 0)
            wealthInBtcRatio = Math.Round((liveWealth.WealthInSats / (decimal)allWealthPricedInSats) * 100m, 2);
        
        var netWorthInUsd = 0m;
        if (_currencySettings.MainFiatCurrency != FiatCurrency.Usd.Code
            && _ratesState.FiatRates?.TryGetValue(_currencySettings.MainFiatCurrency, out var mainRate) == true
            && mainRate != 0)
        {
            netWorthInUsd = Math.Round(netWorthInMainFiatCurrency / mainRate, 2);
        }

        return new Wealth(
            currentWealthInFiat,
            nonBtcWealth,
            liveWealth.WealthInSats,
            allWealthPricedInSats,
            wealthInBtcRatio,
            liveWealth.AssetsWealthInMainFiatCurrency,
            liveWealth.AssetsWealthInSats,
            netWorthInMainFiatCurrency,
            netWorthInSats,
            netWorthInUsd);
    }

    private decimal GetCurrentBtcPriceInMainFiat()
    {
        if (_ratesState.BitcoinPrice.HasValue && _ratesState.FiatRates is not null)
        {
            var fiatRate = _ratesState.FiatRates.GetValueOrDefault(_currencySettings.MainFiatCurrency, 1m);
            return _ratesState.BitcoinPrice.Value * fiatRate;
        }
        return 0m;
    }

    private decimal GetCustomBtcPriceInMainFiat()
    {
        if (_customBtcPriceState.CustomBtcPriceUsd.HasValue && _ratesState.FiatRates is not null)
        {
            var fiatRate = _ratesState.FiatRates.GetValueOrDefault(_currencySettings.MainFiatCurrency, 1m);
            return _customBtcPriceState.CustomBtcPriceUsd.Value * fiatRate;
        }
        return 0m;
    }
}
