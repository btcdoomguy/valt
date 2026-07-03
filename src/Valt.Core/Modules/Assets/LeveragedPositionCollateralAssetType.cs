namespace Valt.Core.Modules.Assets;

/// <summary>
/// Determines the asset used as collateral for a leveraged position.
/// </summary>
public enum LeveragedPositionCollateralAssetType
{
    /// <summary>
    /// Collateral is a fiat amount (e.g., USD). Leverage is explicit.
    /// </summary>
    Fiat = 0,

    /// <summary>
    /// Collateral is BTC. Position is defined by contract count and contract size.
    /// Leverage is derived from notional exposure / collateral.
    /// </summary>
    Btc = 1
}
