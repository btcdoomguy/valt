namespace Valt.Core.Modules.Assets;

/// <summary>
/// Determines which value the user provides as input for a leveraged position.
/// </summary>
public enum LeveragedPositionInputMode
{
    /// <summary>
    /// User inputs the collateral (initial margin) amount.
    /// </summary>
    Collateral = 0,

    /// <summary>
    /// User inputs the exact position size (e.g., 10 BTC), and collateral is auto-calculated.
    /// </summary>
    ExactPosition = 1
}
