using System.Text.Json.Serialization;
using Valt.Core.Common;

namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class StackBitcoinGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.StackBitcoin;

    [JsonPropertyName("targetSats")]
    public long TargetSats { get; }

    [JsonIgnore]
    public BtcValue TargetAmount => BtcValue.ParseSats(TargetSats);

    [JsonConstructor]
    public StackBitcoinGoalType(long targetSats)
    {
        TargetSats = targetSats;
    }

    public StackBitcoinGoalType(BtcValue targetAmount)
    {
        TargetSats = targetAmount.Sats;
    }
}
