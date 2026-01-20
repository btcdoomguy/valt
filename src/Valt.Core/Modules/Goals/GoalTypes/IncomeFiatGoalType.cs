namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class IncomeFiatGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.IncomeFiat;

    public bool RequiresPriceDataForCalculation => true;

    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public decimal TargetAmount { get; }

    public decimal CalculatedIncome { get; }

    public IncomeFiatGoalType(decimal targetAmount, decimal calculatedIncome = 0)
    {
        TargetAmount = targetAmount;
        CalculatedIncome = calculatedIncome;
    }

    public IncomeFiatGoalType WithCalculatedIncome(decimal calculatedIncome)
    {
        return new IncomeFiatGoalType(TargetAmount, calculatedIncome);
    }

    public bool HasSameTargetAs(IGoalType other)
    {
        return other is IncomeFiatGoalType i && i.TargetAmount == TargetAmount;
    }

    public IGoalType WithResetProgress()
    {
        return new IncomeFiatGoalType(TargetAmount, calculatedIncome: 0);
    }
}
