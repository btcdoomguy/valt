namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class IncomeFiatGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.IncomeFiat;

    public decimal TargetAmount { get; }

    public string Currency { get; }

    public decimal CalculatedIncome { get; }

    public IncomeFiatGoalType(decimal targetAmount, string currency, decimal calculatedIncome = 0)
    {
        TargetAmount = targetAmount;
        Currency = currency;
        CalculatedIncome = calculatedIncome;
    }

    public IncomeFiatGoalType WithCalculatedIncome(decimal calculatedIncome)
    {
        return new IncomeFiatGoalType(TargetAmount, Currency, calculatedIncome);
    }
}
