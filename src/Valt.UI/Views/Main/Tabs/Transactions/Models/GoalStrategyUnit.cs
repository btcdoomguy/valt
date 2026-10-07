namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

/// <summary>
/// Unit in which the Goal Summary modal renders running totals. A property of the
/// goal type (not of any single modal), shared by the goals panel and the summary modal.
/// </summary>
public enum GoalStrategyUnit
{
    Fiat,
    Sats,
    Count,
    Percentage
}
