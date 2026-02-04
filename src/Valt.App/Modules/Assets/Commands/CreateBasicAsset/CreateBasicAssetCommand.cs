using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.CreateBasicAsset;

/// <summary>
/// Command to create a basic asset (Stock, ETF, Crypto, Commodity, Custom).
/// </summary>
public record CreateBasicAssetCommand : ICommand<CreateBasicAssetResult>
{
    /// <summary>
    /// Asset name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Asset type: 0=Stock, 1=Etf, 2=Crypto, 3=Commodity, 6=Custom
    /// </summary>
    public required int AssetType { get; init; }

    /// <summary>
    /// Currency code (e.g., USD, BRL).
    /// </summary>
    public required string CurrencyCode { get; init; }

    /// <summary>
    /// Ticker symbol (e.g., AAPL, BTC).
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Quantity held.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Current price per unit.
    /// </summary>
    public required decimal CurrentPrice { get; init; }

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

public record CreateBasicAssetResult(string AssetId);
