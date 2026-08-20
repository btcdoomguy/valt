using System;
using Valt.Core.Common;
using Valt.Core.Modules.Assets.Simulation;

namespace Valt.UI.Views.Main.Modals.BtcLoanSimulator;

public class BtcLoanSimulationItem
{
    public string DisplayName { get; init; } = string.Empty;
    public string? AssetId { get; init; }
    public bool IsNewSimulation { get; init; }

    // Phase 47 prefill fields (unused in Phase 45)
    public long CollateralSats { get; init; }
    public decimal PrincipalAmount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal Apr { get; init; }
    public decimal LiquidationLtv { get; init; }
    public decimal Fees { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public BtcLoanInterestMode InterestMode { get; init; }

    public override string ToString() => DisplayName;
}
