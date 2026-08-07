using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;

namespace Valt.App.Modules.SpendingAnalytics.Contracts;

public interface IBurnRateQueries
{
    Task<BurnRateDataDto> GetBurnRateAsync(GetBurnRateQuery query);
}
