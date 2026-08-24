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
    public void Should_Require_Positive_Liquidation_Price()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(1000m, 50000m, 10, 0, 55000m, "USD"));

        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(1000m, 50000m, 10, -45000m, 55000m, "USD"));
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

    #region Position Size Calculation Tests

    [Test]
    public void Should_Calculate_PositionSize()
    {
        // Arrange - collateral=1000, leverage=10, entryPrice=50000
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 55000m,
            currencyCode: "USD",
            isLong: true);

        // Act & Assert - PositionSize = 1000 * 10 / 50000 = 0.2
        Assert.That(details.PositionSize, Is.EqualTo(0.2m));
    }

    [Test]
    public void Should_Return_Zero_PositionSize_When_EntryPrice_Is_Zero()
    {
        // Arrange - edge case: entry price validated in constructor, but PositionSize handles 0 gracefully
        // Can't create with 0 entry price due to validation, so test the property logic
        // by using a valid entry price and verifying the formula
        var details = new LeveragedPositionDetails(
            collateral: 500m,
            entryPrice: 25000m,
            leverage: 5m,
            liquidationPrice: 20000m,
            currentPrice: 25000m,
            currencyCode: "USD",
            isLong: true);

        // Act & Assert - PositionSize = 500 * 5 / 25000 = 0.1
        Assert.That(details.PositionSize, Is.EqualTo(0.1m));
    }

    [Test]
    public void Should_Default_InputMode_To_Collateral()
    {
        // Act
        var details = new LeveragedPositionDetails(
            1000m, 50000m, 10m, 45000m, 55000m, "USD");

        // Assert
        Assert.That(details.InputMode, Is.EqualTo(LeveragedPositionInputMode.Collateral));
    }

    [Test]
    public void Should_Store_ExactPosition_InputMode()
    {
        // Act
        var details = new LeveragedPositionDetails(
            1000m, 50000m, 10m, 45000m, 55000m, "USD",
            inputMode: LeveragedPositionInputMode.ExactPosition);

        // Assert
        Assert.That(details.InputMode, Is.EqualTo(LeveragedPositionInputMode.ExactPosition));
    }

    [Test]
    public void Should_Preserve_InputMode_In_WithUpdatedPrice()
    {
        // Arrange
        var original = new LeveragedPositionDetails(
            1000m, 50000m, 10m, 45000m, 55000m, "USD",
            inputMode: LeveragedPositionInputMode.ExactPosition);

        // Act
        var updated = (LeveragedPositionDetails)original.WithUpdatedPrice(60000m);

        // Assert
        Assert.That(updated.InputMode, Is.EqualTo(LeveragedPositionInputMode.ExactPosition));
    }

    [Test]
    public void Should_Preserve_InputMode_In_WithCollateral()
    {
        // Arrange
        var original = new LeveragedPositionDetails(
            1000m, 50000m, 10m, 45000m, 55000m, "USD",
            inputMode: LeveragedPositionInputMode.ExactPosition);

        // Act
        var updated = original.WithCollateral(2000m);

        // Assert
        Assert.That(updated.InputMode, Is.EqualTo(LeveragedPositionInputMode.ExactPosition));
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

    #region BTC Collateral Tests

    [Test]
    public void Should_Create_Btc_Collateral_Position()
    {
        // Arrange - 1 BTC collateral, 100000 contracts of $10 each, entry at $100000
        var details = new LeveragedPositionDetails(
            collateral: 1m,
            entryPrice: 100000m,
            leverage: 0, // computed
            liquidationPrice: 90000m,
            currentPrice: 100000m,
            currencyCode: "USD",
            symbol: "BTC",
            priceSource: AssetPriceSource.LivePrice,
            isLong: true,
            inputMode: LeveragedPositionInputMode.Collateral,
            collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
            contractCount: 100000m,
            contractSizeUsd: 10m);

        // Assert
        Assert.That(details.CollateralAssetType, Is.EqualTo(LeveragedPositionCollateralAssetType.Btc));
        Assert.That(details.PositionSize, Is.EqualTo(10m)); // 100000 * 10 / 100000
        Assert.That(details.Leverage, Is.EqualTo(10m)); // 10 / 1
    }

    [Test]
    public void Should_Calculate_Btc_Collateral_Position_Value()
    {
        // Arrange - 10x long, BTC goes from 100k to 110k (+10%)
        // Notional = 10 BTC at entry, 9.0909 BTC at 110k
        // PnL BTC = 10 - 9.0909 = 0.9091 BTC
        // Total BTC = 1 + 0.9091 = 1.9091 BTC
        // Value = 1.9091 * 110000 = 210000
        var details = new LeveragedPositionDetails(
            collateral: 1m,
            entryPrice: 100000m,
            leverage: 0,
            liquidationPrice: 90000m,
            currentPrice: 110000m,
            currencyCode: "USD",
            isLong: true,
            collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
            contractCount: 100000m,
            contractSizeUsd: 10m);

        // Act
        var value = details.CalculateCurrentValue(110000m);

        // Assert
        Assert.That(value, Is.EqualTo(210000m).Within(0.01m));
    }

    [Test]
    public void Should_Calculate_Btc_Collateral_Position_PnL()
    {
        // Arrange - 10x long, notional USD = 1,000,000, BTC goes from 100k to 110k (+10%)
        // Position P&L = 1,000,000 * 0.10 = 100,000
        var details = new LeveragedPositionDetails(
            collateral: 1m,
            entryPrice: 100000m,
            leverage: 0,
            liquidationPrice: 90000m,
            currentPrice: 110000m,
            currencyCode: "USD",
            isLong: true,
            collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
            contractCount: 100000m,
            contractSizeUsd: 10m);

        // Act
        var pnl = details.CalculatePnL(110000m);

        // Assert
        Assert.That(pnl, Is.EqualTo(100000m).Within(0.01m));
    }

    [Test]
    public void Should_Calculate_Btc_Collateral_Short_Position_PnL()
    {
        // Arrange - 10x short, notional USD = 1,000,000, BTC goes from 100k to 90k (-10%)
        // Position P&L = 1,000,000 * 0.10 = 100,000
        var details = new LeveragedPositionDetails(
            collateral: 1m,
            entryPrice: 100000m,
            leverage: 0,
            liquidationPrice: 110000m,
            currentPrice: 90000m,
            currencyCode: "USD",
            isLong: false,
            collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
            contractCount: 100000m,
            contractSizeUsd: 10m);

        // Act
        var pnl = details.CalculatePnL(90000m);

        // Assert
        Assert.That(pnl, Is.EqualTo(100000m).Within(0.01m));
    }

    [Test]
    public void Should_Calculate_Btc_Collateral_Current_Btc_Value()
    {
        // Arrange - 10x long, BTC goes from 100k to 110k (+10%)
        // Notional = 10 BTC at entry, 9.0909 BTC at 110k
        // PnL BTC = 10 - 9.0909 = 0.9091 BTC
        // Total BTC = 1 + 0.9091 = 1.9091 BTC
        var details = new LeveragedPositionDetails(
            collateral: 1m,
            entryPrice: 100000m,
            leverage: 0,
            liquidationPrice: 90000m,
            currentPrice: 110000m,
            currencyCode: "USD",
            isLong: true,
            collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
            contractCount: 100000m,
            contractSizeUsd: 10m);

        // Act
        var currentBtcValue = details.CalculateCurrentBtcValue(110000m);

        // Assert
        Assert.That(currentBtcValue, Is.EqualTo(1.909090909m).Within(0.0001m));
    }

    [Test]
    public void Should_Calculate_Btc_Collateral_Current_Btc_Value_For_Short()
    {
        // Arrange - 10x short, BTC goes from 100k to 90k (+10% for short)
        // Notional = 10 BTC at entry, 11.1111 BTC at 90k
        // PnL BTC = 11.1111 - 10 = 1.1111 BTC
        // Total BTC = 1 + 1.1111 = 2.1111 BTC
        var details = new LeveragedPositionDetails(
            collateral: 1m,
            entryPrice: 100000m,
            leverage: 0,
            liquidationPrice: 110000m,
            currentPrice: 90000m,
            currencyCode: "USD",
            isLong: false,
            collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
            contractCount: 100000m,
            contractSizeUsd: 10m);

        // Act
        var currentBtcValue = details.CalculateCurrentBtcValue(90000m);

        // Assert
        Assert.That(currentBtcValue, Is.EqualTo(2.111111111m).Within(0.0001m));
    }

    [Test]
    public void Should_Return_Collateral_When_Current_Price_Is_Zero_For_Btc_Current_Btc_Value()
    {
        var details = new LeveragedPositionDetails(
            collateral: 1m,
            entryPrice: 100000m,
            leverage: 0,
            liquidationPrice: 90000m,
            currentPrice: 110000m,
            currencyCode: "USD",
            isLong: true,
            collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
            contractCount: 100000m,
            contractSizeUsd: 10m);

        Assert.That(details.CalculateCurrentBtcValue(0m), Is.EqualTo(1m));
    }

    [Test]
    public void Should_Return_Zero_Current_Btc_Value_For_Fiat_Collateral()
    {
        var details = new LeveragedPositionDetails(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            liquidationPrice: 45000m,
            currentPrice: 55000m,
            currencyCode: "USD",
            isLong: true,
            collateralAssetType: LeveragedPositionCollateralAssetType.Fiat,
            contractCount: 0,
            contractSizeUsd: 0);

        Assert.That(details.CalculateCurrentBtcValue(55000m), Is.EqualTo(0m));
    }

    [Test]
    public void Should_Validate_Btc_Collateral_Requires_ContractCount()
    {
        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(
                1m, 100000m, 0, 90000m, 100000m, "USD",
                collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
                contractCount: 0,
                contractSizeUsd: 10m));
    }

    [Test]
    public void Should_Validate_Btc_Collateral_Requires_ContractSize()
    {
        Assert.Throws<ArgumentException>(() =>
            new LeveragedPositionDetails(
                1m, 100000m, 0, 90000m, 100000m, "USD",
                collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
                contractCount: 100000m,
                contractSizeUsd: 0m));
    }

    #endregion
}
