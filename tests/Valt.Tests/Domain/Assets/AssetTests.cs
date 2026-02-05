using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Core.Modules.Assets.Events;
using Valt.Infra.Kernel;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Assets;

[TestFixture]
public class AssetTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    #region Creation Tests

    [Test]
    public void Should_Create_New_Asset_With_Generated_Id()
    {
        // Arrange
        var name = new AssetName("Test Stock");
        var details = new BasicAssetDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Act
        var asset = Asset.New(name, details, Icon.Empty, true, true, 0);

        // Assert
        Assert.That(asset.Id, Is.Not.Null);
        Assert.That(asset.Id.ToString(), Is.Not.Empty);
        Assert.That(asset.Name.Value, Is.EqualTo("Test Stock"));
        Assert.That(asset.Details, Is.EqualTo(details));
        Assert.That(asset.IncludeInNetWorth, Is.True);
        Assert.That(asset.Visible, Is.True);
    }

    [Test]
    public void Should_Raise_AssetCreatedEvent_On_New()
    {
        // Arrange
        var name = new AssetName("Test Asset");
        var details = new BasicAssetDetails(AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");

        // Act
        var asset = Asset.New(name, details, Icon.Empty);

        // Assert
        Assert.That(asset.Events.Count, Is.EqualTo(1));
        Assert.That(asset.Events.First(), Is.TypeOf<AssetCreatedEvent>());
        var createdEvent = (AssetCreatedEvent)asset.Events.First();
        Assert.That(createdEvent.Asset, Is.EqualTo(asset));
    }

    #endregion

    #region Price Update Tests

    [Test]
    public void Should_Update_Price_And_Raise_Event()
    {
        // Arrange
        var asset = AssetBuilder.AStockAsset(price: 100m).Build();
        asset.ClearEvents();

        // Act
        asset.UpdatePrice(150m);

        // Assert
        Assert.That(asset.GetCurrentPrice(), Is.EqualTo(150m));
        Assert.That(asset.Events.Count, Is.EqualTo(1));
        Assert.That(asset.Events.First(), Is.TypeOf<AssetPriceUpdatedEvent>());
        var priceEvent = (AssetPriceUpdatedEvent)asset.Events.First();
        Assert.That(priceEvent.OldPrice, Is.EqualTo(100m));
        Assert.That(priceEvent.NewPrice, Is.EqualTo(150m));
    }

    [Test]
    public void Should_Not_Raise_Event_When_Price_Unchanged()
    {
        // Arrange
        var asset = AssetBuilder.AStockAsset(price: 100m).Build();
        asset.ClearEvents();

        // Act
        asset.UpdatePrice(100m);

        // Assert
        Assert.That(asset.Events.Count, Is.EqualTo(0));
    }

    [Test]
    public void Should_Update_LastPriceUpdateAt_When_Price_Changes()
    {
        // Arrange
        var oldTime = DateTime.UtcNow.AddHours(-1);
        var asset = AssetBuilder.AStockAsset(price: 100m)
            .WithLastPriceUpdateAt(oldTime)
            .Build();

        // Act
        asset.UpdatePrice(150m);

        // Assert
        Assert.That(asset.LastPriceUpdateAt, Is.GreaterThan(oldTime));
    }

    #endregion

    #region Current Value Tests

    [Test]
    public void Should_Calculate_Current_Value_For_Basic_Asset()
    {
        // Arrange
        var asset = AssetBuilder.AStockAsset(price: 150m, quantity: 10).Build();

        // Act
        var value = asset.GetCurrentValue();

        // Assert
        Assert.That(value, Is.EqualTo(1500m));
    }

    [Test]
    public void Should_Calculate_Current_Value_For_RealEstate()
    {
        // Arrange
        var asset = AssetBuilder.ARealEstateAsset(value: 500000m).Build();

        // Act
        var value = asset.GetCurrentValue();

        // Assert
        Assert.That(value, Is.EqualTo(500000m));
    }

    [Test]
    public void Should_Calculate_Current_Value_For_LeveragedPosition_Long()
    {
        // Arrange - Long 10x position with 10% price increase
        var asset = AssetBuilder.ALeveragedPosition(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            currentPrice: 55000m, // 10% increase
            isLong: true).Build();

        // Act
        var value = asset.GetCurrentValue();

        // Assert - 10% * 10x = 100% gain, so value should be 2000
        Assert.That(value, Is.EqualTo(2000m));
    }

    [Test]
    public void Should_Calculate_Current_Value_For_LeveragedPosition_Short()
    {
        // Arrange - Short 10x position with 10% price decrease
        var asset = AssetBuilder.ALeveragedPosition(
            collateral: 1000m,
            entryPrice: 50000m,
            leverage: 10m,
            currentPrice: 45000m, // 10% decrease
            isLong: false).Build();

        // Act
        var value = asset.GetCurrentValue();

        // Assert - 10% decrease * 10x = 100% gain for short, so value should be 2000
        Assert.That(value, Is.EqualTo(2000m));
    }

    #endregion

    #region Edit Tests

    [Test]
    public void Should_Edit_Asset_And_Raise_Event()
    {
        // Arrange
        var asset = AssetBuilder.AStockAsset().Build();
        asset.ClearEvents();
        var newName = new AssetName("Updated Name");
        var newDetails = new BasicAssetDetails(AssetTypes.Etf, 20, "SPY", AssetPriceSource.YahooFinance, 450m, "USD");

        // Act
        asset.Edit(newName, newDetails, Icon.Empty, false, false);

        // Assert
        Assert.That(asset.Name.Value, Is.EqualTo("Updated Name"));
        Assert.That(asset.Details, Is.EqualTo(newDetails));
        Assert.That(asset.IncludeInNetWorth, Is.False);
        Assert.That(asset.Visible, Is.False);
        Assert.That(asset.Events.Count, Is.EqualTo(1));
        Assert.That(asset.Events.First(), Is.TypeOf<AssetUpdatedEvent>());
    }

    #endregion

    #region Display Order Tests

    [Test]
    public void Should_Set_Display_Order()
    {
        // Arrange
        var asset = AssetBuilder.AnAsset().WithDisplayOrder(0).Build();
        asset.ClearEvents();

        // Act
        asset.SetDisplayOrder(5);

        // Assert
        Assert.That(asset.DisplayOrder, Is.EqualTo(5));
        Assert.That(asset.Events.Count, Is.EqualTo(1));
        Assert.That(asset.Events.First(), Is.TypeOf<AssetUpdatedEvent>());
    }

    #endregion

    #region Visibility Tests

    [Test]
    public void Should_Set_Visibility()
    {
        // Arrange
        var asset = AssetBuilder.AnAsset().WithVisible(true).Build();
        asset.ClearEvents();

        // Act
        asset.SetVisibility(false);

        // Assert
        Assert.That(asset.Visible, Is.False);
        Assert.That(asset.Events.Count, Is.EqualTo(1));
        Assert.That(asset.Events.First(), Is.TypeOf<AssetUpdatedEvent>());
    }

    #endregion

    #region Include In Net Worth Tests

    [Test]
    public void Should_Set_Include_In_Net_Worth()
    {
        // Arrange
        var asset = AssetBuilder.AnAsset().WithIncludeInNetWorth(true).Build();
        asset.ClearEvents();

        // Act
        asset.SetIncludeInNetWorth(false);

        // Assert
        Assert.That(asset.IncludeInNetWorth, Is.False);
        Assert.That(asset.Events.Count, Is.EqualTo(1));
        Assert.That(asset.Events.First(), Is.TypeOf<AssetUpdatedEvent>());
    }

    #endregion

    #region Currency Code Tests

    [Test]
    public void Should_Get_Currency_Code_For_Basic_Asset()
    {
        // Arrange
        var asset = AssetBuilder.AStockAsset().Build();

        // Act
        var currency = asset.GetCurrencyCode();

        // Assert
        Assert.That(currency, Is.EqualTo("USD"));
    }

    [Test]
    public void Should_Get_Currency_Code_For_RealEstate()
    {
        // Arrange
        var asset = AssetBuilder.AnAsset()
            .WithRealEstateDetails(500000m, "BRL")
            .Build();

        // Act
        var currency = asset.GetCurrencyCode();

        // Assert
        Assert.That(currency, Is.EqualTo("BRL"));
    }

    [Test]
    public void Should_Get_Currency_Code_For_LeveragedPosition()
    {
        // Arrange
        var asset = AssetBuilder.ALeveragedPosition().Build();

        // Act
        var currency = asset.GetCurrencyCode();

        // Assert
        Assert.That(currency, Is.EqualTo("USD"));
    }

    #endregion
}
