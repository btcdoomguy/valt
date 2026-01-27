using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.DTOs;

namespace Valt.App.Modules.AvgPrice.Queries.GetProfile;

/// <summary>
/// Query to get a single average price profile by ID.
/// </summary>
public record GetProfileQuery : IQuery<AvgPriceProfileDTO?>
{
    public required string ProfileId { get; init; }
}
