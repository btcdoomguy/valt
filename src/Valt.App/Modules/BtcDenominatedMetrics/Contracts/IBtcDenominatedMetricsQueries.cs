using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.App.Modules.BtcDenominatedMetrics.Queries;

namespace Valt.App.Modules.BtcDenominatedMetrics.Contracts;

public interface IBtcDenominatedMetricsQueries
{
    Task<BtcDenominatedMetricsDataDto> GetBtcDenominatedMetricsAsync(GetBtcDenominatedMetricsQuery query);
}
