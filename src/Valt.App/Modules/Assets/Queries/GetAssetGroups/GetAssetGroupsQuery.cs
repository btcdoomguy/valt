using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetAssetGroups;

public record GetAssetGroupsQuery : IQuery<IReadOnlyList<AssetGroupDTO>>;
