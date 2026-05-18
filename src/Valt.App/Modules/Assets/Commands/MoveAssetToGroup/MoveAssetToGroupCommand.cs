using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;

namespace Valt.App.Modules.Assets.Commands.MoveAssetToGroup;

public record MoveAssetToGroupCommand : ICommand<Unit>
{
    public required string AssetId { get; init; }
    public string? TargetGroupId { get; init; }
}

internal sealed class MoveAssetToGroupHandler : ICommandHandler<MoveAssetToGroupCommand, Unit>
{
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetGroupRepository _assetGroupRepository;

    public MoveAssetToGroupHandler(
        IAssetRepository assetRepository,
        IAssetGroupRepository assetGroupRepository)
    {
        _assetRepository = assetRepository;
        _assetGroupRepository = assetGroupRepository;
    }

    public async Task<Result<Unit>> HandleAsync(MoveAssetToGroupCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.AssetId))
            return Result<Unit>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.AssetId), ["Asset ID is required"] }
                }));

        var asset = await _assetRepository.GetByIdAsync(new AssetId(command.AssetId));
        if (asset is null)
            return Result<Unit>.Failure("ASSET_NOT_FOUND", $"Asset with id {command.AssetId} not found");

        AssetGroupId? targetGroupId = null;

        if (!string.IsNullOrWhiteSpace(command.TargetGroupId))
        {
            var targetGroup = await _assetGroupRepository.GetByIdAsync(new AssetGroupId(command.TargetGroupId));
            if (targetGroup is null)
                return Result<Unit>.Failure("GROUP_NOT_FOUND", $"Asset group with id {command.TargetGroupId} not found");

            targetGroupId = targetGroup.Id;
        }

        asset.AssignToGroup(targetGroupId);
        await _assetRepository.SaveAsync(asset);

        return Result<Unit>.Success(Unit.Value);
    }
}
