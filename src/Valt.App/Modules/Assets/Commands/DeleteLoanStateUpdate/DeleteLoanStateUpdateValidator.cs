using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.DeleteLoanStateUpdate;

internal sealed class DeleteLoanStateUpdateValidator : IValidator<DeleteLoanStateUpdateCommand>
{
    public ValidationResult Validate(DeleteLoanStateUpdateCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");

        if (instance.EffectiveDate == default)
            builder.AddError(nameof(instance.EffectiveDate), "Effective date is required.");

        return builder.Build();
    }
}
