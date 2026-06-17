using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Validation;

public static class FixedExpenseValidationRules
{
    public static void ValidateCommonFields(string? name, string? categoryId, string? defaultAccountId, string? currency, ValidationResultBuilder builder)
    {
        builder.AddErrorIfNullOrWhiteSpace(name, nameof(name), "Name is required");
        builder.AddErrorIfNullOrWhiteSpace(categoryId, nameof(categoryId), "Category is required");

        if (string.IsNullOrWhiteSpace(defaultAccountId) && string.IsNullOrWhiteSpace(currency))
            builder.AddError(nameof(defaultAccountId), "Either default account or currency must be provided");

        if (!string.IsNullOrWhiteSpace(defaultAccountId) && !string.IsNullOrWhiteSpace(currency))
            builder.AddError(nameof(currency), "Cannot specify both default account and currency");
    }

    public static void ValidateRange(FixedExpenseRangeInputDTO range, string prefix, ValidationResultBuilder builder)
    {
        var hasFixedAmount = range.FixedAmount.HasValue;
        var hasRangedAmount = range.RangedAmountMin.HasValue || range.RangedAmountMax.HasValue;

        if (!hasFixedAmount && !hasRangedAmount)
        {
            builder.AddError($"{prefix}.FixedAmount", "Either fixed amount or ranged amount must be provided");
        }
        else if (hasFixedAmount && hasRangedAmount)
        {
            builder.AddError($"{prefix}.FixedAmount", "Cannot specify both fixed amount and ranged amount");
        }
        else if (hasRangedAmount)
        {
            if (!range.RangedAmountMin.HasValue)
                builder.AddError($"{prefix}.RangedAmountMin", "Minimum amount is required for ranged amounts");
            if (!range.RangedAmountMax.HasValue)
                builder.AddError($"{prefix}.RangedAmountMax", "Maximum amount is required for ranged amounts");
            if (range.RangedAmountMin.HasValue && range.RangedAmountMax.HasValue && range.RangedAmountMin > range.RangedAmountMax)
                builder.AddError($"{prefix}.RangedAmountMin", "Minimum amount cannot be greater than maximum");
        }
        else if (hasFixedAmount && range.FixedAmount <= 0)
        {
            builder.AddError($"{prefix}.FixedAmount", "Fixed amount must be greater than zero");
        }

        if (range.PeriodId < 0 || range.PeriodId > 3)
            builder.AddError($"{prefix}.PeriodId", "Invalid period type");

        bool isMonthlyOrYearly = range.PeriodId is 0 or 1;
        if (isMonthlyOrYearly)
        {
            if (range.Day < 1 || range.Day > 31)
                builder.AddError($"{prefix}.Day", "Day must be between 1 and 31 for monthly/yearly periods");
        }
        else
        {
            if (range.Day < 0 || range.Day > 6)
                builder.AddError($"{prefix}.Day", "Day must be between 0 (Sunday) and 6 (Saturday) for weekly/biweekly periods");
        }
    }
}
