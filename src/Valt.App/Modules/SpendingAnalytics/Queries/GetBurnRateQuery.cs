using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingAnalytics.DTOs;

namespace Valt.App.Modules.SpendingAnalytics.Queries;

public record GetBurnRateQuery : IQuery<BurnRateDataDto>
{
    public required decimal CurrentWealthInFiat { get; init; }
    public string[] CategoryIds { get; init; } = Array.Empty<string>();
    public string[] AccountIds { get; init; } = Array.Empty<string>();
}
