namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class SaveFiatGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.SaveFiat;

    public bool RequiresPriceDataForCalculation => true;

    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public decimal TargetAmount { get; }

    public decimal CalculatedSavings { get; }

    public SaveFiatGoalType(decimal targetAmount, decimal calculatedSavings = 0)
    {
        TargetAmount = targetAmount;
        CalculatedSavings = calculatedSavings;
    }

    public SaveFiatGoalType WithCalculatedSavings(decimal calculatedSavings)
    {
        return new SaveFiatGoalType(TargetAmount, calculatedSavings);
    }

    public bool HasSameTargetAs(IGoalType other)
    {
        return other is SaveFiatGoalType s && s.TargetAmount == TargetAmount;
    }

    public IGoalType WithResetProgress()
    {
        return new SaveFiatGoalType(TargetAmount, calculatedSavings: 0);
    }
}
