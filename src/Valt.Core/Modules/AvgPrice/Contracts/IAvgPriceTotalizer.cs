namespace Valt.Core.Modules.AvgPrice.Calculations;

public interface IAvgPriceTotalizer
{
    Task<TotalsDTO> GetTotalsAsync(int year, IEnumerable<AvgPriceProfileId> profileIds);

    public record TotalsDTO(int Year, IEnumerable<MonthlyTotalsDTO> MonthlyTotals, ValuesDTO YearlyTotals);
    
    public record MonthlyTotalsDTO(DateTime Month, ValuesDTO Values);
    
    public record ValuesDTO(decimal AmountBought, decimal AmountSold, decimal TotalProfitLoss, decimal Volume);
}