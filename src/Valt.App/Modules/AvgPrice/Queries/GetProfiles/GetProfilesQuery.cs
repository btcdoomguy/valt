using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.DTOs;

namespace Valt.App.Modules.AvgPrice.Queries.GetProfiles;

public record GetProfilesQuery : IQuery<IReadOnlyList<AvgPriceProfileDTO>>
{
    public bool ShowHidden { get; init; }
}
