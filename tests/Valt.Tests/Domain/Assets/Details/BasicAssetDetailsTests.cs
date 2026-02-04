using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Domain.Assets.Details;

[TestFixture]
public class BasicAssetDetailsTests
{
    #region Construction Tests

    [Test]
    public void Should_Create_With_Valid_Parameters()
    {
        // Act
        var details = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Assert
        Assert.That(details.AssetType, Is.EqualTo(AssetTypes.Stock));
        Assert.That(details.Quantity, Is.EqualTo(10));
        Assert.That(details.Symbol, Is.EqualTo("AAPL"));
        Assert.That(details.PriceSource, Is.EqualTo(AssetPriceSource.Manual));
        Assert.That(details.CurrentPrice, Is.EqualTo(150m));
        Assert.That(details.CurrencyCode, Is.EqualTo("USD"));
    }

    [Test]
    public void Should_Throw_For_Invalid_AssetType_RealEstate()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new BasicAssetDetails(AssetTypes.RealEstate, 1, "TEST", AssetPriceSource.Manual, 100m, "USD"));
    }

    [Test]
    public void Should_Throw_For_Invalid_AssetType_LeveragedPosition()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new BasicAssetDetails(AssetTypes.LeveragedPosition, 1, "TEST", AssetPriceSource.Manual, 100m, "USD"));
    }

    [Test]
    public void Should_Allow_Stock_AssetType()
    {
        // Act & Assert - No exception
        var details = new BasicAssetDetails(AssetTypes.Stock, 1, "TEST", AssetPriceSource.Manual, 100m, "USD");
        Assert.That(details.AssetType, Is.EqualTo(AssetTypes.Stock));
    }

    [Test]
    public void Should_Allow_Etf_AssetType()
    {
        // Act & Assert - No exception
        var details = new BasicAssetDetails(AssetTypes.Etf, 1, "SPY", AssetPriceSource.Manual, 100m, "USD");
        Assert.That(details.AssetType, Is.EqualTo(AssetTypes.Etf));
    }

    [Test]
    public void Should_Allow_Crypto_AssetType()
    {
        // Act & Assert - No exception
        var details = new BasicAssetDetails(AssetTypes.Crypto, 1, "ETH", AssetPriceSource.Manual, 100m, "USD");
        Assert.That(details.AssetType, Is.EqualTo(AssetTypes.Crypto));
    }

    [Test]
    public void Should_Allow_Commodity_AssetType()
    {
        // Act & Assert - No exception
        var details = new BasicAssetDetails(AssetTypes.Commodity, 1, "GOLD", AssetPriceSource.Manual, 100m, "USD");
        Assert.That(details.AssetType, Is.EqualTo(AssetTypes.Commodity));
    }

    [Test]
    public void Should_Allow_Custom_AssetType()
    {
        // Act & Assert - No exception
        var details = new BasicAssetDetails(AssetTypes.Custom, 1, "CUSTOM", AssetPriceSource.Manual, 100m, "USD");
        Assert.That(details.AssetType, Is.EqualTo(AssetTypes.Custom));
    }

    #endregion

    #region Value Calculation Tests

    [Test]
    public void Should_Calculate_Current_Value()
    {
        // Arrange
        var details = new BasicAssetDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Act
        var value = details.CalculateCurrentValue(150m);

        // Assert - Quantity * CurrentPrice
        Assert.That(value, Is.EqualTo(1500m));
    }

    [Test]
    public void Should_Calculate_Current_Value_With_Different_Price()
    {
        // Arrange
        var details = new BasicAssetDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Act
        var value = details.CalculateCurrentValue(200m);

        // Assert - Uses provided price, not stored price
        Assert.That(value, Is.EqualTo(2000m));
    }

    [Test]
    public void Should_Calculate_Zero_Value_With_Zero_Quantity()
    {
        // Arrange
        var details = new BasicAssetDetails(AssetTypes.Stock, 0, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Act
        var value = details.CalculateCurrentValue(150m);

        // Assert
        Assert.That(value, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Calculate_Value_With_Fractional_Quantity()
    {
        // Arrange
        var details = new BasicAssetDetails(AssetTypes.Crypto, 0.5m, "BTC", AssetPriceSource.Manual, 50000m, "USD");

        // Act
        var value = details.CalculateCurrentValue(50000m);

        // Assert
        Assert.That(value, Is.EqualTo(25000m));
    }

    #endregion

    #region Builder Method Tests

    [Test]
    public void Should_Create_New_Details_With_Updated_Price()
    {
        // Arrange
        var original = new BasicAssetDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Act
        var updated = (BasicAssetDetails)original.WithUpdatedPrice(200m);

        // Assert
        Assert.That(updated.CurrentPrice, Is.EqualTo(200m));
        Assert.That(updated.Quantity, Is.EqualTo(original.Quantity));
        Assert.That(updated.Symbol, Is.EqualTo(original.Symbol));
        Assert.That(updated.AssetType, Is.EqualTo(original.AssetType));
        Assert.That(updated.PriceSource, Is.EqualTo(original.PriceSource));
        Assert.That(updated.CurrencyCode, Is.EqualTo(original.CurrencyCode));
    }

    [Test]
    public void Should_Create_New_Details_With_Updated_Quantity()
    {
        // Arrange
        var original = new BasicAssetDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Act
        var updated = original.WithQuantity(20);

        // Assert
        Assert.That(updated.Quantity, Is.EqualTo(20));
        Assert.That(updated.CurrentPrice, Is.EqualTo(original.CurrentPrice));
        Assert.That(updated.Symbol, Is.EqualTo(original.Symbol));
        Assert.That(updated.AssetType, Is.EqualTo(original.AssetType));
        Assert.That(updated.PriceSource, Is.EqualTo(original.PriceSource));
        Assert.That(updated.CurrencyCode, Is.EqualTo(original.CurrencyCode));
    }

    #endregion

    #region Price Source Tests

    [Test]
    public void Should_Support_Manual_PriceSource()
    {
        // Act
        var details = new BasicAssetDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Assert
        Assert.That(details.PriceSource, Is.EqualTo(AssetPriceSource.Manual));
    }

    [Test]
    public void Should_Support_YahooFinance_PriceSource()
    {
        // Act
        var details = new BasicAssetDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.YahooFinance, 150m, "USD");

        // Assert
        Assert.That(details.PriceSource, Is.EqualTo(AssetPriceSource.YahooFinance));
    }

    #endregion

    #region Acquisition and P&L Tests

    [Test]
    public void Should_Create_With_Acquisition_Data()
    {
        // Act
        var details = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD",
            acquisitionDate: new DateOnly(2024, 1, 15), acquisitionPrice: 120m);

        // Assert
        Assert.That(details.AcquisitionDate, Is.EqualTo(new DateOnly(2024, 1, 15)));
        Assert.That(details.AcquisitionPrice, Is.EqualTo(120m));
    }

    [Test]
    public void Should_Create_Without_Acquisition_Data()
    {
        // Act
        var details = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Assert
        Assert.That(details.AcquisitionDate, Is.Null);
        Assert.That(details.AcquisitionPrice, Is.Null);
    }

    [Test]
    public void Should_Calculate_PnL_With_Profit()
    {
        // Arrange
        var details = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD",
            acquisitionPrice: 100m);

        // Act
        var pnl = details.CalculatePnL();

        // Assert: (150 - 100) * 10 = 500
        Assert.That(pnl, Is.EqualTo(500m));
    }

    [Test]
    public void Should_Calculate_PnL_With_Loss()
    {
        // Arrange
        var details = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 80m, "USD",
            acquisitionPrice: 100m);

        // Act
        var pnl = details.CalculatePnL();

        // Assert: (80 - 100) * 10 = -200
        Assert.That(pnl, Is.EqualTo(-200m));
    }

    [Test]
    public void Should_Calculate_PnL_Zero_Without_Acquisition_Price()
    {
        // Arrange
        var details = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Act
        var pnl = details.CalculatePnL();

        // Assert
        Assert.That(pnl, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Calculate_PnL_Percentage_Positive()
    {
        // Arrange
        var details = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD",
            acquisitionPrice: 100m);

        // Act
        var pnlPct = details.CalculatePnLPercentage();

        // Assert: (150 - 100) / 100 * 100 = 50%
        Assert.That(pnlPct, Is.EqualTo(50m));
    }

    [Test]
    public void Should_Calculate_PnL_Percentage_Negative()
    {
        // Arrange
        var details = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 80m, "USD",
            acquisitionPrice: 100m);

        // Act
        var pnlPct = details.CalculatePnLPercentage();

        // Assert: (80 - 100) / 100 * 100 = -20%
        Assert.That(pnlPct, Is.EqualTo(-20m));
    }

    [Test]
    public void Should_Preserve_Acquisition_Data_On_WithUpdatedPrice()
    {
        // Arrange
        var original = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD",
            acquisitionDate: new DateOnly(2024, 1, 15), acquisitionPrice: 120m);

        // Act
        var updated = (BasicAssetDetails)original.WithUpdatedPrice(200m);

        // Assert
        Assert.That(updated.CurrentPrice, Is.EqualTo(200m));
        Assert.That(updated.AcquisitionDate, Is.EqualTo(new DateOnly(2024, 1, 15)));
        Assert.That(updated.AcquisitionPrice, Is.EqualTo(120m));
    }

    [Test]
    public void Should_Preserve_Acquisition_Data_On_WithQuantity()
    {
        // Arrange
        var original = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD",
            acquisitionDate: new DateOnly(2024, 1, 15), acquisitionPrice: 120m);

        // Act
        var updated = original.WithQuantity(20);

        // Assert
        Assert.That(updated.Quantity, Is.EqualTo(20));
        Assert.That(updated.AcquisitionDate, Is.EqualTo(new DateOnly(2024, 1, 15)));
        Assert.That(updated.AcquisitionPrice, Is.EqualTo(120m));
    }

    #endregion
}
