using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetAssets;

/// <summary>
/// Query to get all assets, ordered by visibility (visible first), display order, and name.
/// </summary>
public record GetAssetsQuery : IQuery<IReadOnlyList<AssetDTO>>;
