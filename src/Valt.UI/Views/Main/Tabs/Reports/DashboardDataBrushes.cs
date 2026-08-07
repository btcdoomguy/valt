using Avalonia.Media;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Reports;

/// <summary>
/// Provides semantic brushes for dashboard row values based on configurable thresholds.
/// </summary>
public static class DashboardDataBrushes
{
    public static IBrush ForLtv(decimal ltv) => ltv switch
    {
        <= 60m => TransactionGridResources.Credit,
        <= 75m => TransactionGridResources.Warning,
        _ => TransactionGridResources.Debt
    };

    public static IBrush ForStackPledged(decimal percent) => percent switch
    {
        <= 40m => TransactionGridResources.Credit,
        _ => TransactionGridResources.Debt
    };

    public static IBrush ForMayerMultiple(decimal value) => value switch
    {
        <= 0.8m => TransactionGridResources.Credit,
        < 1.2m => TransactionGridResources.Warning,
        _ => TransactionGridResources.Debt
    };

    public static IBrush ForFearAndGreed(int value) => value switch
    {
        <= 30 => TransactionGridResources.Debt,
        <= 70 => TransactionGridResources.Warning,
        _ => TransactionGridResources.Credit
    };

    public static IBrush ForAthDifference(decimal percent)
    {
        var abs = System.Math.Abs(percent);
        return abs switch
        {
            <= 25m => TransactionGridResources.Credit,
            <= 50m => TransactionGridResources.Warning,
            _ => TransactionGridResources.Debt
        };
    }

    public static IBrush ForLeverage(decimal percent) => percent switch
    {
        <= 30m => TransactionGridResources.Credit,
        <= 70m => TransactionGridResources.Warning,
        _ => TransactionGridResources.Debt
    };

    public static IBrush ForYoYEvolution(decimal percent)
    {
        var abs = System.Math.Abs(percent);
        return abs switch
        {
            <= 15m => TransactionGridResources.Credit,
            <= 30m => TransactionGridResources.Warning,
            _ => TransactionGridResources.Debt
        };
    }
}
