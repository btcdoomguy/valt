using Valt.App.Kernel.Validation;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Commands.EditGoal;

public class EditGoalValidator : IValidator<EditGoalCommand>
{
    public ValidationResult Validate(EditGoalCommand instance)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(instance.GoalId))
            errors.Add(nameof(instance.GoalId), ["Goal ID is required"]);

        // Validate period
        if (instance.Period < 0 || instance.Period > 1)
            errors.Add(nameof(instance.Period), ["Period must be 0 (Monthly) or 1 (Yearly)"]);

        // Validate goal type
        if (instance.GoalType is null)
        {
            errors.Add(nameof(instance.GoalType), ["Goal type is required"]);
        }
        else
        {
            ValidateGoalType(instance.GoalType, errors);
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    private static void ValidateGoalType(GoalTypeInputDTO goalType, Dictionary<string, string[]> errors)
    {
        switch (goalType)
        {
            case StackBitcoinGoalTypeDTO stackBitcoin:
                if (stackBitcoin.TargetSats <= 0)
                    errors.Add("GoalType.TargetSats", ["Target sats must be greater than zero"]);
                break;

            case SpendingLimitGoalTypeDTO spendingLimit:
                if (spendingLimit.TargetAmount <= 0)
                    errors.Add("GoalType.TargetAmount", ["Target amount must be greater than zero"]);
                break;

            case DcaGoalTypeDTO dca:
                if (dca.TargetPurchaseCount <= 0)
                    errors.Add("GoalType.TargetPurchaseCount", ["Target purchase count must be greater than zero"]);
                break;

            case IncomeFiatGoalTypeDTO incomeFiat:
                if (incomeFiat.TargetAmount <= 0)
                    errors.Add("GoalType.TargetAmount", ["Target amount must be greater than zero"]);
                break;

            case IncomeBtcGoalTypeDTO incomeBtc:
                if (incomeBtc.TargetSats <= 0)
                    errors.Add("GoalType.TargetSats", ["Target sats must be greater than zero"]);
                break;

            case ReduceExpenseCategoryGoalTypeDTO reduceExpense:
                if (reduceExpense.TargetAmount <= 0)
                    errors.Add("GoalType.TargetAmount", ["Target amount must be greater than zero"]);
                if (string.IsNullOrWhiteSpace(reduceExpense.CategoryId))
                    errors.Add("GoalType.CategoryId", ["Category is required"]);
                break;

            case BitcoinHodlGoalTypeDTO hodl:
                if (hodl.MaxSellableSats < 0)
                    errors.Add("GoalType.MaxSellableSats", ["Max sellable sats cannot be negative"]);
                break;

            default:
                errors.Add(nameof(EditGoalCommand.GoalType), ["Unknown goal type"]);
                break;
        }
    }
}
