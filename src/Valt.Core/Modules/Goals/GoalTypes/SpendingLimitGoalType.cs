namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class SpendingLimitGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.SpendingLimit;

    public decimal TargetAmount { get; }

    public string Currency { get; }

    public decimal CalculatedSpending { get; }

    public SpendingLimitGoalType(decimal targetAmount, string currency, decimal calculatedSpending = 0)
    {
        TargetAmount = targetAmount;
        Currency = currency;
        CalculatedSpending = calculatedSpending;
    }

    public SpendingLimitGoalType WithCalculatedSpending(decimal calculatedSpending)
    {
        return new SpendingLimitGoalType(TargetAmount, Currency, calculatedSpending);
    }
}
