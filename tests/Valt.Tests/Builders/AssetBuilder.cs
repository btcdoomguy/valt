using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Builders;

/// <summary>
/// Builder for creating Asset instances for testing.
/// </summary>
public class AssetBuilder
{
    private AssetId _id = new();
    private AssetName _name = new("Test Asset");
    private IAssetDetails _details = new BasicAssetDetails(
        AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 100m, "USD");
    private Icon _icon = Icon.Empty;
    private bool _includeInNetWorth = true;
    private bool _visible = true;
    private DateTime _lastPriceUpdateAt = DateTime.UtcNow;
    private DateTime _createdAt = DateTime.UtcNow;
    private int _displayOrder = 0;
    private int _version = 1;

    public AssetBuilder WithId(AssetId id)
    {
        _id = id;
        return this;
    }

    public AssetBuilder WithName(string name)
    {
        _name = new AssetName(name);
        return this;
    }

    public AssetBuilder WithDetails(IAssetDetails details)
    {
        _details = details;
        return this;
    }

    public AssetBuilder WithBasicDetails(
        AssetTypes assetType = AssetTypes.Stock,
        decimal quantity = 10,
        string symbol = "AAPL",
        AssetPriceSource priceSource = AssetPriceSource.Manual,
        decimal currentPrice = 100m,
        string currencyCode = "USD")
    {
        _details = new BasicAssetDetails(assetType, quantity, symbol, priceSource, currentPrice, currencyCode);
        return this;
    }

    public AssetBuilder WithLeveragedDetails(
        decimal collateral = 1000m,
        decimal entryPrice = 50000m,
        decimal leverage = 10m,
        decimal liquidationPrice = 45000m,
        decimal currentPrice = 55000m,
        string currencyCode = "USD",
        string? symbol = "BTC",
        AssetPriceSource priceSource = AssetPriceSource.Manual,
        bool isLong = true)
    {
        _details = new LeveragedPositionDetails(
            collateral, entryPrice, leverage, liquidationPrice, currentPrice, currencyCode, symbol, priceSource, isLong);
        return this;
    }

    public AssetBuilder WithRealEstateDetails(
        decimal currentValue = 500000m,
        string currencyCode = "USD",
        string? address = null,
        decimal? monthlyRentalIncome = null)
    {
        _details = new RealEstateAssetDetails(currentValue, currencyCode, address, monthlyRentalIncome);
        return this;
    }

    public AssetBuilder WithIcon(Icon icon)
    {
        _icon = icon;
        return this;
    }

    public AssetBuilder WithIncludeInNetWorth(bool include)
    {
        _includeInNetWorth = include;
        return this;
    }

    public AssetBuilder WithVisible(bool visible)
    {
        _visible = visible;
        return this;
    }

    public AssetBuilder WithLastPriceUpdateAt(DateTime lastPriceUpdateAt)
    {
        _lastPriceUpdateAt = lastPriceUpdateAt;
        return this;
    }

    public AssetBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public AssetBuilder WithDisplayOrder(int displayOrder)
    {
        _displayOrder = displayOrder;
        return this;
    }

    public AssetBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public Asset Build()
    {
        return Asset.Create(_id, _name, _details, _icon, _includeInNetWorth, _visible,
            _lastPriceUpdateAt, _createdAt, _displayOrder, _version);
    }

    // Static factory methods
    public static AssetBuilder AnAsset() => new();

    public static AssetBuilder AStockAsset(string symbol = "AAPL", decimal price = 150m, decimal quantity = 10) =>
        new AssetBuilder()
            .WithName($"{symbol} Stock")
            .WithBasicDetails(AssetTypes.Stock, quantity, symbol, AssetPriceSource.Manual, price, "USD");

    public static AssetBuilder AnEtfAsset(string symbol = "SPY", decimal price = 450m, decimal quantity = 5) =>
        new AssetBuilder()
            .WithName($"{symbol} ETF")
            .WithBasicDetails(AssetTypes.Etf, quantity, symbol, AssetPriceSource.Manual, price, "USD");

    public static AssetBuilder ACryptoAsset(string symbol = "ETH", decimal price = 2500m, decimal quantity = 2) =>
        new AssetBuilder()
            .WithName($"{symbol} Crypto")
            .WithBasicDetails(AssetTypes.Crypto, quantity, symbol, AssetPriceSource.Manual, price, "USD");

    public static AssetBuilder ARealEstateAsset(decimal value = 500000m, string? address = "123 Main St") =>
        new AssetBuilder()
            .WithName("Property")
            .WithRealEstateDetails(value, "USD", address, null);

    public static AssetBuilder ALeveragedPosition(
        decimal collateral = 1000m,
        decimal entryPrice = 50000m,
        decimal leverage = 10m,
        decimal currentPrice = 55000m,
        bool isLong = true) =>
        new AssetBuilder()
            .WithName("BTC Long 10x")
            .WithLeveragedDetails(
                collateral, entryPrice, leverage,
                liquidationPrice: isLong ? 45000m : 55000m,
                currentPrice, "USD", "BTC", AssetPriceSource.Manual, isLong);

    public static AssetBuilder ABitcoinLeveragedPosition(
        decimal collateral = 1000m,
        decimal entryPrice = 50000m,
        decimal leverage = 10m,
        decimal currentPrice = 55000m,
        bool isLong = true) =>
        new AssetBuilder()
            .WithName("BTC Long 10x")
            .WithLeveragedDetails(
                collateral, entryPrice, leverage,
                liquidationPrice: isLong ? 45000m : 55000m,
                currentPrice, "USD", "BTC", AssetPriceSource.LivePrice, isLong);
}
