using Valt.App.Kernel.Queries;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;

namespace Valt.App.Modules.BtcDenominatedMetrics.Queries;

public record GetBtcDenominatedMetricsQuery : IQuery<BtcDenominatedMetricsDataDto>
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string[] CategoryIds { get; init; } = Array.Empty<string>();
    public string[] AccountIds { get; init; } = Array.Empty<string>();
}
