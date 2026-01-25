using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.DTOs;

namespace Valt.App.Modules.AvgPrice.Queries.GetLinesOfProfile;

public record GetLinesOfProfileQuery : IQuery<IReadOnlyList<AvgPriceLineDTO>>
{
    public required string ProfileId { get; init; }
}
