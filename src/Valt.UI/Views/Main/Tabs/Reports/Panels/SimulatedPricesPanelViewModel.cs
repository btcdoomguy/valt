using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.UI.Lang;
using Valt.UI.State;
using Valt.UI.UserControls;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// Panel displaying portfolio value at simulated BTC price scenarios.
/// </summary>
public partial class SimulatedPricesPanelViewModel : DashboardPanelViewModel
{
    private readonly IConfigurationManager _configurationManager;
    private readonly AccountsTotalState _accountsTotalState;
    private readonly RatesState _ratesState;
    private readonly CurrencySettings _currencySettings;

    public SimulatedPricesPanelViewModel(
        IConfigurationManager configurationManager,
        AccountsTotalState accountsTotalState,
        RatesState ratesState,
        CurrencySettings currencySettings,
        ILogger<SimulatedPricesPanelViewModel> logger)
        : base(logger)
    {
        _configurationManager = configurationManager;
        _accountsTotalState = accountsTotalState;
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
            var lines = _configurationManager.GetSimulatedPriceLines();
            var currentBtcPriceUsd = _ratesState.BitcoinPrice;
            var fiatRates = _ratesState.FiatRates;
            var mainCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);
            var wealth = _accountsTotalState.CurrentWealth;

            if (currentBtcPriceUsd is null || fiatRates is null || !fiatRates.ContainsKey(mainCurrency.Code) || lines.Count == 0)
            {
                SetEmpty(language.Reports_SimulatedPrices_Title, "\uF04A");
                return;
            }

            var mainFiatRate = fiatRates[mainCurrency.Code];
            var wealthInSats = wealth.WealthInSats;
            var wealthInMainFiat = wealth.WealthInMainFiatCurrency;

            var rows = new ObservableCollection<RowItem>();

            foreach (var line in lines)
            {
                decimal simulatedPriceUsd;
                string leftLabel;

                if (line.Type == SimulatedPriceType.Percentage)
                {
                    simulatedPriceUsd = (line.Value / 100m) * currentBtcPriceUsd.Value;
                    leftLabel = $"{FormatFiat(simulatedPriceUsd, FiatCurrency.Usd.Code)} ({line.Value:G0}%)";
                }
                else
                {
                    simulatedPriceUsd = line.Value;
                    leftLabel = $"{FormatFiat(simulatedPriceUsd, FiatCurrency.Usd.Code)} ({language.Reports_SimulatedPrices_Fixed})";
                }

                var btcPortionInFiat = (wealthInSats / 100_000_000m) * simulatedPriceUsd * mainFiatRate;
                var simulatedTotalFiat = btcPortionInFiat + wealthInMainFiat;

                rows.Add(new RowItem(leftLabel, FormatFiat(simulatedTotalFiat, mainCurrency.Code)));
            }

            SetData(language.Reports_SimulatedPrices_Title, rows, "\uF04A");
        }
        catch (Exception ex)
        {
            SetError(language.Reports_SimulatedPrices_Title, ex, "\uF04A");
        }
    }
}
