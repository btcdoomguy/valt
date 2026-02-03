using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;
using Valt.Infra.Modules.Assets.Queries;
using Valt.Infra.Modules.Assets.Queries.DTOs;
using Valt.Infra.Settings;

namespace Valt.Infra.Mcp.Tools;

/// <summary>
/// MCP tools for asset management.
/// Assets are external investments tracked separately from accounts (stocks, ETFs, crypto, real estate, etc.).
/// </summary>
[McpServerToolType]
public class AssetTools
{
    /// <summary>
    /// Gets all assets.
    /// </summary>
    [McpServerTool, Description("Get all tracked assets (investments like stocks, ETFs, crypto, real estate, etc.)")]
    public static async Task<IReadOnlyList<AssetDTO>> GetAssets(
        IAssetQueries queries)
    {
        return await queries.GetAllAsync();
    }

    /// <summary>
    /// Gets visible assets only.
    /// </summary>
    [McpServerTool, Description("Get only visible assets")]
    public static async Task<IReadOnlyList<AssetDTO>> GetVisibleAssets(
        IAssetQueries queries)
    {
        return await queries.GetVisibleAsync();
    }

    /// <summary>
    /// Gets a single asset by ID.
    /// </summary>
    [McpServerTool, Description("Get a single asset by its ID")]
    public static async Task<AssetDTO?> GetAsset(
        IAssetQueries queries,
        [Description("The asset ID")] string assetId)
    {
        return await queries.GetByIdAsync(assetId);
    }

    /// <summary>
    /// Gets the asset summary (totals for net worth calculation).
    /// </summary>
    [McpServerTool, Description("Get asset summary with totals in main currency and sats")]
    public static async Task<AssetSummaryDTO> GetAssetsSummary(
        IAssetQueries queries,
        CurrencySettings currencySettings)
    {
        return await queries.GetSummaryAsync(currencySettings.MainFiatCurrency);
    }

    /// <summary>
    /// Creates a basic asset (stock, ETF, crypto, commodity, or custom).
    /// </summary>
    [McpServerTool, Description("Create a basic asset (stock, ETF, crypto, commodity, or custom)")]
    public static async Task<string> CreateBasicAsset(
        IAssetRepository repository,
        INotificationPublisher publisher,
        [Description("Asset name")] string name,
        [Description("Asset type: 0=Stock, 1=Etf, 2=Crypto, 3=Commodity, 6=Custom")] int assetType,
        [Description("Currency code (e.g., USD, BRL)")] string currencyCode,
        [Description("Ticker symbol (e.g., AAPL, BTC)")] string symbol,
        [Description("Quantity held")] decimal quantity,
        [Description("Current price per unit")] decimal currentPrice,
        [Description("Price source: 0=Manual, 1=YahooFinance, 2=LivePrice (for BTC only)")] int priceSource = 0,
        [Description("Include in net worth calculation")] bool includeInNetWorth = true,
        [Description("Visible in list")] bool visible = true,
        [Description("Icon identifier (optional)")] string? icon = null)
    {
        var assetName = new AssetName(name);
        var assetTypeEnum = (AssetTypes)assetType;
        var priceSourceEnum = (AssetPriceSource)priceSource;

        var details = new BasicAssetDetails(
            assetType: assetTypeEnum,
            quantity: quantity,
            symbol: symbol,
            priceSource: priceSourceEnum,
            currentPrice: currentPrice,
            currencyCode: currencyCode);

        var parsedIcon = string.IsNullOrWhiteSpace(icon)
            ? Icon.Empty
            : Icon.RestoreFromId(icon);

        var asset = Asset.New(
            assetName,
            details,
            parsedIcon,
            includeInNetWorth,
            visible);

        await repository.SaveAsync(asset);

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Basic asset created with ID: {asset.Id}";
    }

    /// <summary>
    /// Creates a real estate asset.
    /// </summary>
    [McpServerTool, Description("Create a real estate asset")]
    public static async Task<string> CreateRealEstateAsset(
        IAssetRepository repository,
        INotificationPublisher publisher,
        [Description("Property name")] string name,
        [Description("Currency code (e.g., USD, BRL)")] string currencyCode,
        [Description("Current market value")] decimal currentValue,
        [Description("Property address (optional)")] string? address = null,
        [Description("Monthly rental income (optional)")] decimal? monthlyRentalIncome = null,
        [Description("Include in net worth calculation")] bool includeInNetWorth = true,
        [Description("Visible in list")] bool visible = true,
        [Description("Icon identifier (optional)")] string? icon = null)
    {
        var assetName = new AssetName(name);

        var details = new RealEstateAssetDetails(
            address: address,
            currentValue: currentValue,
            currencyCode: currencyCode,
            monthlyRentalIncome: monthlyRentalIncome);

        var parsedIcon = string.IsNullOrWhiteSpace(icon)
            ? Icon.Empty
            : Icon.RestoreFromId(icon);

        var asset = Asset.New(
            assetName,
            details,
            parsedIcon,
            includeInNetWorth,
            visible);

        await repository.SaveAsync(asset);

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Real estate asset created with ID: {asset.Id}";
    }

