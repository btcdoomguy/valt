using Valt.App.Kernel.Validation;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Validation;

public static class GoalValidationRules
{
    public static void ValidateCommonFields(int period, DateOnly? startDate, DateOnly refDate, ValidationResultBuilder builder)
    {
        if (period < 0 || period > 1)
            builder.AddError(nameof(period), "Period must be 0 (Monthly) or 1 (Yearly)");

        if (period == 1 && startDate.HasValue && startDate.Value.Year != refDate.Year)
            builder.AddError(nameof(startDate), "Start date must be in the same year as the reference year");
    }

    public static void ValidateGoalType(GoalTypeInputDTO goalType, ValidationResultBuilder builder)
    {
        switch (goalType)
        {
            case StackBitcoinGoalTypeDTO stackBitcoin:
                if (stackBitcoin.TargetSats <= 0)
                    builder.AddError("GoalType.TargetSats", "Target sats must be greater than zero");
                break;

            case SpendingLimitGoalTypeDTO spendingLimit:
                if (spendingLimit.TargetAmount <= 0)
                    builder.AddError("GoalType.TargetAmount", "Target amount must be greater than zero");
                break;

            case DcaGoalTypeDTO dca:
                if (dca.TargetPurchaseCount <= 0)
                    builder.AddError("GoalType.TargetPurchaseCount", "Target purchase count must be greater than zero");
                break;

            case IncomeFiatGoalTypeDTO incomeFiat:
                if (incomeFiat.TargetAmount <= 0)
                    builder.AddError("GoalType.TargetAmount", "Target amount must be greater than zero");
                break;

            case IncomeBtcGoalTypeDTO incomeBtc:
                if (incomeBtc.TargetSats <= 0)
                    builder.AddError("GoalType.TargetSats", "Target sats must be greater than zero");
                break;

            case ReduceExpenseCategoryGoalTypeDTO reduceExpense:
                if (reduceExpense.TargetAmount <= 0)
                    builder.AddError("GoalType.TargetAmount", "Target amount must be greater than zero");
                if (string.IsNullOrWhiteSpace(reduceExpense.CategoryId))
                    builder.AddError("GoalType.CategoryId", "Category is required");
                break;

            case BitcoinHodlGoalTypeDTO hodl:
                if (hodl.MaxSellableSats < 0)
                    builder.AddError("GoalType.MaxSellableSats", "Max sellable sats cannot be negative");
                break;

            case SaveFiatGoalTypeDTO saveFiat:
                if (saveFiat.TargetAmount <= 0)
                    builder.AddError("GoalType.TargetAmount", "Target amount must be greater than zero");
                break;

            case SavingsRateGoalTypeDTO savingsRate:
                if (savingsRate.TargetPercentage <= 0 || savingsRate.TargetPercentage > 100)
                    builder.AddError("GoalType.TargetPercentage", "Target percentage must be between 1 and 100");
                break;

            case NetWorthBtcGoalTypeDTO netWorthBtc:
                if (netWorthBtc.TargetSats <= 0)
                    builder.AddError("GoalType.TargetSats", "Target sats must be greater than zero");
                break;

            default:
                builder.AddError("GoalType", "Unknown goal type");
                break;
        }
    }
}
