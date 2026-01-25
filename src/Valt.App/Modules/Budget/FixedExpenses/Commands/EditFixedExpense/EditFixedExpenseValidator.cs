using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.EditFixedExpense;

public class EditFixedExpenseValidator : IValidator<EditFixedExpenseCommand>
{
    public ValidationResult Validate(EditFixedExpenseCommand instance)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(instance.FixedExpenseId))
            errors.Add(nameof(instance.FixedExpenseId), ["Fixed expense ID is required"]);

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(nameof(instance.Name), ["Name is required"]);

        if (string.IsNullOrWhiteSpace(instance.CategoryId))
            errors.Add(nameof(instance.CategoryId), ["Category is required"]);

        // Either DefaultAccountId or Currency must be provided, but not both
        if (string.IsNullOrWhiteSpace(instance.DefaultAccountId) && string.IsNullOrWhiteSpace(instance.Currency))
            errors.Add(nameof(instance.DefaultAccountId), ["Either default account or currency must be provided"]);

        if (!string.IsNullOrWhiteSpace(instance.DefaultAccountId) && !string.IsNullOrWhiteSpace(instance.Currency))
            errors.Add(nameof(instance.Currency), ["Cannot specify both default account and currency"]);

        // Validate new range if provided
        if (instance.NewRange is not null)
            ValidateRange(instance.NewRange, errors);

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    private static void ValidateRange(FixedExpenseRangeInputDTO range, Dictionary<string, string[]> errors)
    {
        var prefix = nameof(EditFixedExpenseCommand.NewRange);

        // Either FixedAmount or RangedAmount must be provided
        var hasFixedAmount = range.FixedAmount.HasValue;
        var hasRangedAmount = range.RangedAmountMin.HasValue || range.RangedAmountMax.HasValue;

        if (!hasFixedAmount && !hasRangedAmount)
        {
            errors.Add($"{prefix}.FixedAmount", ["Either fixed amount or ranged amount must be provided"]);
        }
        else if (hasFixedAmount && hasRangedAmount)
        {
            errors.Add($"{prefix}.FixedAmount", ["Cannot specify both fixed amount and ranged amount"]);
        }
        else if (hasRangedAmount)
        {
            if (!range.RangedAmountMin.HasValue)
                errors.Add($"{prefix}.RangedAmountMin", ["Minimum amount is required for ranged amounts"]);
            if (!range.RangedAmountMax.HasValue)
                errors.Add($"{prefix}.RangedAmountMax", ["Maximum amount is required for ranged amounts"]);
            if (range.RangedAmountMin.HasValue && range.RangedAmountMax.HasValue && range.RangedAmountMin > range.RangedAmountMax)
                errors.Add($"{prefix}.RangedAmountMin", ["Minimum amount cannot be greater than maximum"]);
        }
        else if (hasFixedAmount && range.FixedAmount <= 0)
        {
            errors.Add($"{prefix}.FixedAmount", ["Fixed amount must be greater than zero"]);
        }

        // Validate period
        if (range.PeriodId < 0 || range.PeriodId > 3)
            errors.Add($"{prefix}.PeriodId", ["Invalid period type"]);

        // Validate day based on period
        bool isMonthlyOrYearly = range.PeriodId is 0 or 1; // Monthly or Yearly
        if (isMonthlyOrYearly)
        {
            if (range.Day < 1 || range.Day > 31)
                errors.Add($"{prefix}.Day", ["Day must be between 1 and 31 for monthly/yearly periods"]);
        }
        else
        {
            if (range.Day < 0 || range.Day > 6)
                errors.Add($"{prefix}.Day", ["Day must be between 0 (Sunday) and 6 (Saturday) for weekly/biweekly periods"]);
        }
    }
}
