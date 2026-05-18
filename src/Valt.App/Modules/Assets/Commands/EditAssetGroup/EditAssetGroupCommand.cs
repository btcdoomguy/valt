using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;

namespace Valt.App.Modules.Assets.Commands.EditAssetGroup;

public record EditAssetGroupCommand : ICommand<Unit>
{
    public required string GroupId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}

internal sealed class EditAssetGroupValidator : IValidator<EditAssetGroupCommand>
{
    public ValidationResult Validate(EditAssetGroupCommand command)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(command.GroupId, nameof(command.GroupId), "Group ID is required.");
        builder.AddErrorIfNullOrWhiteSpace(command.Name, nameof(command.Name), "Name is required.");
        if (!string.IsNullOrWhiteSpace(command.Name) && command.Name.Length > 50)
            builder.AddError(nameof(command.Name), "Name must be 50 characters or less.");

        return builder.Build();
    }
}

internal sealed class EditAssetGroupHandler : ICommandHandler<EditAssetGroupCommand, Unit>
{
    private readonly IAssetGroupRepository _assetGroupRepository;
    private readonly IValidator<EditAssetGroupCommand> _validator;

    public EditAssetGroupHandler(
        IAssetGroupRepository assetGroupRepository,
        IValidator<EditAssetGroupCommand> validator)
    {
        _assetGroupRepository = assetGroupRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(EditAssetGroupCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<Unit>.Failure(new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var group = await _assetGroupRepository.GetByIdAsync(new AssetGroupId(command.GroupId));
        if (group is null)
            return Result<Unit>.NotFound("AssetGroup", command.GroupId);

        group.Rename(AssetGroupName.New(command.Name));
        group.ChangeDescription(command.Description);
        await _assetGroupRepository.SaveAsync(group);

        return Result<Unit>.Success(Unit.Value);
    }
}
