namespace Valt.Core.Modules.Assets.Simulation;

public sealed record BtcLoanSimulationInput
{
    public required long CollateralSats { get; init; }
    public required decimal PrincipalAmount { get; init; }
    public required string CurrencyCode { get; init; }
    public required decimal Apr { get; init; }
    public required decimal LiquidationLtv { get; init; }
    public required decimal Fees { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required BtcLoanInterestMode InterestMode { get; init; }
}
