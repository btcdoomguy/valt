using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.CreateLeveragedPosition;

/// <summary>
/// Command to create a leveraged position asset (futures, perpetuals, margin).
/// </summary>
public record CreateLeveragedPositionCommand : ICommand<CreateLeveragedPositionResult>
{
    /// <summary>
    /// Position name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Currency code for collateral and P&amp;L (e.g., USD).
    /// </summary>
    public required string CurrencyCode { get; init; }

    /// <summary>
    /// Ticker symbol (e.g., BTC-PERP).
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Collateral amount.
    /// </summary>
    public required decimal Collateral { get; init; }

    /// <summary>
    /// Entry price.
    /// </summary>
    public required decimal EntryPrice { get; init; }

    /// <summary>
    /// Current price.
    /// </summary>
    public required decimal CurrentPrice { get; init; }

    /// <summary>
    /// Leverage multiplier (e.g., 2, 5, 10).
    /// </summary>
    public required decimal Leverage { get; init; }

    /// <summary>
    /// Liquidation price.
    /// </summary>
    public required decimal LiquidationPrice { get; init; }

    /// <summary>
    /// True for long position, false for short.
    /// </summary>
    public bool IsLong { get; init; } = true;

    /// <summary>
    /// Price source: 0=Manual, 1=YahooFinance, 2=LivePrice
    /// </summary>
    public int PriceSource { get; init; } = 0;

    /// <summary>
    /// Include in net worth calculation.
    /// </summary>
    public bool IncludeInNetWorth { get; init; } = true;

    /// <summary>
    /// Visible in list.
    /// </summary>
    public bool Visible { get; init; } = true;

    /// <summary>
    /// Icon identifier (optional).
    /// </summary>
    public string? Icon { get; init; }
}

public record CreateLeveragedPositionResult(string AssetId);
