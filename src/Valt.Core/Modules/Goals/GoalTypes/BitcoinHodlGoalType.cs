namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class BitcoinHodlGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.BitcoinHodl;

    public bool RequiresPriceDataForCalculation => false;

    public ProgressionMode ProgressionMode => ProgressionMode.DecreasingSuccess;

    /// <summary>
    /// Maximum allowed sats to sell. 0 means no sales allowed (full HODL).
    /// </summary>
    public long MaxSellableSats { get; }

    /// <summary>
    /// Actual sats sold in the period.
    /// </summary>
    public long CalculatedSoldSats { get; }

    public BitcoinHodlGoalType(long maxSellableSats, long calculatedSoldSats = 0)
    {
        MaxSellableSats = maxSellableSats;
        CalculatedSoldSats = calculatedSoldSats;
    }

    public BitcoinHodlGoalType WithCalculatedSoldSats(long calculatedSoldSats)
    {
        return new BitcoinHodlGoalType(MaxSellableSats, calculatedSoldSats);
    }
}
