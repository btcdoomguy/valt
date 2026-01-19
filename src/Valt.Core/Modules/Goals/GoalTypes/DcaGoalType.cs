namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class DcaGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.Dca;

    public bool RequiresPriceDataForCalculation => false;

    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public int TargetPurchaseCount { get; }

    public int CalculatedPurchaseCount { get; }

    public DcaGoalType(int targetPurchaseCount, int calculatedPurchaseCount = 0)
    {
        TargetPurchaseCount = targetPurchaseCount;
        CalculatedPurchaseCount = calculatedPurchaseCount;
    }

    public DcaGoalType WithCalculatedPurchaseCount(int calculatedPurchaseCount)
    {
        return new DcaGoalType(TargetPurchaseCount, calculatedPurchaseCount);
    }

    public bool HasSameTargetAs(IGoalType other)
    {
        return other is DcaGoalType d && d.TargetPurchaseCount == TargetPurchaseCount;
    }

    public IGoalType WithResetProgress()
    {
        return new DcaGoalType(TargetPurchaseCount, calculatedPurchaseCount: 0);
    }
}
