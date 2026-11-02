using Valt.Core.Common;
using Valt.Infra.Kernel;

namespace Valt.Infra.Modules.Reports.MonthlyTotals;

/// <summary>
/// Represents the monthly totals data
/// </summary>
public record MonthlyTotalsData
{
    public required FiatCurrency MainCurrency { get; init; }
    public required IReadOnlyList<Item> Items { get; init; }
    public required Totals Total { get; init; }
    
    public record Item
    {
        public required DateOnly MonthYear { get; init; }
        public required decimal BtcTotal { get; init; }

        /// <summary>
        /// Asset-inclusive Total Wealth change vs. the previous month: (bitcoin balance +
        /// latest wealth snapshot sats, converted to BTC) vs. the same wealth basis at the
        /// previous month's end. A month with no qualifying snapshot contributes 0 assets
        /// to its side; 0% when the previous month has no data.
        /// </summary>
        public required decimal BtcMonthlyChange { get; init; }

        /// <summary>
        /// Asset-inclusive Total Wealth change vs. the end of the previous calendar year:
        /// (bitcoin balance + latest wealth snapshot sats, converted to BTC) vs. the same
        /// wealth basis at the prior year's December end. A month with no qualifying
        /// snapshot contributes 0 assets to its side; 0% only when the previous calendar
        /// year has no monthly data.
        /// </summary>
        public required decimal BtcYearlyChange { get; init; }

        public required decimal FiatTotal { get; init; }

        /// <summary>
        /// Asset-inclusive Total Wealth change vs. the previous month: (fiat balance +
        /// latest wealth snapshot value in the main currency) vs. the same wealth basis at
        /// the previous month's end. A month with no qualifying snapshot contributes 0
        /// assets to its side; 0% when the previous month has no data.
        /// </summary>
        public required decimal FiatMonthlyChange { get; init; }

        /// <summary>
        /// Asset-inclusive Total Wealth change vs. the end of the previous calendar year:
        /// (fiat balance + latest wealth snapshot value in the main currency) vs. the same
        /// wealth basis at the prior year's December end. A month with no qualifying
        /// snapshot contributes 0 assets to its side; 0% only when the previous calendar
        /// year has no monthly data.
        /// </summary>
        public required decimal FiatYearlyChange { get; init; }
        
        public required decimal Income { get; init; }
        public required decimal Expenses { get; init; }
        
        public required decimal BitcoinPurchased { get; init; }
        public required decimal BitcoinSold { get; init; }
        
        public required decimal BitcoinIncome { get; init; }
        public required decimal BitcoinExpenses { get; init; }
        
        public required decimal AllIncomeInFiat { get; init; }
        public required decimal AllExpensesInFiat { get; init; }

        /// <summary>
        /// Aggregate "Included in Wealth Totals" asset value from the latest wealth snapshot
        /// on or before this period's end date, in the report's main currency. Zero when no
        /// snapshot exists on or before the period end (or the snapshot currency mismatches).
        /// </summary>
        public decimal AssetsTotalFiat { get; init; }

        /// <summary>
        /// Same snapshot value expressed in satoshis; zero under the same fallback conditions.
        /// </summary>
        public long AssetsTotalSats { get; init; }
    }

    public record Totals
    {
        public required decimal Income { get; init; }
        public required decimal Expenses { get; init; }
        
        public required decimal BitcoinPurchased { get; init; }
        public required decimal BitcoinSold { get; init; }
        
        public required decimal BitcoinIncome { get; init; }
        public required decimal BitcoinExpenses { get; init; }
        
        public required decimal AllIncomeInFiat { get; init; }
        public required decimal AllExpensesInFiat { get; init; }
    }
}