    /// <summary>
    /// Creates a leveraged position asset.
    /// </summary>
    [McpServerTool, Description("Create a leveraged trading position (futures, perpetuals, margin)")]
    public static async Task<string> CreateLeveragedPosition(
        IAssetRepository repository,
        INotificationPublisher publisher,
        [Description("Position name")] string name,
        [Description("Currency code for collateral and P&L (e.g., USD)")] string currencyCode,
        [Description("Ticker symbol (e.g., BTC-PERP)")] string symbol,
        [Description("Collateral amount")] decimal collateral,
        [Description("Entry price")] decimal entryPrice,
        [Description("Current price")] decimal currentPrice,
        [Description("Leverage multiplier (e.g., 2, 5, 10)")] decimal leverage,
        [Description("Liquidation price")] decimal liquidationPrice,
        [Description("True for long position, false for short")] bool isLong = true,
        [Description("Price source: 0=Manual, 1=YahooFinance, 2=LivePrice (for BTC only)")] int priceSource = 0,
        [Description("Include in net worth calculation")] bool includeInNetWorth = true,
        [Description("Visible in list")] bool visible = true,
        [Description("Icon identifier (optional)")] string? icon = null)
    {
        var assetName = new AssetName(name);
        var priceSourceEnum = (AssetPriceSource)priceSource;

        var details = new LeveragedPositionDetails(
            collateral: collateral,
            entryPrice: entryPrice,
            leverage: leverage,
            liquidationPrice: liquidationPrice,
            currentPrice: currentPrice,
            currencyCode: currencyCode,
            symbol: symbol,
            priceSource: priceSourceEnum,
            isLong: isLong);

        var parsedIcon = string.IsNullOrWhiteSpace(icon)
            ? Icon.Empty
            : Icon.RestoreFromId(icon);

        var asset = Asset.New(
            assetName,
            details,
            parsedIcon,
            includeInNetWorth,
            visible);

        await repository.SaveAsync(asset);

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Leveraged position created with ID: {asset.Id}";
    }

    /// <summary>
    /// Updates the current price of an asset.
    /// </summary>
    [McpServerTool, Description("Update the current price of an asset")]
    public static async Task<string> UpdateAssetPrice(
        IAssetRepository repository,
        INotificationPublisher publisher,
        [Description("The asset ID")] string assetId,
        [Description("New current price")] decimal newPrice)
    {
        var asset = await repository.GetByIdAsync(new AssetId(assetId));
        if (asset is null)
        {
            return $"Error: Asset with ID {assetId} not found";
        }

        asset.UpdatePrice(newPrice);
        await repository.SaveAsync(asset);

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} price updated to {newPrice}";
    }

    /// <summary>
    /// Updates the quantity of a basic asset.
    /// </summary>
    [McpServerTool, Description("Update the quantity of a basic asset")]
    public static async Task<string> UpdateAssetQuantity(
        IAssetRepository repository,
        INotificationPublisher publisher,
        [Description("The asset ID")] string assetId,
        [Description("New quantity")] decimal newQuantity)
    {
        var asset = await repository.GetByIdAsync(new AssetId(assetId));
        if (asset is null)
        {
            return $"Error: Asset with ID {assetId} not found";
        }

        if (asset.Details is not BasicAssetDetails basicDetails)
        {
            return $"Error: Asset {assetId} is not a basic asset (cannot update quantity)";
        }

        // Edit the asset with updated details
        var newDetails = basicDetails.WithQuantity(newQuantity);
        asset.Edit(asset.Name, newDetails, asset.Icon, asset.IncludeInNetWorth, asset.Visible);
        await repository.SaveAsync(asset);

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} quantity updated to {newQuantity}";
    }

    /// <summary>
    /// Toggles the visibility of an asset.
    /// </summary>
    [McpServerTool, Description("Toggle the visibility of an asset")]
    public static async Task<string> ToggleAssetVisibility(
        IAssetRepository repository,
        INotificationPublisher publisher,
        [Description("The asset ID")] string assetId)
    {
        var asset = await repository.GetByIdAsync(new AssetId(assetId));
        if (asset is null)
        {
            return $"Error: Asset with ID {assetId} not found";
        }

        asset.SetVisibility(!asset.Visible);
        await repository.SaveAsync(asset);

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} visibility set to {asset.Visible}";
    }

    /// <summary>
    /// Toggles whether an asset is included in net worth.
    /// </summary>
    [McpServerTool, Description("Toggle whether an asset is included in net worth calculation")]
    public static async Task<string> ToggleAssetNetWorthInclusion(
        IAssetRepository repository,
        INotificationPublisher publisher,
        [Description("The asset ID")] string assetId)
    {
        var asset = await repository.GetByIdAsync(new AssetId(assetId));
        if (asset is null)
        {
            return $"Error: Asset with ID {assetId} not found";
        }

        asset.SetIncludeInNetWorth(!asset.IncludeInNetWorth);
        await repository.SaveAsync(asset);

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} include in net worth set to {asset.IncludeInNetWorth}";
    }

    /// <summary>
    /// Deletes an asset.
    /// </summary>
    [McpServerTool, Description("Delete an asset")]
    public static async Task<string> DeleteAsset(
        IAssetRepository repository,
        INotificationPublisher publisher,
        [Description("The asset ID to delete")] string assetId)
    {
        var asset = await repository.GetByIdAsync(new AssetId(assetId));
        if (asset is null)
        {
            return $"Error: Asset with ID {assetId} not found";
        }

        await repository.DeleteAsync(asset);

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} deleted successfully";
    }
}
