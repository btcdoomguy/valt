using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports;


/// <summary>
/// Represents the monthly totals data
/// </summary>
public record MonthlyTotalsData
{
    public required DateOnly MonthYear { get; init; }
    public required BitcoinData Bitcoin { get; init; }
    public required FiatData Fiat { get; init; }
    
    public record BitcoinData
    {
        public required decimal BtcTotal { get; init; }
        public required decimal VariationFromPreviousMonth { get; init; }
        public required decimal VariationFromPreviousYear { get; init; }
    }

    public record FiatData
    {
        public required FiatCurrency MainCurrency { get; init; }
        public required decimal FiatTotal { get; init; }
        public required decimal VariationFromPreviousMonth { get; init; }
        public required decimal VariationFromPreviousYear { get; init; }
    }
}