using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.CreateBasicAsset;
using Valt.App.Modules.Assets.Commands.CreateBtcLending;
using Valt.App.Modules.Assets.Commands.CreateBtcLoan;
using Valt.App.Modules.Assets.Commands.CreateLeveragedPosition;
using Valt.App.Modules.Assets.Commands.CreateRealEstateAsset;
using Valt.App.Modules.Assets.Commands.DeleteAsset;
using Valt.App.Modules.Assets.Commands.RepayLoan;
using Valt.App.Modules.Assets.Commands.SetAssetIncludeInNetWorth;
using Valt.App.Modules.Assets.Commands.SetAssetVisibility;
using Valt.App.Modules.Assets.Commands.UpdateAssetPrice;
using Valt.App.Modules.Assets.Commands.UpdateAssetQuantity;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetAsset;
using Valt.App.Modules.Assets.Queries.GetAssets;
using Valt.App.Modules.Assets.Queries.GetAssetSummary;
using Valt.App.Modules.Assets.Queries.GetVisibleAssets;
using Valt.Core.Modules.Assets;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;
using Valt.Infra.Modules.Assets.PriceProviders;
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
        IQueryDispatcher queryDispatcher)
    {
        return await queryDispatcher.DispatchAsync(new GetAssetsQuery());
    }

    /// <summary>
    /// Gets visible assets only.
    /// </summary>
    [McpServerTool, Description("Get only visible assets")]
    public static async Task<IReadOnlyList<AssetDTO>> GetVisibleAssets(
        IQueryDispatcher queryDispatcher)
    {
        return await queryDispatcher.DispatchAsync(new GetVisibleAssetsQuery());
    }

    /// <summary>
    /// Gets a single asset by ID.
    /// </summary>
    [McpServerTool, Description("Get a single asset by its ID")]
    public static async Task<AssetDTO?> GetAsset(
        IQueryDispatcher queryDispatcher,
        [Description("The asset ID")] string assetId)
    {
        return await queryDispatcher.DispatchAsync(new GetAssetQuery { AssetId = assetId });
    }

    /// <summary>
    /// Gets the asset summary (totals for net worth calculation).
    /// </summary>
    [McpServerTool, Description("Get asset summary with totals in main currency and sats, including assets vs liabilities breakdown")]
    public static async Task<AssetSummaryDTO> GetAssetsSummary(
        IQueryDispatcher queryDispatcher,
        CurrencySettings currencySettings)
    {
        return await queryDispatcher.DispatchAsync(new GetAssetSummaryQuery
        {
            MainCurrencyCode = currencySettings.MainFiatCurrency
        });
    }

    /// <summary>
    /// Creates a basic asset (stock, ETF, crypto, commodity, or custom).
    /// </summary>
    [McpServerTool, Description("Create a basic asset (stock, ETF, crypto, commodity, or custom)")]
    public static async Task<string> CreateBasicAsset(
        ICommandDispatcher commandDispatcher,
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
        var result = await commandDispatcher.DispatchAsync(new CreateBasicAssetCommand
        {
            Name = name,
            AssetType = assetType,
            CurrencyCode = currencyCode,
            Symbol = symbol,
            Quantity = quantity,
            CurrentPrice = currentPrice,
            PriceSource = priceSource,
            IncludeInNetWorth = includeInNetWorth,
            Visible = visible,
            Icon = icon
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Basic asset created with ID: {result.Value}";
    }

    /// <summary>
    /// Creates a real estate asset.
    /// </summary>
    [McpServerTool, Description("Create a real estate asset")]
    public static async Task<string> CreateRealEstateAsset(
        ICommandDispatcher commandDispatcher,
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
        var result = await commandDispatcher.DispatchAsync(new CreateRealEstateAssetCommand
        {
            Name = name,
            CurrencyCode = currencyCode,
            CurrentValue = currentValue,
            Address = address,
            MonthlyRentalIncome = monthlyRentalIncome,
            IncludeInNetWorth = includeInNetWorth,
            Visible = visible,
            Icon = icon
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Real estate asset created with ID: {result.Value}";
    }

    /// <summary>
    /// Creates a leveraged position asset.
    /// </summary>
    [McpServerTool, Description("Create a leveraged trading position (futures, perpetuals, margin)")]
    public static async Task<string> CreateLeveragedPosition(
        ICommandDispatcher commandDispatcher,
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
        [Description("Icon identifier (optional)")] string? icon = null,
        [Description("Input mode: 0=Collateral (default), 1=ExactPosition (specify positionSize instead)")] int inputMode = 0,
        [Description("Position size in underlying units (e.g., 0.2 BTC). Only used when inputMode=1")] decimal? positionSize = null)
    {
        var result = await commandDispatcher.DispatchAsync(new CreateLeveragedPositionCommand
        {
            Name = name,
            CurrencyCode = currencyCode,
            Symbol = symbol,
            Collateral = collateral,
            EntryPrice = entryPrice,
            CurrentPrice = currentPrice,
            Leverage = leverage,
            LiquidationPrice = liquidationPrice,
            IsLong = isLong,
            PriceSource = priceSource,
            IncludeInNetWorth = includeInNetWorth,
            Visible = visible,
            Icon = icon,
            InputMode = inputMode,
            PositionSize = positionSize
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Leveraged position created with ID: {result.Value}";
    }

    /// <summary>
    /// Updates the current price of an asset.
    /// </summary>
    [McpServerTool, Description("Update the current price of an asset")]
    public static async Task<string> UpdateAssetPrice(
        ICommandDispatcher commandDispatcher,
        INotificationPublisher publisher,
        [Description("The asset ID")] string assetId,
        [Description("New current price")] decimal newPrice)
    {
        var result = await commandDispatcher.DispatchAsync(new UpdateAssetPriceCommand
        {
            AssetId = assetId,
            NewPrice = newPrice
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} price updated to {newPrice}";
    }

    /// <summary>
    /// Updates the quantity of a basic asset.
    /// </summary>
    [McpServerTool, Description("Update the quantity of a basic asset")]
    public static async Task<string> UpdateAssetQuantity(
        ICommandDispatcher commandDispatcher,
        INotificationPublisher publisher,
        [Description("The asset ID")] string assetId,
        [Description("New quantity")] decimal newQuantity)
    {
        var result = await commandDispatcher.DispatchAsync(new UpdateAssetQuantityCommand
        {
            AssetId = assetId,
            NewQuantity = newQuantity
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} quantity updated to {newQuantity}";
    }

    /// <summary>
    /// Toggles the visibility of an asset.
    /// </summary>
    [McpServerTool, Description("Toggle the visibility of an asset")]
    public static async Task<string> ToggleAssetVisibility(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        INotificationPublisher publisher,
        [Description("The asset ID")] string assetId)
    {
        var asset = await queryDispatcher.DispatchAsync(new GetAssetQuery { AssetId = assetId });
        if (asset is null)
        {
            return $"Error: Asset with ID {assetId} not found";
        }

        var result = await commandDispatcher.DispatchAsync(new SetAssetVisibilityCommand
        {
            AssetId = assetId,
            Visible = !asset.Visible
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} visibility set to {!asset.Visible}";
    }

    /// <summary>
    /// Toggles whether an asset is included in net worth.
    /// </summary>
    [McpServerTool, Description("Toggle whether an asset is included in net worth calculation")]
    public static async Task<string> ToggleAssetNetWorthInclusion(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        INotificationPublisher publisher,
        [Description("The asset ID")] string assetId)
    {
        var asset = await queryDispatcher.DispatchAsync(new GetAssetQuery { AssetId = assetId });
        if (asset is null)
        {
            return $"Error: Asset with ID {assetId} not found";
        }

        var result = await commandDispatcher.DispatchAsync(new SetAssetIncludeInNetWorthCommand
        {
            AssetId = assetId,
            IncludeInNetWorth = !asset.IncludeInNetWorth
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} include in net worth set to {!asset.IncludeInNetWorth}";
    }

    /// <summary>
    /// Creates a BTC-collateralized loan asset.
    /// </summary>
    [McpServerTool, Description("Create a BTC-collateralized loan (borrowing fiat against BTC collateral). Tracks LTV, health status, and accrued interest.")]
    public static async Task<string> CreateBtcLoan(
        ICommandDispatcher commandDispatcher,
        INotificationPublisher publisher,
        IAssetPriceProviderSelector priceProviderSelector,
        [Description("Loan name")] string name,
        [Description("Currency code for the loan (e.g., USD, BRL)")] string currencyCode,
        [Description("Lending platform name (e.g., HodlHodl, Ledn)")] string platformName,
        [Description("BTC collateral in satoshis")] long collateralSats,
        [Description("Borrowed fiat amount")] decimal loanAmount,
        [Description("Annual percentage rate as decimal (e.g., 0.12 for 12%)")] decimal apr,
        [Description("Initial LTV ratio at loan origination (percentage, e.g., 50)")] decimal initialLtv,
        [Description("LTV ratio that triggers liquidation (percentage, e.g., 80)")] decimal liquidationLtv,
        [Description("LTV ratio that triggers margin call (percentage, e.g., 70)")] decimal marginCallLtv,
        [Description("Loan start date (yyyy-MM-dd)")] string loanStartDate,
        [Description("Fees paid for the loan")] decimal fees = 0,
        [Description("Repayment date (yyyy-MM-dd, optional)")] string? repaymentDate = null,
        [Description("Include in net worth calculation")] bool includeInNetWorth = true,
        [Description("Visible in list")] bool visible = true,
        [Description("Icon identifier (optional)")] string? icon = null)
    {
        // Fetch current BTC price for LTV calculations
        decimal currentBtcPrice = 0;
        var priceResult = await priceProviderSelector.GetPriceAsync(AssetPriceSource.LivePrice, "BTC", currencyCode);
        if (priceResult is not null)
            currentBtcPrice = priceResult.Price;

        var result = await commandDispatcher.DispatchAsync(new CreateBtcLoanCommand
        {
            Name = name,
            CurrencyCode = currencyCode,
            PlatformName = platformName,
            CollateralSats = collateralSats,
            LoanAmount = loanAmount,
            Apr = apr,
            InitialLtv = initialLtv,
            LiquidationLtv = liquidationLtv,
            MarginCallLtv = marginCallLtv,
            Fees = fees,
            LoanStartDate = DateOnly.Parse(loanStartDate),
            RepaymentDate = string.IsNullOrWhiteSpace(repaymentDate) ? null : DateOnly.Parse(repaymentDate),
            CurrentBtcPrice = currentBtcPrice,
            IncludeInNetWorth = includeInNetWorth,
            Visible = visible,
            Icon = icon
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"BTC loan created with ID: {result.Value}";
    }

    /// <summary>
    /// Creates a BTC/fiat lending position asset.
    /// </summary>
    [McpServerTool, Description("Create a lending position (lending fiat/BTC to a borrower or platform). Tracks earned interest.")]
    public static async Task<string> CreateBtcLending(
        ICommandDispatcher commandDispatcher,
        INotificationPublisher publisher,
        [Description("Lending position name")] string name,
        [Description("Currency code (e.g., USD, BRL)")] string currencyCode,
        [Description("Amount lent")] decimal amountLent,
        [Description("Annual percentage rate earned as decimal (e.g., 0.05 for 5%)")] decimal apr,
        [Description("Borrower or platform name")] string borrowerOrPlatformName,
        [Description("Lending start date (yyyy-MM-dd)")] string lendingStartDate,
        [Description("Expected repayment date (yyyy-MM-dd, optional)")] string? expectedRepaymentDate = null,
        [Description("Include in net worth calculation")] bool includeInNetWorth = true,
        [Description("Visible in list")] bool visible = true,
        [Description("Icon identifier (optional)")] string? icon = null)
    {
        var result = await commandDispatcher.DispatchAsync(new CreateBtcLendingCommand
        {
            Name = name,
            CurrencyCode = currencyCode,
            AmountLent = amountLent,
            Apr = apr,
            BorrowerOrPlatformName = borrowerOrPlatformName,
            LendingStartDate = DateOnly.Parse(lendingStartDate),
            ExpectedRepaymentDate = string.IsNullOrWhiteSpace(expectedRepaymentDate) ? null : DateOnly.Parse(expectedRepaymentDate),
            IncludeInNetWorth = includeInNetWorth,
            Visible = visible,
            Icon = icon
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Lending position created with ID: {result.Value}";
    }

    /// <summary>
    /// Marks a BTC loan or lending position as repaid.
    /// </summary>
    [McpServerTool, Description("Mark a BTC loan or lending position as repaid")]
    public static async Task<string> RepayLoan(
        ICommandDispatcher commandDispatcher,
        INotificationPublisher publisher,
        [Description("The asset ID to mark as repaid")] string assetId)
    {
        var result = await commandDispatcher.DispatchAsync(new RepayLoanCommand
        {
            AssetId = assetId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} marked as repaid";
    }

    /// <summary>
    /// Deletes an asset.
    /// </summary>
    [McpServerTool, Description("Delete an asset")]
    public static async Task<string> DeleteAsset(
        ICommandDispatcher commandDispatcher,
        INotificationPublisher publisher,
        [Description("The asset ID to delete")] string assetId)
    {
        var result = await commandDispatcher.DispatchAsync(new DeleteAssetCommand
        {
            AssetId = assetId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error!.Message}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Asset {assetId} deleted successfully";
    }
}
