using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.AddLoanStateUpdate;

internal sealed class AddLoanStateUpdateValidator : IValidator<AddLoanStateUpdateCommand>
{
    public ValidationResult Validate(AddLoanStateUpdateCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");

        if (instance.EffectiveDate == default)
            builder.AddError(nameof(instance.EffectiveDate), "Effective date is required.");

        if (instance.CurrentTotalDebt < 0)
            builder.AddError(nameof(instance.CurrentTotalDebt), "Current total debt cannot be negative.");

        if (instance.CollateralSats <= 0)
            builder.AddError(nameof(instance.CollateralSats), "Collateral must be greater than zero.");

        if (instance.Apr < 0)
            builder.AddError(nameof(instance.Apr), "APR cannot be negative.");

        if (instance.Fees < 0)
            builder.AddError(nameof(instance.Fees), "Fees cannot be negative.");

        return builder.Build();
    }
}
