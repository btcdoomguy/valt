using Valt.App.Kernel.Queries;
using Valt.Core.Modules.AvgPrice;
using static Valt.Core.Modules.AvgPrice.Calculations.IAvgPriceTotalizer;

namespace Valt.App.Modules.AvgPrice.Queries.GetAvgPriceTotals;

public sealed record GetAvgPriceTotalsQuery(AvgPriceProfileId ProfileId, int Year) : IQuery<TotalsDTO>
{
    public GetAvgPriceTotalsQuery(int year, AvgPriceProfileId profileId) : this(profileId, year)
    {
    }
}
