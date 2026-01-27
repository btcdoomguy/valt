using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.CreateFixedExpense;

public class CreateFixedExpenseValidator : IValidator<CreateFixedExpenseCommand>
{
    public ValidationResult Validate(CreateFixedExpenseCommand instance)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(nameof(instance.Name), ["Name is required"]);

        if (string.IsNullOrWhiteSpace(instance.CategoryId))
            errors.Add(nameof(instance.CategoryId), ["Category is required"]);

        // Either DefaultAccountId or Currency must be provided, but not both
        if (string.IsNullOrWhiteSpace(instance.DefaultAccountId) && string.IsNullOrWhiteSpace(instance.Currency))
            errors.Add(nameof(instance.DefaultAccountId), ["Either default account or currency must be provided"]);

        if (!string.IsNullOrWhiteSpace(instance.DefaultAccountId) && !string.IsNullOrWhiteSpace(instance.Currency))
            errors.Add(nameof(instance.Currency), ["Cannot specify both default account and currency"]);

        if (instance.Ranges.Count == 0)
            errors.Add(nameof(instance.Ranges), ["At least one range is required"]);
        else
        {
            for (int i = 0; i < instance.Ranges.Count; i++)
            {
                var range = instance.Ranges[i];
                ValidateRange(range, i, errors);
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    private static void ValidateRange(FixedExpenses.DTOs.FixedExpenseRangeInputDTO range, int index, Dictionary<string, string[]> errors)
    {
        var prefix = $"Ranges[{index}]";

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
