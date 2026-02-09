namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class SavingsRateGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.SavingsRate;

    public bool RequiresPriceDataForCalculation => true;

    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public decimal TargetPercentage { get; }

    public decimal CalculatedPercentage { get; }

    public SavingsRateGoalType(decimal targetPercentage, decimal calculatedPercentage = 0)
    {
        TargetPercentage = targetPercentage;
        CalculatedPercentage = calculatedPercentage;
    }

    public SavingsRateGoalType WithCalculatedPercentage(decimal calculatedPercentage)
    {
        return new SavingsRateGoalType(TargetPercentage, calculatedPercentage);
    }

    public bool HasSameTargetAs(IGoalType other)
    {
        return other is SavingsRateGoalType s && s.TargetPercentage == TargetPercentage;
    }

    public IGoalType WithResetProgress()
    {
        return new SavingsRateGoalType(TargetPercentage, calculatedPercentage: 0);
    }
}
