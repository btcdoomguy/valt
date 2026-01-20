using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.WealthOverview;

public class WealthOverviewData
{
    public required FiatCurrency MainCurrency { get; init; }
    public required WealthOverviewPeriod Period { get; init; }
    public required IReadOnlyList<Item> Items { get; init; }

    public record Item(DateOnly PeriodEnd, string Label, decimal FiatTotal, decimal BtcTotal);
}
