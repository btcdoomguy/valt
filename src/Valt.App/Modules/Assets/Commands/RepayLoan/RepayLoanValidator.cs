using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.RepayLoan;

internal sealed class RepayLoanValidator : IValidator<RepayLoanCommand>
{
    public ValidationResult Validate(RepayLoanCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.AssetId, nameof(instance.AssetId), "Asset ID is required.");

        return builder.Build();
    }
}
