using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Assets.Commands.CreateBtcLoan;

internal sealed class CreateBtcLoanValidator : IValidator<CreateBtcLoanCommand>
{
    private const int MaxNameLength = 100;

    public ValidationResult Validate(CreateBtcLoanCommand instance)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(instance.Name, nameof(instance.Name), "Loan name is required.");

        if (instance.Name?.Length > MaxNameLength)
            builder.AddError(nameof(instance.Name), $"Loan name cannot exceed {MaxNameLength} characters.");

        builder.AddErrorIfNullOrWhiteSpace(instance.CurrencyCode, nameof(instance.CurrencyCode), "Currency code is required.");

        builder.AddErrorIfNullOrWhiteSpace(instance.PlatformName, nameof(instance.PlatformName), "Platform name is required.");

        if (instance.CollateralSats <= 0)
            builder.AddError(nameof(instance.CollateralSats), "Collateral must be greater than zero.");

        if (instance.LoanAmount <= 0)
            builder.AddError(nameof(instance.LoanAmount), "Loan amount must be greater than zero.");

        if (instance.Apr < 0)
            builder.AddError(nameof(instance.Apr), "APR cannot be negative.");

        if (instance.InitialLtv <= 0)
            builder.AddError(nameof(instance.InitialLtv), "Initial LTV must be greater than zero.");

        if (instance.MarginCallLtv <= 0)
            builder.AddError(nameof(instance.MarginCallLtv), "Margin call LTV must be greater than zero.");

        if (instance.LiquidationLtv <= 0)
            builder.AddError(nameof(instance.LiquidationLtv), "Liquidation LTV must be greater than zero.");

        if (instance.LiquidationLtv <= instance.MarginCallLtv)
            builder.AddError(nameof(instance.LiquidationLtv), "Liquidation LTV must be greater than margin call LTV.");

        if (instance.Fees < 0)
            builder.AddError(nameof(instance.Fees), "Fees cannot be negative.");

        if (instance.FixedTotalDebt.HasValue)
        {
            if (instance.FixedTotalDebt.Value < instance.LoanAmount + instance.Fees)
                builder.AddError(nameof(instance.FixedTotalDebt),
                    "Fixed total debt must be greater than or equal to the loan amount plus fees.");

            if (!instance.RepaymentDate.HasValue)
                builder.AddError(nameof(instance.RepaymentDate),
                    "Repayment date is required when using a fixed total debt.");
            else if (instance.RepaymentDate.Value <= instance.LoanStartDate)
                builder.AddError(nameof(instance.RepaymentDate),
                    "Repayment date must be after the loan start date when using a fixed total debt.");
        }

        return builder.Build();
    }
}
