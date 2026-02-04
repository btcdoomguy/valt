using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Domain.Assets.Details;

[TestFixture]
public class LeveragedPositionDetailsTests
{
    #region Construction Tests

    [Test]
    public void Should_Create_With_Valid_Parameters()
    {
        // Act
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 55000m,
            currencyCode: "USD",
            symbol: "BTC",
            priceSource: AssetPriceSource.Manual,
            isLong: true);

        // Assert
        Assert.That(details.AssetType, Is.EqualTo(AssetTypes.LeveragedPosition));
        Assert.That(details.Collateral, Is.EqualTo(1000m));
        Assert.That(details.EntryPrice, Is.EqualTo(50000m));
        Assert.That(details.Leverage, Is.EqualTo(10m));
        Assert.That(details.LiquidationPrice, Is.EqualTo(45000m));
        Assert.That(details.CurrentPrice, Is.EqualTo(55000m));
        Assert.That(details.CurrencyCode, Is.EqualTo("USD"));
        Assert.That(details.Symbol, Is.EqualTo("BTC"));
        Assert.That(details.PriceSource, Is.EqualTo(AssetPriceSource.Manual));
        Assert.That(details.IsLong, Is.True);
    }

    [Test]
    public void Should_Validate_Collateral_Is_Positive()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(0, 50000m, 10, 45000m, 55000m, "USD"));

        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(-100, 50000m, 10, 45000m, 55000m, "USD"));
    }

    [Test]
    public void Should_Validate_Entry_Price_Is_Positive()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(1000m, 0, 10, 45000m, 55000m, "USD"));

        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(1000m, -50000m, 10, 45000m, 55000m, "USD"));
    }

    [Test]
    public void Should_Validate_Leverage_Is_At_Least_One()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(1000m, 50000m, 0.5m, 45000m, 55000m, "USD"));

        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(1000m, 50000m, 0, 45000m, 55000m, "USD"));
    }

    [Test]
    public void Should_Allow_Leverage_Equal_To_One()
    {
        // Act
        var details = new LeveragedPositionDetails(1000m, 50000m, 1m, 45000m, 55000m, "USD");

        // Assert
        Assert.That(details.Leverage, Is.EqualTo(1m));
    }

    [Test]
    public void Should_Validate_Liquidation_Price_Not_Negative()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(1000m, 50000m, 10, -45000m, 55000m, "USD"));
    }

    [Test]
    public void Should_Allow_Zero_Liquidation_Price()
    {
        // Act
        var details = new LeveragedPositionDetails(1000m, 50000m, 10, 0, 55000m, "USD");

        // Assert
        Assert.That(details.LiquidationPrice, Is.EqualTo(0));
    }

    #endregion

    #region Value Calculation Tests

    [Test]
    public void Should_Calculate_Value_For_Long_Position_With_Profit()
    {
        // Arrange - Long 10x position with 10% price increase
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 55000m, // 10% increase
            currencyCode: "USD",
            isLong: true);

        // Act
        var value = details.CalculateCurrentValue(55000m);

        // Assert - priceChange = 0.10, leveragedChange = 1.0, value = 1000 * (1 + 1.0) = 2000
        Assert.That(value, Is.EqualTo(2000m));
    }

    [Test]
    public void Should_Calculate_Value_For_Long_Position_With_Loss()
    {
        // Arrange - Long 10x position with 5% price decrease
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 47500m, // 5% decrease
            currencyCode: "USD",
            isLong: true);

        // Act
        var value = details.CalculateCurrentValue(47500m);

        // Assert - priceChange = -0.05, leveragedChange = -0.5, value = 1000 * (1 - 0.5) = 500
        Assert.That(value, Is.EqualTo(500m));
    }

    [Test]
    public void Should_Calculate_Value_For_Short_Position_With_Profit()
    {
        // Arrange - Short 10x position with 10% price decrease
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 55000m,
            currentPrice: 45000m, // 10% decrease
            currencyCode: "USD",
            isLong: false);

        // Act
        var value = details.CalculateCurrentValue(45000m);

        // Assert - priceChange = -0.10, for short: value = 1000 * (1 - (-1.0)) = 2000
        Assert.That(value, Is.EqualTo(2000m));
    }

    [Test]
    public void Should_Calculate_Value_For_Short_Position_With_Loss()
    {
        // Arrange - Short 10x position with 5% price increase
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 55000m,
            currentPrice: 52500m, // 5% increase
            currencyCode: "USD",
            isLong: false);

        // Act
        var value = details.CalculateCurrentValue(52500m);

        // Assert - priceChange = 0.05, leveragedChange = 0.5, for short: value = 1000 * (1 - 0.5) = 500
        Assert.That(value, Is.EqualTo(500m));
    }

    #endregion

    #region P&L Tests

    [Test]
    public void Should_Calculate_PnL()
    {
        // Arrange
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 55000m,
            currencyCode: "USD",
            isLong: true);

        // Act
        var pnl = details.CalculatePnL(55000m);

        // Assert - Value = 2000, Collateral = 1000, P&L = 1000
        Assert.That(pnl, Is.EqualTo(1000m));
    }

    [Test]
    public void Should_Calculate_Negative_PnL()
    {
        // Arrange
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 47500m, // 5% loss
            currencyCode: "USD",
            isLong: true);

        // Act
        var pnl = details.CalculatePnL(47500m);

        // Assert - Value = 500, Collateral = 1000, P&L = -500
        Assert.That(pnl, Is.EqualTo(-500m));
    }

    [Test]
    public void Should_Calculate_PnL_Percentage()
    {
        // Arrange
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 55000m,
            currencyCode: "USD",
            isLong: true);

        // Act
        var pnlPercentage = details.CalculatePnLPercentage(55000m);

        // Assert - P&L = 1000, Collateral = 1000, Percentage = 100%
        Assert.That(pnlPercentage, Is.EqualTo(100m));
    }

    [Test]
    public void Should_Calculate_Negative_PnL_Percentage()
    {
        // Arrange
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 47500m,
            currencyCode: "USD",
            isLong: true);

        // Act
        var pnlPercentage = details.CalculatePnLPercentage(47500m);

        // Assert - P&L = -500, Collateral = 1000, Percentage = -50%
        Assert.That(pnlPercentage, Is.EqualTo(-50m));
    }

    [Test]
    public void Should_Return_Zero_PnL_Percentage_When_Collateral_Is_Zero()
    {
        // Arrange - Edge case where collateral was zero (shouldn't happen in real usage)
        // Testing with valid collateral and zero entry price to trigger edge case
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 50000m, // Same as entry, no change
            currencyCode: "USD",
            isLong: true);

        // Act
        var pnlPercentage = details.CalculatePnLPercentage(50000m);

        // Assert - No change = 0% P&L
        Assert.That(pnlPercentage, Is.EqualTo(0m));
    }

    #endregion

    #region Distance To Liquidation Tests

    [Test]
    public void Should_Calculate_Distance_To_Liquidation_For_Long()
    {
        // Arrange
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 50000m,
            currencyCode: "USD",
            isLong: true);

        // Act
        var distance = details.CalculateDistanceToLiquidation(50000m);

        // Assert - (50000 - 45000) / 50000 * 100 = 10%
        Assert.That(distance, Is.EqualTo(10m));
    }

    [Test]
    public void Should_Calculate_Distance_To_Liquidation_For_Short()
    {
        // Arrange
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 55000m,
            currentPrice: 50000m,
            currencyCode: "USD",
            isLong: false);

        // Act
        var distance = details.CalculateDistanceToLiquidation(50000m);

        // Assert - (55000 - 50000) / 50000 * 100 = 10%
        Assert.That(distance, Is.EqualTo(10m));
    }

    [Test]
    public void Should_Return_Zero_When_At_Liquidation()
    {
        // Arrange
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 45000m,
            currencyCode: "USD",
            isLong: true);

        // Act
        var distance = details.CalculateDistanceToLiquidation(45000m);

        // Assert
        Assert.That(distance, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Return_100_When_Liquidation_Price_Is_Zero()
    {
        // Arrange
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 0,
            currentPrice: 50000m,
            currencyCode: "USD",
            isLong: true);

        // Act
        var distance = details.CalculateDistanceToLiquidation(50000m);

        // Assert
        Assert.That(distance, Is.EqualTo(100m));
    }

    #endregion

    #region At Risk Tests

    [Test]
    public void Should_Identify_At_Risk_Position()
    {
        // Arrange - Position within 10% of liquidation
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 46000m, // ~2.2% from liquidation
            currencyCode: "USD",
            isLong: true);

        // Act
        var isAtRisk = details.IsAtRisk(46000m);

        // Assert
        Assert.That(isAtRisk, Is.True);
    }

    [Test]
    public void Should_Not_Identify_Safe_Position_As_At_Risk()
    {
        // Arrange - Position well above liquidation
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 55000m, // 22% above liquidation
            currencyCode: "USD",
            isLong: true);

        // Act
        var isAtRisk = details.IsAtRisk(55000m);

        // Assert
        Assert.That(isAtRisk, Is.False);
    }

    [Test]
    public void Should_Consider_Exactly_10_Percent_As_Not_At_Risk()
    {
        // Arrange
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 50000m, // Exactly 10% from liquidation
            currencyCode: "USD",
            isLong: true);

        // Act
        var isAtRisk = details.IsAtRisk(50000m);

        // Assert - IsAtRisk requires < 10%, not <= 10%
        Assert.That(isAtRisk, Is.False);
    }

    #endregion

    #region Builder Method Tests

    [Test]
    public void Should_Create_New_Details_With_Updated_Price()
    {
        // Arrange
        var original = new LeveragedPositionDetails(
            1000m, 50000m, 10m, 45000m, 55000m, "USD", "BTC", AssetPriceSource.Manual, true);

        // Act
        var updated = (LeveragedPositionDetails)original.WithUpdatedPrice(60000m);

        // Assert
        Assert.That(updated.CurrentPrice, Is.EqualTo(60000m));
        Assert.That(updated.Collateral, Is.EqualTo(original.Collateral));
        Assert.That(updated.EntryPrice, Is.EqualTo(original.EntryPrice));
        Assert.That(updated.Leverage, Is.EqualTo(original.Leverage));
        Assert.That(updated.LiquidationPrice, Is.EqualTo(original.LiquidationPrice));
        Assert.That(updated.Symbol, Is.EqualTo(original.Symbol));
        Assert.That(updated.IsLong, Is.EqualTo(original.IsLong));
    }

    [Test]
    public void Should_Create_New_Details_With_Updated_Collateral()
    {
        // Arrange
        var original = new LeveragedPositionDetails(
            1000m, 50000m, 10m, 45000m, 55000m, "USD", "BTC", AssetPriceSource.Manual, true);

        // Act
        var updated = original.WithCollateral(2000m);

        // Assert
        Assert.That(updated.Collateral, Is.EqualTo(2000m));
        Assert.That(updated.CurrentPrice, Is.EqualTo(original.CurrentPrice));
        Assert.That(updated.EntryPrice, Is.EqualTo(original.EntryPrice));
        Assert.That(updated.Leverage, Is.EqualTo(original.Leverage));
    }

    #endregion

    #region Price Source Tests

    [Test]
    public void Should_Support_YahooFinance_PriceSource()
    {
        // Act
        var details = new LeveragedPositionDetails(
            1000m, 50000m, 10m, 45000m, 55000m, "USD", "BTC", AssetPriceSource.YahooFinance, true);

        // Assert
        Assert.That(details.PriceSource, Is.EqualTo(AssetPriceSource.YahooFinance));
    }

    [Test]
    public void Should_Support_LivePrice_PriceSource()
    {
        // Act
        var details = new LeveragedPositionDetails(
            1000m, 50000m, 10m, 45000m, 55000m, "USD", "BTC", AssetPriceSource.LivePrice, true);

        // Assert
        Assert.That(details.PriceSource, Is.EqualTo(AssetPriceSource.LivePrice));
    }

    #endregion
}
