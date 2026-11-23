using Valt.App.Kernel.Queries;
using Valt.Core.Modules.AvgPrice.Calculations;
using static Valt.Core.Modules.AvgPrice.Calculations.IAvgPriceTotalizer;

namespace Valt.App.Modules.AvgPrice.Queries.GetAvgPriceTotals;

internal sealed class GetAvgPriceTotalsHandler(IAvgPriceTotalizer avgPriceTotalizer)
    : IQueryHandler<GetAvgPriceTotalsQuery, TotalsDTO>
{
    public Task<TotalsDTO> HandleAsync(GetAvgPriceTotalsQuery query, CancellationToken ct = default)
    {
        return avgPriceTotalizer.GetTotalsAsync(query.Year, [query.ProfileId]);
    }
}
