namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class DcaGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.Dca;

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
}
