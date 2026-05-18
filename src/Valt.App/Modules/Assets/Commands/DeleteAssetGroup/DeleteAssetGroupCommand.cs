using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;

namespace Valt.App.Modules.Assets.Commands.DeleteAssetGroup;

public record DeleteAssetGroupCommand : ICommand<Unit>
{
    public required string GroupId { get; init; }
}

internal sealed class DeleteAssetGroupHandler : ICommandHandler<DeleteAssetGroupCommand, Unit>
{
    private readonly IAssetGroupRepository _assetGroupRepository;

    public DeleteAssetGroupHandler(IAssetGroupRepository assetGroupRepository)
    {
        _assetGroupRepository = assetGroupRepository;
    }

    public async Task<Result<Unit>> HandleAsync(DeleteAssetGroupCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.GroupId))
            return Result<Unit>.Failure("VALIDATION_FAILED", "Group ID is required.");

        var group = await _assetGroupRepository.GetByIdAsync(new AssetGroupId(command.GroupId));
        if (group is null)
            return Result<Unit>.NotFound("AssetGroup", command.GroupId);

        await _assetGroupRepository.DeleteAsync(group.Id);
        return Result<Unit>.Success(Unit.Value);
    }
}
