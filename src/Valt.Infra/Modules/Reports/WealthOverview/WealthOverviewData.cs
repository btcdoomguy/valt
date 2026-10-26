using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.WealthOverview;

public class WealthOverviewData
{
    public required FiatCurrency MainCurrency { get; init; }
    public required WealthOverviewPeriod Period { get; init; }
    public required IReadOnlyList<Item> Items { get; init; }

    /// <param name="AssetsTotalFiat">
    /// Aggregate "Included in Wealth Totals" asset value from the latest wealth snapshot
    /// on or before this period's end date (or today for future periods), in the report's
    /// main currency. Zero when no snapshot qualifies (or the snapshot currency mismatches).
    /// Kept separate from <paramref name="FiatTotal"/> so the UI decides how to combine.
    /// </param>
    public record Item(DateOnly PeriodEnd, string Label, decimal FiatTotal, decimal BtcTotal,
        decimal AssetsTotalFiat = 0);
}
