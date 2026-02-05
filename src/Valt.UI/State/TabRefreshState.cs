using System;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.UI.Views;

namespace Valt.UI.State;

public partial class TabRefreshState : ObservableObject
{
    private readonly Lock _lock = new();

    [ObservableProperty]
    private bool _transactionsNeedsRefresh;

    [ObservableProperty]
    private bool _reportsNeedsRefresh;

    [ObservableProperty]
    private bool _avgPriceNeedsRefresh;

    [ObservableProperty]
    private bool _assetsNeedsRefresh;

    public bool NeedsRefresh(MainViewTabNames tab)
    {
        lock (_lock)
        {
            return tab switch
            {
                MainViewTabNames.TransactionsPageContent => TransactionsNeedsRefresh,
                MainViewTabNames.ReportsPageContent => ReportsNeedsRefresh,
                MainViewTabNames.AvgPricePageContent => AvgPriceNeedsRefresh,
                MainViewTabNames.AssetsPageContent => AssetsNeedsRefresh,
                _ => false
            };
        }
    }

    public void SetAllNeedRefresh()
    {
        lock (_lock)
        {
            Dispatcher.UIThread.Post(() =>
            {
                TransactionsNeedsRefresh = true;
                ReportsNeedsRefresh = true;
                AvgPriceNeedsRefresh = true;
                AssetsNeedsRefresh = true;
            });
        }
    }

    public void ClearRefresh(MainViewTabNames tab)
    {
        lock (_lock)
        {
            Dispatcher.UIThread.Post(() =>
            {
                switch (tab)
                {
                    case MainViewTabNames.TransactionsPageContent:
                        TransactionsNeedsRefresh = false;
                        break;
                    case MainViewTabNames.ReportsPageContent:
                        ReportsNeedsRefresh = false;
                        break;
                    case MainViewTabNames.AvgPricePageContent:
                        AvgPriceNeedsRefresh = false;
                        break;
                    case MainViewTabNames.AssetsPageContent:
                        AssetsNeedsRefresh = false;
                        break;
                }
            });
        }
    }

    public void ClearAll()
    {
        lock (_lock)
        {
            Dispatcher.UIThread.Post(() =>
            {
                TransactionsNeedsRefresh = false;
                ReportsNeedsRefresh = false;
                AvgPriceNeedsRefresh = false;
                AssetsNeedsRefresh = false;
            });
        }
    }
}
