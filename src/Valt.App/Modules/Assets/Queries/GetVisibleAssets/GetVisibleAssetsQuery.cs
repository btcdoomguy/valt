using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetVisibleAssets;

/// <summary>
/// Query to get only visible assets, ordered by display order and name.
/// </summary>
public record GetVisibleAssetsQuery : IQuery<IReadOnlyList<AssetDTO>>;
