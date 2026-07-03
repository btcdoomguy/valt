using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetVisibleAssets;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Infra.Kernel;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.State;
using Valt.UI.UserControls;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// Panel displaying leverage-position summary data on the reports dashboard.
/// </summary>
public partial class LeveragePositionsPanelViewModel : DashboardPanelViewModel, ILeveragePositionsPanelViewModel
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AccountsTotalState _accountsTotalState;
    private readonly RatesState _ratesState;
    private readonly CustomBtcPriceState _customBtcPriceState;
    private readonly CurrencySettings _currencySettings;
    private readonly ILogger<LeveragePositionsPanelViewModel> _logger;

    public decimal? AllTimeHighFiatValue { get; set; }

    public LeveragePositionsPanelViewModel(
        IQueryDispatcher queryDispatcher,
        AccountsTotalState accountsTotalState,
        RatesState ratesState,
        CustomBtcPriceState customBtcPriceState,
        CurrencySettings currencySettings,
        ILogger<LeveragePositionsPanelViewModel> logger)
        : base(logger)
    {
        _queryDispatcher = queryDispatcher;
        _accountsTotalState = accountsTotalState;
        _ratesState = ratesState;
        _customBtcPriceState = customBtcPriceState;
        _currencySettings = currencySettings;
        _logger = logger;
    }

    public override async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;

            var assets = await _queryDispatcher.DispatchAsync(new GetVisibleAssetsQuery());

            // Filter to leveraged positions that are visible and included in net worth
            var leveragedPositions = assets
                .Where(a => a.IsLong.HasValue && a.IncludeInNetWorth && a.Visible)
                .ToList();

            if (leveragedPositions.Count == 0)
            {
                IsVisible = false;
                IsLoading = false;
                return;
            }

            var wealth = _accountsTotalState.CurrentWealth;
            var btcSpotSats = wealth.WealthInSats;

            // Calculate BTC exposure from leveraged positions
            decimal totalBtcExposure = 0;
            foreach (var position in leveragedPositions)
            {
                if (!position.EntryPrice.HasValue || position.EntryPrice.Value == 0)
                    continue;

                decimal btcExposure;
                if (position.CollateralAssetTypeId == (int)LeveragedPositionCollateralAssetType.Btc)
                {
                    if (!position.ContractCount.HasValue || !position.ContractSizeUsd.HasValue)
                        continue;

                    btcExposure = position.ContractCount.Value * position.ContractSizeUsd.Value / position.EntryPrice.Value;
                }
                else
                {
                    if (!position.Collateral.HasValue || !position.Leverage.HasValue)
                        continue;

                    // Notional value = Collateral * Leverage
                    var notionalValue = position.Collateral.Value * position.Leverage.Value;
                    // BTC exposure = Notional / Entry Price
                    btcExposure = notionalValue / position.EntryPrice.Value;
                }

                // Apply direction: Long = positive, Short = negative
                if (!position.IsLong!.Value)
                    btcExposure = -btcExposure;

                totalBtcExposure += btcExposure;
            }

            // Convert BTC exposure to sats
            var exposureSats = (long)(totalBtcExposure * 100_000_000m);

            // Leveraged stack = BTC spot + exposure from leverage
            var leveragedStackSats = btcSpotSats + exposureSats;

            // Calculate leverage percentage: |exposure| / |leveraged stack| * 100
            decimal leveragePercentage = 0;
            if (leveragedStackSats != 0)
            {
                leveragePercentage = Math.Abs(totalBtcExposure) / Math.Abs(leveragedStackSats / 100_000_000m) * 100m;
            }

            // Calculate total P&L in main fiat currency
            var fiatRates = _ratesState.FiatRates;
            var btcPrice = _ratesState.BitcoinPrice;
            var mainCurrency = _currencySettings.MainFiatCurrency;
            decimal totalPnlInMainFiat = 0;

            if (fiatRates != null && btcPrice.HasValue)
            {
                foreach (var position in leveragedPositions)
                {
                    var pnl = CalculateSimulatedLeveragedPnl(position, fiatRates);
                    if (!pnl.HasValue) continue;
                    var currency = position.CurrencyCode;

                    if (currency == mainCurrency)
                        totalPnlInMainFiat += pnl.Value;
                    else if (currency == FiatCurrency.Usd.Code)
                        totalPnlInMainFiat += pnl.Value * fiatRates[mainCurrency];
                    else if (fiatRates.ContainsKey(currency))
                        totalPnlInMainFiat += (pnl.Value / fiatRates[currency]) * fiatRates[mainCurrency];
                }
            }
            totalPnlInMainFiat = Math.Round(totalPnlInMainFiat, 2);

            // Calculate P&L in BTC (sats)
            long pnlInSats = 0;
            if (fiatRates != null && btcPrice.HasValue && fiatRates.ContainsKey(mainCurrency))
            {
                var mainFiatRate = fiatRates[mainCurrency];
                pnlInSats = BtcPriceCalculator.CalculateBtcAmountOfFiat(
                    totalPnlInMainFiat, mainFiatRate, btcPrice.Value);
            }

            // Calculate BTC price to hit ATH using leveraged stack
            decimal? requiredBtcPriceLeveraged = null;
            if (AllTimeHighFiatValue.HasValue)
            {
                var currentFiat = wealth.WealthInMainFiatCurrency;
                var leveragedStackBtc = leveragedStackSats / 100_000_000m;
                if (leveragedStackBtc > 0)
                {
                    var requiredFiatDiff = AllTimeHighFiatValue.Value - currentFiat;
                    if (requiredFiatDiff > 0)
                        requiredBtcPriceLeveraged = requiredFiatDiff / leveragedStackBtc;
                }
            }

            var fiatCurrency = FiatCurrency.GetFromCode(mainCurrency);
            var leveragedStackFormatted = CurrencyDisplay.FormatSatsAsBitcoin(leveragedStackSats) + " BTC";
            var exposureFormatted = (totalBtcExposure >= 0 ? "+" : "") + totalBtcExposure.ToString("0.########") + " BTC";
            var leveragePercentFormatted = leveragePercentage.ToString("0.##") + "%";
            var positionCountFormatted = leveragedPositions.Count.ToString();
            var pnlFiatFormatted = (totalPnlInMainFiat >= 0 ? "+" : "")
                + CurrencyDisplay.FormatFiat(totalPnlInMainFiat, fiatCurrency.Code);
            var pnlBtcFormatted = (pnlInSats >= 0 ? "+" : "")
                + CurrencyDisplay.FormatSatsAsBitcoin(pnlInSats) + " BTC";

            var rows = new ObservableCollection<RowItem>
            {
                new(language.Reports_LeveragePositions_LeveragedStack, leveragedStackFormatted, TooltipContent.Text(language.Reports_LeveragePositions_LeveragedStack_Tooltip)),
                new(language.Reports_LeveragePositions_LeverageExposure, exposureFormatted),
                new(language.Reports_LeveragePositions_LeveragePercentage, leveragePercentFormatted, TooltipContent.Text(language.Reports_LeveragePositions_LeveragePercentage_Tooltip)),
                new(language.Reports_LeveragePositions_PositionCount, positionCountFormatted),
                new(language.Reports_LeveragePositions_CurrentResult, pnlFiatFormatted),
                new(language.Reports_LeveragePositions_CurrentResultBtc, pnlBtcFormatted)
            };

            if (requiredBtcPriceLeveraged.HasValue)
                rows.Add(new RowItem(language.Reports_LeveragePositions_BtcPriceToHitAth,
                    CurrencyDisplay.FormatFiat(requiredBtcPriceLeveraged.Value, fiatCurrency.Code)));

            SetData(language.Reports_LeveragePositions_Title, rows, "\uEA0B");
            IsVisible = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leverage positions data");
            IsVisible = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public override void Refresh()
    {
        RefreshAsync().FireAndForgetSafeAsync(new FireAndForgetTaskRunner(), _logger);
    }

    /// <summary>
    /// Calculates the simulated P&L for a leveraged position when BTC price is simulated.
    /// Only recalculates if the position is a BTC position (Symbol starts with "BTC").
    /// </summary>
    private decimal? CalculateSimulatedLeveragedPnl(AssetDTO position, IReadOnlyDictionary<string, decimal> fiatRates)
    {
        // If no custom price is active, return the stored P&L
        if (!_customBtcPriceState.IsActive || !_customBtcPriceState.CustomBtcPriceUsd.HasValue)
            return position.PnL;

        if (!position.EntryPrice.HasValue || position.EntryPrice.Value == 0)
            return position.PnL;

        // Get simulated BTC price in position's currency
        var simulatedPriceUsd = _customBtcPriceState.CustomBtcPriceUsd.Value;
        decimal simulatedPrice;
        if (position.CurrencyCode == FiatCurrency.Usd.Code)
        {
            simulatedPrice = simulatedPriceUsd;
        }
        else if (fiatRates.TryGetValue(position.CurrencyCode, out var rate))
        {
            simulatedPrice = simulatedPriceUsd * rate;
        }
        else
        {
            return position.PnL;
        }

        var isLong = position.IsLong ?? true;

        if (position.CollateralAssetTypeId == (int)LeveragedPositionCollateralAssetType.Btc)
        {
            if (!position.ContractCount.HasValue || !position.ContractSizeUsd.HasValue || !position.Collateral.HasValue)
                return position.PnL;

            var notionalBtcEntry = position.ContractCount.Value * position.ContractSizeUsd.Value / position.EntryPrice.Value;
            var notionalBtcCurrent = position.ContractCount.Value * position.ContractSizeUsd.Value / simulatedPrice;
            var pnlBtc = isLong
                ? notionalBtcEntry - notionalBtcCurrent
                : notionalBtcCurrent - notionalBtcEntry;
            var totalBtc = position.Collateral.Value + pnlBtc;
            return totalBtc * simulatedPrice - position.Collateral.Value * position.EntryPrice.Value;
        }

        // Legacy fiat-collateral calculation
        if (!position.Collateral.HasValue || !position.Leverage.HasValue)
            return position.PnL;

        var priceChange = (simulatedPrice - position.EntryPrice.Value) / position.EntryPrice.Value;
        var leveragedChange = priceChange * position.Leverage.Value;

        decimal currentValue;
        if (isLong)
            currentValue = position.Collateral.Value * (1 + leveragedChange);
        else
            currentValue = position.Collateral.Value * (1 - leveragedChange);

        return currentValue - position.Collateral.Value;
    }
}
