namespace Valt.Core.Modules.Assets.Simulation;

public sealed record BtcLoanSimulationResult
{
    public required decimal TotalRepay { get; init; }
    public required decimal Principal { get; init; }
    public required decimal Interest { get; init; }
    public required decimal Fees { get; init; }
    public required decimal LiquidationPrice { get; init; }
    public required decimal EffectiveApr { get; init; }
    public required IReadOnlyList<LoanScheduleEntry> Schedule { get; init; }
}
