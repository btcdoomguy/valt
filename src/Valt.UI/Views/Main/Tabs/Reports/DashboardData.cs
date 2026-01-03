using System.Collections.ObjectModel;
using LiveChartsCore.Kernel;

namespace Valt.UI.Views.Main.Tabs.Reports;

public record DashboardData(string Title, ObservableCollection<RowItem> Rows)
{
    public static DashboardData DesignTimeSample => new(
        "Your all-time high",
        new ObservableCollection<RowItem>
        {
            new("All-time high", "$ 125,432.00"),
            new("Date", "Dec 15, 2025"),
            new("Current difference", "-12.5%"),
            new("Max drawdown date", "Nov 03, 2024"),
            new("Max drawdown", "-35.2%")
        });

    public static DashboardData BtcStackDesignTimeSample => new(
        "Your BTC Stack",
        new ObservableCollection<RowItem>
        {
            new("Current stack", "1.23456789 BTC"),
            new("% of total supply", "0.00000588%"),
            new("People with same stack", "17,010,309")
        });
}