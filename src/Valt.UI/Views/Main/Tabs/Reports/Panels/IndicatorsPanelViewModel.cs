using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.Indicators;
using Valt.UI.Lang;
using Valt.UI.UserControls;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// Panel displaying market indicators (Mayer Multiple, Rainbow Chart, Fear & Greed, BTC Dominance).
/// </summary>
public partial class IndicatorsPanelViewModel : DashboardPanelViewModel
{
    private readonly IIndicatorCache _indicatorCache;

    public IndicatorsPanelViewModel(IIndicatorCache indicatorCache, ILogger<IndicatorsPanelViewModel> logger)
        : base(logger)
    {
        _indicatorCache = indicatorCache;
    }

    public override Task RefreshAsync()
    {
        var cached = _indicatorCache.GetLatest();
        if (cached is not null)
        {
            Dispatcher.UIThread.Post(() => UpdateIndicatorsData(cached));
        }
        else
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }

    public void UpdateIndicatorsData(IndicatorSnapshot snapshot)
    {
        var rows = new ObservableCollection<RowItem>();

        if (snapshot.MayerMultiple is not null)
            rows.Add(new RowItem(language.Reports_Indicators_MayerMultiple,
                snapshot.MayerMultiple.Multiple.ToString("F2"),
                Tooltip: new TooltipContent([
                    new TooltipLine([new TooltipRun("> 2.4", Bold: true), new TooltipRun(": overvalued")]),
                    new TooltipLine([new TooltipRun("1.0", Bold: true), new TooltipRun(": fair value")]),
                    new TooltipLine([new TooltipRun("< 0.8", Bold: true), new TooltipRun(": buying opportunity")]),
                ]),
                Url: "https://charts.bitcoin.com/mayer.html"));

        if (snapshot.RainbowChart is not null)
            rows.Add(new RowItem(language.Reports_Indicators_RainbowChart,
                snapshot.RainbowChart.CurrentZone,
                Tooltip: new TooltipContent([
                    new TooltipLine([new TooltipRun("Zones (low\u2192high):", Bold: true)]),
                    new TooltipLine([new TooltipRun("Fire Sale \u2192 Basically a Fire Sale \u2192 Buy! \u2192 Accumulate \u2192 Still Cheap \u2192 HODL! \u2192 Is this a bubble? \u2192 FOMO Intensifies \u2192 Sell. Seriously, SELL! \u2192 Maximum Bubble Territory")]),
                ]),
                Url: "https://charts.bitcoin.com/rainbow.html"));

        if (snapshot.FearAndGreed is not null)
            rows.Add(new RowItem(language.Reports_Indicators_FearAndGreed,
                $"{snapshot.FearAndGreed.Value} - {snapshot.FearAndGreed.Classification}",
                Tooltip: new TooltipContent([
                    new TooltipLine([new TooltipRun("0\u201325", Bold: true), new TooltipRun(": Extreme Fear (buy)")]),
                    new TooltipLine([new TooltipRun("26\u201350", Bold: true), new TooltipRun(": Fear")]),
                    new TooltipLine([new TooltipRun("51\u201375", Bold: true), new TooltipRun(": Greed")]),
                    new TooltipLine([new TooltipRun("76\u2013100", Bold: true), new TooltipRun(": Extreme Greed (sell)")]),
                ]),
                Url: "https://alternative.me/crypto/fear-and-greed-index/"));

        if (snapshot.BitcoinDominance is not null)
            rows.Add(new RowItem(language.Reports_Indicators_BtcDominance,
                $"{snapshot.BitcoinDominance.DominancePercent:F1}%",
                Tooltip: new TooltipContent([
                    new TooltipLine([new TooltipRun("> 60%", Bold: true), new TooltipRun(": Bitcoin season")]),
                    new TooltipLine([new TooltipRun("< 40%", Bold: true), new TooltipRun(": Altcoin season")]),
                ]),
                Url: "https://www.coingecko.com/en/global-charts"));

        var isStale = !snapshot.IsUpToDate;
        SetData(language.Reports_Indicators_Title, rows, "\uE6C4", isStale);
    }
}
