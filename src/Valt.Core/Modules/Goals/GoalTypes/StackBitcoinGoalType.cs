using Valt.Core.Common;

namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class StackBitcoinGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.StackBitcoin;

    public long TargetSats { get; }

    public long CalculatedSats { get; }

    public BtcValue TargetAmount => BtcValue.ParseSats(TargetSats);

    public StackBitcoinGoalType(long targetSats, long calculatedSats = 0)
    {
        TargetSats = targetSats;
        CalculatedSats = calculatedSats;
    }

    public StackBitcoinGoalType(BtcValue targetAmount)
    {
        TargetSats = targetAmount.Sats;
        CalculatedSats = 0;
    }

    public StackBitcoinGoalType WithCalculatedSats(long calculatedSats)
    {
        return new StackBitcoinGoalType(TargetSats, calculatedSats);
    }
}
