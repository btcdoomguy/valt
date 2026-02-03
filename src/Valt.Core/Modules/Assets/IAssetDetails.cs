namespace Valt.Core.Modules.Assets;

public interface IAssetDetails
{
    AssetTypes AssetType { get; }

    /// <summary>
    /// Calculates the current value of the asset in its native currency.
    /// </summary>
    decimal CalculateCurrentValue(decimal currentPrice);

    /// <summary>
    /// Creates a copy of this asset details with updated values.
    /// </summary>
    IAssetDetails WithUpdatedPrice(decimal newPrice);
}
