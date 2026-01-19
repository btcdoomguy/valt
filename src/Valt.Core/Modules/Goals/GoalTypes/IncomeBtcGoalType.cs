using Valt.Core.Common;

namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class IncomeBtcGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.IncomeBtc;

    public bool RequiresPriceDataForCalculation => false;

    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public long TargetSats { get; }

    public long CalculatedSats { get; }

    public BtcValue TargetAmount => BtcValue.ParseSats(TargetSats);

    public IncomeBtcGoalType(long targetSats, long calculatedSats = 0)
    {
        TargetSats = targetSats;
        CalculatedSats = calculatedSats;
    }

    public IncomeBtcGoalType(BtcValue targetAmount)
    {
        TargetSats = targetAmount.Sats;
        CalculatedSats = 0;
    }

    public IncomeBtcGoalType WithCalculatedSats(long calculatedSats)
    {
        return new IncomeBtcGoalType(TargetSats, calculatedSats);
    }

    public bool HasSameTargetAs(IGoalType other)
    {
        return other is IncomeBtcGoalType i && i.TargetSats == TargetSats;
    }

    public IGoalType WithResetProgress()
    {
        return new IncomeBtcGoalType(TargetSats, calculatedSats: 0);
    }
}
