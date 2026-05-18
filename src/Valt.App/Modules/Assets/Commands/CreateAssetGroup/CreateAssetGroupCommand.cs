using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;

namespace Valt.App.Modules.Assets.Commands.CreateAssetGroup;

public record CreateAssetGroupCommand : ICommand<string>
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}

internal sealed class CreateAssetGroupValidator : IValidator<CreateAssetGroupCommand>
{
    public ValidationResult Validate(CreateAssetGroupCommand command)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(command.Name, nameof(command.Name), "Name is required.");
        if (!string.IsNullOrWhiteSpace(command.Name) && command.Name.Length > 50)
            builder.AddError(nameof(command.Name), "Name must be 50 characters or less.");

        return builder.Build();
    }
}

internal sealed class CreateAssetGroupHandler : ICommandHandler<CreateAssetGroupCommand, string>
{
    private readonly IAssetGroupRepository _assetGroupRepository;
    private readonly IValidator<CreateAssetGroupCommand> _validator;

    public CreateAssetGroupHandler(
        IAssetGroupRepository assetGroupRepository,
        IValidator<CreateAssetGroupCommand> validator)
    {
        _assetGroupRepository = assetGroupRepository;
        _validator = validator;
    }

    public async Task<Result<string>> HandleAsync(CreateAssetGroupCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<string>.Failure(new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var group = AssetGroup.New(command.Name, command.Description);
        await _assetGroupRepository.SaveAsync(group);

        return Result<string>.Success(group.Id.Value);
    }
}
