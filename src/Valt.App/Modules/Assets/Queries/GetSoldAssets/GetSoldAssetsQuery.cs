using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetSoldAssets;

/// <summary>
/// Query to get all sold assets, ordered by date sold descending, then by name.
/// </summary>
public record GetSoldAssetsQuery : IQuery<IReadOnlyList<AssetDTO>>;
