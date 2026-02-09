namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class NetWorthBtcGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.NetWorthBtc;

    public bool RequiresPriceDataForCalculation => true;

    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public long TargetSats { get; }

    public long CalculatedSats { get; }

    public NetWorthBtcGoalType(long targetSats, long calculatedSats = 0)
    {
        TargetSats = targetSats;
        CalculatedSats = calculatedSats;
    }

    public NetWorthBtcGoalType WithCalculatedSats(long calculatedSats)
    {
        return new NetWorthBtcGoalType(TargetSats, calculatedSats);
    }

    public bool HasSameTargetAs(IGoalType other)
    {
        return other is NetWorthBtcGoalType n && n.TargetSats == TargetSats;
    }

    public IGoalType WithResetProgress()
    {
        return new NetWorthBtcGoalType(TargetSats, calculatedSats: 0);
    }
}
