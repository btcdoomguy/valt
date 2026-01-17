namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class SpendingLimitGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.SpendingLimit;

    public bool RequiresPriceDataForCalculation => true;

    public decimal TargetAmount { get; }

    public decimal CalculatedSpending { get; }

    public SpendingLimitGoalType(decimal targetAmount, decimal calculatedSpending = 0)
    {
        TargetAmount = targetAmount;
        CalculatedSpending = calculatedSpending;
    }

    public SpendingLimitGoalType WithCalculatedSpending(decimal calculatedSpending)
    {
        return new SpendingLimitGoalType(TargetAmount, calculatedSpending);
    }
}
