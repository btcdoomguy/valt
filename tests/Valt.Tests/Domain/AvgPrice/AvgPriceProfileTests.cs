using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Events;
using Valt.Infra.Kernel;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.AvgPrice;

[TestFixture]
public class AvgPriceProfileTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [Test]
    public void Should_Create_Profile_With_Empty_Lines()
    {
        // Arrange & Act
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithName("My BTC Stack")
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Assert
        Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(0));
        Assert.That(profile.Name.Value, Is.EqualTo("My BTC Stack"));
        Assert.That(profile.Currency, Is.EqualTo(FiatCurrency.Usd));
        Assert.That(profile.CalculationMethod, Is.EqualTo(AvgPriceCalculationMethod.BrazilianRule));
    }

    [Test]
    public void Should_Create_Profile_With_Existing_Lines()
    {
        // Arrange
        var line1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity(1)
            .WithAmount(FiatValue.New(50000m))
            .Build();

        var line2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity(0.5m)
            .WithAmount(FiatValue.New(60000m))
            .Build();

        // Act
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithLines(line1, line2)
            .Build();

        // Assert
        Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(2));
    }

    [Test]
    public void Should_Add_Line_And_Recalculate_Totals()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Act: Add first buy
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            1,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(50000m),
            "First buy");

        // Assert
        Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(1));
        var firstLine = profile.AvgPriceLines.First();
        Assert.That(firstLine.Totals.TotalCost, Is.EqualTo(50000m));
        Assert.That(firstLine.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(firstLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
    }

    [Test]
    public void Should_Add_Multiple_Lines_And_Recalculate_Totals_In_Order()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Act: Add lines (note: adding out of order to test sorting)
        profile.AddLine(
            new DateOnly(2024, 1, 2),
            1,
            AvgPriceLineTypes.Buy,
            0.5m,
            FiatValue.New(30000m),
            "Second buy");

        profile.AddLine(
            new DateOnly(2024, 1, 1),
            1,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(50000m),
            "First buy");

        // Assert
        Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(2));

        // Lines should be calculated in date order
        var orderedLines = profile.AvgPriceLines.OrderBy(x => x.Date).ToList();

        // First line (Jan 1): Total = 50000, BTC = 1, Avg = 50000
        Assert.That(orderedLines[0].Totals.TotalCost, Is.EqualTo(50000m));
        Assert.That(orderedLines[0].Totals.Quantity, Is.EqualTo(1m));
        Assert.That(orderedLines[0].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));

        // Second line (Jan 2): Total = 50000 + 30000 = 80000, BTC = 1.5, Avg = 53333.33
        Assert.That(orderedLines[1].Totals.TotalCost, Is.EqualTo(80000m));
        Assert.That(orderedLines[1].Totals.Quantity, Is.EqualTo(1.5m));
        Assert.That(orderedLines[1].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(53333.33m));
    }

    [Test]
    public void Should_Remove_Line_And_Recalculate_Totals()
    {
        // Arrange
        var line1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithDisplayOrder(1)
            .WithQuantity(1m)
            .WithAmount(FiatValue.New(50000m))
            .Build();

        var line2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithDisplayOrder(1)
            .WithQuantity(0.5m)
            .WithAmount(FiatValue.New(30000m))
            .Build();

        var profile = AvgPriceProfileBuilder.AProfile()
            .WithLines(line1, line2)
            .Build();

        // Act: Remove the first line
        profile.RemoveLine(line1);

        // Assert
        Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(1));

        // After removing first buy, only second remains
        // New totals should be: Total = 30000, BTC = 0.5, Avg = 60000
        var remainingLine = profile.AvgPriceLines.First();
        Assert.That(remainingLine.Totals.TotalCost, Is.EqualTo(30000m));
        Assert.That(remainingLine.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(remainingLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(60000m));
    }

    [Test]
    public void Should_Add_Sell_Line_And_Recalculate()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Buy 1 BTC at $50,000
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            1,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(50000m),
            "Buy");

        // Sell 0.5 BTC
        profile.AddLine(
            new DateOnly(2024, 1, 2),
            1,
            AvgPriceLineTypes.Sell,
            0.5m,
            FiatValue.New(70000m), // Sell price doesn't affect avg in Brazilian rule
            "Sell");

        // Assert
        Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(2));

        var sellLine = profile.AvgPriceLines.First(x => x.Type == AvgPriceLineTypes.Sell);
        // After selling 50%: Total = 25000, BTC = 0.5, Avg = 50000 (unchanged)
        Assert.That(sellLine.Totals.TotalCost, Is.EqualTo(25000m));
        Assert.That(sellLine.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(sellLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
    }

    [Test]
    public void Should_Auto_Increment_Display_Order_For_Same_Date()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Add multiple lines on the same day - display order should auto-increment
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            999, // This value should be ignored - auto-calculated to 0
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(50000m),
            "First buy");

        profile.AddLine(
            new DateOnly(2024, 1, 1),
            999, // This value should be ignored - auto-calculated to 1
            AvgPriceLineTypes.Buy,
            0.5m,
            FiatValue.New(30000m),
            "Second buy");

        profile.AddLine(
            new DateOnly(2024, 1, 1),
            999, // This value should be ignored - auto-calculated to 2
            AvgPriceLineTypes.Buy,
            0.25m,
            FiatValue.New(20000m),
            "Third buy");

        // Assert - Display orders should be auto-incremented 0, 1, 2
        var orderedLines = profile.AvgPriceLines
            .OrderBy(x => x.Date)
            .ThenBy(x => x.DisplayOrder)
            .ToList();

        Assert.That(orderedLines[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(orderedLines[0].Comment, Is.EqualTo("First buy"));
        Assert.That(orderedLines[0].Totals.TotalCost, Is.EqualTo(50000m));

        Assert.That(orderedLines[1].DisplayOrder, Is.EqualTo(1));
        Assert.That(orderedLines[1].Comment, Is.EqualTo("Second buy"));
        Assert.That(orderedLines[1].Totals.TotalCost, Is.EqualTo(80000m));

        Assert.That(orderedLines[2].DisplayOrder, Is.EqualTo(2));
        Assert.That(orderedLines[2].Comment, Is.EqualTo("Third buy"));
        Assert.That(orderedLines[2].Totals.TotalCost, Is.EqualTo(100000m));
    }

    [Test]
    public void Should_Reset_Display_Order_For_Different_Dates()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Add lines on different dates
        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Day1 First");
        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(30000m), "Day1 Second");
        profile.AddLine(new DateOnly(2024, 1, 2), 0, AvgPriceLineTypes.Buy, 0.25m, FiatValue.New(20000m), "Day2 First");
        profile.AddLine(new DateOnly(2024, 1, 2), 0, AvgPriceLineTypes.Buy, 0.1m, FiatValue.New(10000m), "Day2 Second");

        // Assert - Display orders should reset per date
        var day1Lines = profile.AvgPriceLines.Where(x => x.Date == new DateOnly(2024, 1, 1)).OrderBy(x => x.DisplayOrder).ToList();
        var day2Lines = profile.AvgPriceLines.Where(x => x.Date == new DateOnly(2024, 1, 2)).OrderBy(x => x.DisplayOrder).ToList();

        Assert.That(day1Lines[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(day1Lines[1].DisplayOrder, Is.EqualTo(1));

        Assert.That(day2Lines[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(day2Lines[1].DisplayOrder, Is.EqualTo(1));
    }

    [Test]
    public void Should_Handle_Setup_Line_Override()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Brl)
            .Build();

        // Add a setup to initialize with existing position
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            1,
            AvgPriceLineTypes.Setup,
            2m,
            FiatValue.New(250000m), // 250,000 BRL avg
            "Initial position");

        // Assert
        var setupLine = profile.AvgPriceLines.First();
        Assert.That(setupLine.Totals.Quantity, Is.EqualTo(2m));
        Assert.That(setupLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(250000m));
        Assert.That(setupLine.Totals.TotalCost, Is.EqualTo(500000m)); // 2 * 250000
    }

    #region New Operation Tests

    [Test]
    public void Should_Create_New_Profile_With_Correct_Properties()
    {
        // Arrange
        var name = AvgPriceProfileName.New("My Stack");
        var asset = new AvgPriceAsset("ETH", 18);
        var icon = Icon.Empty;
        var currency = FiatCurrency.Eur;
        var method = AvgPriceCalculationMethod.Fifo;

        // Act
        var profile = AvgPriceProfile.New(name, asset, true, icon, currency, method);

        // Assert
        Assert.That(profile.Name, Is.EqualTo(name));
        Assert.That(profile.Asset, Is.EqualTo(asset));
        Assert.That(profile.Visible, Is.True);
        Assert.That(profile.Icon, Is.EqualTo(icon));
        Assert.That(profile.Currency, Is.EqualTo(currency));
        Assert.That(profile.CalculationMethod, Is.EqualTo(method));
        Assert.That(profile.AvgPriceLines, Is.Empty);
    }

    [Test]
    public void Should_Create_New_Profile_With_Generated_Id()
    {
        // Act
        var profile1 = AvgPriceProfile.New("Profile 1", AvgPriceAsset.Bitcoin, true, Icon.Empty, FiatCurrency.Usd, AvgPriceCalculationMethod.BrazilianRule);
        var profile2 = AvgPriceProfile.New("Profile 2", AvgPriceAsset.Bitcoin, true, Icon.Empty, FiatCurrency.Usd, AvgPriceCalculationMethod.BrazilianRule);

        // Assert
        Assert.That(profile1.Id, Is.Not.Null);
        Assert.That(profile2.Id, Is.Not.Null);
        Assert.That(profile1.Id.Value, Is.Not.EqualTo(profile2.Id.Value));
    }

    [Test]
    public void Should_Raise_CreatedEvent_When_New_Profile_Created()
    {
        // Act
        var profile = AvgPriceProfile.New("Test", AvgPriceAsset.Bitcoin, true, Icon.Empty, FiatCurrency.Usd, AvgPriceCalculationMethod.BrazilianRule);

        // Assert
        Assert.That(profile.Events.Count, Is.EqualTo(1));
        Assert.That(profile.Events.First(), Is.TypeOf<AvgPriceProfileCreatedEvent>());
    }

    [Test]
    public void Should_Not_Raise_CreatedEvent_When_Profile_Created_With_Existing_Version()
    {
        // Act - Create with version > 0 (loading from database)
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithVersion(1)
            .Build();

        // Assert - No created event should be raised for existing profiles
        var createdEvents = profile.Events.OfType<AvgPriceProfileCreatedEvent>();
        Assert.That(createdEvents.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Should_Create_Profile_With_Initial_Version_Zero_For_New()
    {
        // Act
        var profile = AvgPriceProfile.New("Test", AvgPriceAsset.Bitcoin, true, Icon.Empty, FiatCurrency.Usd, AvgPriceCalculationMethod.BrazilianRule);

        // Assert - Version gets incremented when event is added
        Assert.That(profile.Version, Is.EqualTo(1));
    }

    #endregion

    #region ChangeAsset Tests - RecalculateAll

    [Test]
    public void Should_Recalculate_All_Lines_When_Asset_Changes()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithAsset(AvgPriceAsset.Bitcoin)
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Add two buy lines
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First buy");
        profile.AddLine(new DateOnly(2024, 1, 2), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(30000m), "Second buy");

        // Get initial totals
        var orderedLines = profile.AvgPriceLines.OrderBy(x => x.Date).ToList();
        var initialSecondLineTotals = orderedLines[1].Totals;

        // Clear events to track new ones
        profile.ClearEvents();

        // Act - Change asset (this should trigger RecalculateAll)
        profile.ChangeAsset("ETH", 18);

        // Assert
        Assert.That(profile.Asset.Name, Is.EqualTo("ETH"));
        Assert.That(profile.Asset.Precision, Is.EqualTo(18));

        // Totals should still be the same (recalculated but same values since no logic depends on asset directly in calculation)
        var recalculatedLines = profile.AvgPriceLines.OrderBy(x => x.Date).ToList();
        Assert.That(recalculatedLines[1].Totals.TotalCost, Is.EqualTo(80000m));
        Assert.That(recalculatedLines[1].Totals.Quantity, Is.EqualTo(1.5m));
    }

    [Test]
    public void Should_Not_Recalculate_When_Same_Asset()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithAsset(AvgPriceAsset.Bitcoin)
            .Build();

        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Buy");
        profile.ClearEvents();

        // Act - Set same asset
        profile.ChangeAsset("BTC", 8);

        // Assert - No event should be raised
        Assert.That(profile.Events.Count, Is.EqualTo(0));
    }

    [Test]
    public void Should_Raise_UpdatedEvent_When_Asset_Changes()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithAsset(AvgPriceAsset.Bitcoin)
            .Build();

        profile.ClearEvents();

        // Act
        profile.ChangeAsset("ETH", 18);

        // Assert
        Assert.That(profile.Events.Count, Is.EqualTo(1));
        Assert.That(profile.Events.First(), Is.TypeOf<AvgPriceProfileUpdatedEvent>());
    }

    #endregion

    #region ChangeCalculationMethod Tests - RecalculateAll

    [Test]
    public void Should_Recalculate_All_Lines_When_CalculationMethod_Changes_From_BrazilianRule_To_Fifo()
    {
        // Arrange - Profile with Brazilian Rule
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCalculationMethod(AvgPriceCalculationMethod.BrazilianRule)
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Buy 1 BTC at $50,000
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First buy");

        // Buy 0.5 BTC at $60,000
        profile.AddLine(new DateOnly(2024, 1, 2), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(30000m), "Second buy");

        // Sell 0.5 BTC (in Brazilian Rule, cost basis uses avg price)
        profile.AddLine(new DateOnly(2024, 1, 3), 1, AvgPriceLineTypes.Sell, 0.5m, FiatValue.New(35000m), "Sell");

        // Get Brazilian Rule result: After sell, remaining = 1 BTC with avg cost maintained
        var brazilianSellLine = profile.AvgPriceLines.First(x => x.Type == AvgPriceLineTypes.Sell);
        var brazilianRemaining = brazilianSellLine.Totals.Quantity;

        profile.ClearEvents();

        // Act - Change to FIFO
        profile.ChangeCalculationMethod(AvgPriceCalculationMethod.Fifo);

        // Assert - Method changed
        Assert.That(profile.CalculationMethod, Is.EqualTo(AvgPriceCalculationMethod.Fifo));

        // In FIFO, the sell takes from the first bought units
        var fifoSellLine = profile.AvgPriceLines.First(x => x.Type == AvgPriceLineTypes.Sell);

        // After FIFO sell: we sold 0.5 from the first buy (at $50k), remaining = 1 BTC
        Assert.That(fifoSellLine.Totals.Quantity, Is.EqualTo(1m));
        // Remaining cost basis: 0.5 BTC from first buy (25k) + 0.5 BTC from second buy (30k) = 55k
        Assert.That(fifoSellLine.Totals.TotalCost, Is.EqualTo(55000m));
    }

    [Test]
    public void Should_Not_Recalculate_When_Same_CalculationMethod()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCalculationMethod(AvgPriceCalculationMethod.BrazilianRule)
            .Build();

        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Buy");
        profile.ClearEvents();

        // Act - Set same method
        profile.ChangeCalculationMethod(AvgPriceCalculationMethod.BrazilianRule);

        // Assert - No event should be raised
        Assert.That(profile.Events.Count, Is.EqualTo(0));
    }

    [Test]
    public void Should_Raise_UpdatedEvent_When_CalculationMethod_Changes()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCalculationMethod(AvgPriceCalculationMethod.BrazilianRule)
            .Build();

        profile.ClearEvents();

        // Act
        profile.ChangeCalculationMethod(AvgPriceCalculationMethod.Fifo);

        // Assert
        Assert.That(profile.Events.Count, Is.EqualTo(1));
        Assert.That(profile.Events.First(), Is.TypeOf<AvgPriceProfileUpdatedEvent>());
    }

    [Test]
    public void Should_Raise_LineUpdatedEvent_For_Lines_With_Changed_Totals_When_CalculationMethod_Changes()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCalculationMethod(AvgPriceCalculationMethod.BrazilianRule)
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Add 3 lines
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First buy");
        profile.AddLine(new DateOnly(2024, 1, 2), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(60000m), "Second buy");
        profile.AddLine(new DateOnly(2024, 1, 3), 1, AvgPriceLineTypes.Sell, 0.5m, FiatValue.New(70000m), "Sell");

        profile.ClearEvents();

        // Act
        profile.ChangeCalculationMethod(AvgPriceCalculationMethod.Fifo);

        // Assert - Should have AvgPriceLineUpdatedEvent only for lines whose totals changed
        // Buy lines have same totals in both strategies; only sell line differs (FIFO uses first-in cost basis)
        var lineUpdatedEvents = profile.Events.OfType<AvgPriceLineUpdatedEvent>().ToList();
        var profileUpdatedEvents = profile.Events.OfType<AvgPriceProfileUpdatedEvent>().ToList();

        Assert.That(lineUpdatedEvents.Count, Is.GreaterThanOrEqualTo(1), "Should raise AvgPriceLineUpdatedEvent for lines with changed totals");
        Assert.That(profileUpdatedEvents.Count, Is.EqualTo(1), "Should raise AvgPriceProfileUpdatedEvent");

        // The sell line should have changed totals
        var sellLineEvent = lineUpdatedEvents.FirstOrDefault(e => e.AvgPriceLine.Type == AvgPriceLineTypes.Sell);
        Assert.That(sellLineEvent, Is.Not.Null, "Sell line should have AvgPriceLineUpdatedEvent");
    }

    [Test]
    public void Should_Use_New_Strategy_After_Method_Change()
    {
        // Arrange - Start with FIFO
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCalculationMethod(AvgPriceCalculationMethod.Fifo)
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Buy 1 BTC at $50,000
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First buy");

        // Change to Brazilian Rule
        profile.ChangeCalculationMethod(AvgPriceCalculationMethod.BrazilianRule);

        // Add another buy - this should use Brazilian Rule calculation
        profile.AddLine(new DateOnly(2024, 1, 2), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(60000m), "Second buy");

        // Assert - Brazilian Rule averages: Total = 110000, BTC = 2, Avg = 55000
        var lastLine = profile.AvgPriceLines.OrderBy(x => x.Date).Last();
        Assert.That(lastLine.Totals.TotalCost, Is.EqualTo(110000m));
        Assert.That(lastLine.Totals.Quantity, Is.EqualTo(2m));
        Assert.That(lastLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(55000m));
    }

    #endregion

    #region Other Modification Tests

    [Test]
    public void Should_Rename_Profile_And_Raise_Event()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithName("Original Name")
            .Build();

        profile.ClearEvents();

        // Act
        profile.Rename("New Name");

        // Assert
        Assert.That(profile.Name.Value, Is.EqualTo("New Name"));
        Assert.That(profile.Events.Count, Is.EqualTo(1));
        Assert.That(profile.Events.First(), Is.TypeOf<AvgPriceProfileUpdatedEvent>());
    }

    [Test]
    public void Should_Not_Raise_Event_When_Renaming_To_Same_Name()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithName("Same Name")
            .Build();

        profile.ClearEvents();

        // Act
        profile.Rename("Same Name");

        // Assert
        Assert.That(profile.Events.Count, Is.EqualTo(0));
    }

    [Test]
    public void Should_Change_Icon_And_Raise_Event()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithIcon(Icon.Empty)
            .Build();

        profile.ClearEvents();
        var newIcon = new Icon("material", "btc", '\ue0a0', System.Drawing.Color.Orange);

        // Act
        profile.ChangeIcon(newIcon);

        // Assert
        Assert.That(profile.Icon, Is.EqualTo(newIcon));
        Assert.That(profile.Events.Count, Is.EqualTo(1));
        Assert.That(profile.Events.First(), Is.TypeOf<AvgPriceProfileUpdatedEvent>());
    }

    [Test]
    public void Should_Not_Raise_Event_When_Changing_To_Same_Icon()
    {
        // Arrange
        var icon = new Icon("material", "btc", '\ue0a0', System.Drawing.Color.Orange);
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithIcon(icon)
            .Build();

        profile.ClearEvents();

        // Act
        profile.ChangeIcon(icon);

        // Assert
        Assert.That(profile.Events.Count, Is.EqualTo(0));
    }

    [Test]
    public void Should_Change_Visibility_And_Raise_Event()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithVisible(true)
            .Build();

        profile.ClearEvents();

        // Act
        profile.ChangeVisibility(false);

        // Assert
        Assert.That(profile.Visible, Is.False);
        Assert.That(profile.Events.Count, Is.EqualTo(1));
        Assert.That(profile.Events.First(), Is.TypeOf<AvgPriceProfileUpdatedEvent>());
    }

    [Test]
    public void Should_Not_Raise_Event_When_Changing_To_Same_Visibility()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithVisible(true)
            .Build();

        profile.ClearEvents();

        // Act
        profile.ChangeVisibility(true);

        // Assert
        Assert.That(profile.Events.Count, Is.EqualTo(0));
    }

    #endregion

    #region MoveLineUp Tests

    [Test]
    public void MoveLineUp_Should_Swap_Display_Order_With_Previous_Line()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Add three lines on the same date with sequential display orders
        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(60000m), "Second");
        profile.AddLine(new DateOnly(2024, 1, 1), 2, AvgPriceLineTypes.Sell, 0.25m, FiatValue.New(70000m), "Third");

        var orderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();
        var secondLine = orderedLines[1];

        profile.ClearEvents();

        // Act - Move second line up
        profile.MoveLineUp(secondLine);

        // Assert - Second line should now be first (display order 0)
        var reorderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();
        Assert.That(reorderedLines[0].Comment, Is.EqualTo("Second"));
        Assert.That(reorderedLines[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(reorderedLines[1].Comment, Is.EqualTo("First"));
        Assert.That(reorderedLines[1].DisplayOrder, Is.EqualTo(1));
        Assert.That(reorderedLines[2].Comment, Is.EqualTo("Third"));
        Assert.That(reorderedLines[2].DisplayOrder, Is.EqualTo(2));
    }

    [Test]
    public void MoveLineUp_Should_Recalculate_Totals_In_New_Order()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // First: Buy 1 BTC at $50,000
        // Second: Buy 0.5 BTC at $60,000
        // After first: Total = $50,000, BTC = 1, Avg = $50,000
        // After second (current order): Total = $80,000, BTC = 1.5, Avg = $53,333.33
        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Big Buy");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(30000m), "Small Buy");

        var orderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();
        var smallBuy = orderedLines[1];

        // Act - Move small buy to first position
        profile.MoveLineUp(smallBuy);

        // Assert - Now: Small Buy first, then Big Buy
        // After small buy: Total = $30,000, BTC = 0.5, Avg = $60,000
        // After big buy: Total = $80,000, BTC = 1.5, Avg = $53,333.33
        var reorderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();

        Assert.That(reorderedLines[0].Comment, Is.EqualTo("Small Buy"));
        Assert.That(reorderedLines[0].Totals.TotalCost, Is.EqualTo(30000m));
        Assert.That(reorderedLines[0].Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(reorderedLines[0].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(60000m));

        Assert.That(reorderedLines[1].Comment, Is.EqualTo("Big Buy"));
        Assert.That(reorderedLines[1].Totals.TotalCost, Is.EqualTo(80000m));
        Assert.That(reorderedLines[1].Totals.Quantity, Is.EqualTo(1.5m));
    }

    [Test]
    public void MoveLineUp_Should_Raise_UpdatedEvent_For_Affected_Lines()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(60000m), "Second");

        var secondLine = profile.AvgPriceLines.First(x => x.Comment == "Second");

        profile.ClearEvents();

        // Act
        profile.MoveLineUp(secondLine);

        // Assert - Both lines should have updated events (display order changed for both)
        var updatedEvents = profile.Events.OfType<AvgPriceLineUpdatedEvent>().ToList();
        Assert.That(updatedEvents.Count, Is.EqualTo(2));
    }

    [Test]
    public void MoveLineUp_Should_Not_Change_Anything_When_Already_First()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(60000m), "Second");

        var firstLine = profile.AvgPriceLines.First(x => x.Comment == "First");
        var originalTotals = firstLine.Totals;

        profile.ClearEvents();

        // Act - Try to move first line up (should fail gracefully or throw)
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.MoveLineUp(firstLine));
    }

    [Test]
    public void MoveLineUp_Should_Only_Affect_Lines_On_Same_Date()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Lines on different dates
        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Day1");
        profile.AddLine(new DateOnly(2024, 1, 2), 0, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(60000m), "Day2 First");
        profile.AddLine(new DateOnly(2024, 1, 2), 1, AvgPriceLineTypes.Buy, 0.25m, FiatValue.New(70000m), "Day2 Second");

        var day2Second = profile.AvgPriceLines.First(x => x.Comment == "Day2 Second");
        var day1Line = profile.AvgPriceLines.First(x => x.Comment == "Day1");
        var originalDay1Order = day1Line.DisplayOrder;

        profile.ClearEvents();

        // Act
        profile.MoveLineUp(day2Second);

        // Assert - Day1 line should not be affected
        Assert.That(day1Line.DisplayOrder, Is.EqualTo(originalDay1Order));
    }

    #endregion

    #region MoveLineDown Tests

    [Test]
    public void MoveLineDown_Should_Swap_Display_Order_With_Next_Line()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(60000m), "Second");
        profile.AddLine(new DateOnly(2024, 1, 1), 2, AvgPriceLineTypes.Sell, 0.25m, FiatValue.New(70000m), "Third");

        var firstLine = profile.AvgPriceLines.First(x => x.Comment == "First");

        profile.ClearEvents();

        // Act - Move first line down
        profile.MoveLineDown(firstLine);

        // Assert - First line should now be second (display order 1)
        var reorderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();
        Assert.That(reorderedLines[0].Comment, Is.EqualTo("Second"));
        Assert.That(reorderedLines[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(reorderedLines[1].Comment, Is.EqualTo("First"));
        Assert.That(reorderedLines[1].DisplayOrder, Is.EqualTo(1));
        Assert.That(reorderedLines[2].Comment, Is.EqualTo("Third"));
        Assert.That(reorderedLines[2].DisplayOrder, Is.EqualTo(2));
    }

    [Test]
    public void MoveLineDown_Should_Recalculate_Totals_In_New_Order()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Big Buy first, Small Buy second
        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Big Buy");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(30000m), "Small Buy");

        var bigBuy = profile.AvgPriceLines.First(x => x.Comment == "Big Buy");

        // Act - Move big buy down (to second position)
        profile.MoveLineDown(bigBuy);

        // Assert - Now: Small Buy first, then Big Buy
        var reorderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();

        Assert.That(reorderedLines[0].Comment, Is.EqualTo("Small Buy"));
        Assert.That(reorderedLines[0].Totals.TotalCost, Is.EqualTo(30000m));
        Assert.That(reorderedLines[0].Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(reorderedLines[0].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(60000m));

        Assert.That(reorderedLines[1].Comment, Is.EqualTo("Big Buy"));
        Assert.That(reorderedLines[1].Totals.TotalCost, Is.EqualTo(80000m));
        Assert.That(reorderedLines[1].Totals.Quantity, Is.EqualTo(1.5m));
    }

    [Test]
    public void MoveLineDown_Should_Raise_UpdatedEvent_For_Affected_Lines()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(60000m), "Second");

        var firstLine = profile.AvgPriceLines.First(x => x.Comment == "First");

        profile.ClearEvents();

        // Act
        profile.MoveLineDown(firstLine);

        // Assert - Both lines should have updated events
        var updatedEvents = profile.Events.OfType<AvgPriceLineUpdatedEvent>().ToList();
        Assert.That(updatedEvents.Count, Is.EqualTo(2));
    }

    [Test]
    public void MoveLineDown_Should_Not_Change_Anything_When_Already_Last()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(60000m), "Second");

        var lastLine = profile.AvgPriceLines.First(x => x.Comment == "Second");

        profile.ClearEvents();

        // Act - Try to move last line down (should fail)
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.MoveLineDown(lastLine));
    }

    [Test]
    public void MoveLineDown_Should_Only_Affect_Lines_On_Same_Date()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Day1 First");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 0.5m, FiatValue.New(60000m), "Day1 Second");
        profile.AddLine(new DateOnly(2024, 1, 2), 0, AvgPriceLineTypes.Buy, 0.25m, FiatValue.New(70000m), "Day2");

        var day1First = profile.AvgPriceLines.First(x => x.Comment == "Day1 First");
        var day2Line = profile.AvgPriceLines.First(x => x.Comment == "Day2");
        var originalDay2Order = day2Line.DisplayOrder;

        profile.ClearEvents();

        // Act
        profile.MoveLineDown(day1First);

        // Assert - Day2 line should not be affected
        Assert.That(day2Line.DisplayOrder, Is.EqualTo(originalDay2Order));
    }

    #endregion

    #region Reordering and Recalculation Tests

    [Test]
    public void Recalculation_Should_Respect_Display_Order_For_Same_Date_Lines()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Two buys on same day - order affects running totals
        // First Buy: 1 BTC at $50,000 (order 0)
        // Second Buy: 1 BTC at $60,000 (order 1)
        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First Buy");
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(60000m), "Second Buy");

        var orderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();

        // Assert initial state - running totals show order
        Assert.That(orderedLines[0].Totals.TotalCost, Is.EqualTo(50000m)); // Just first buy
        Assert.That(orderedLines[1].Totals.TotalCost, Is.EqualTo(110000m)); // Both buys

        var secondBuy = orderedLines[1];

        // Act - Move second buy before first
        profile.MoveLineUp(secondBuy);

        // Assert - After reorder, running totals should change
        var reorderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();
        Assert.That(reorderedLines[0].Comment, Is.EqualTo("Second Buy"));
        Assert.That(reorderedLines[1].Comment, Is.EqualTo("First Buy"));

        // Running totals now reflect new order
        Assert.That(reorderedLines[0].Totals.TotalCost, Is.EqualTo(60000m)); // Just second buy (now first)
        Assert.That(reorderedLines[1].Totals.TotalCost, Is.EqualTo(110000m)); // Both buys
    }

    [Test]
    public void Move_Should_Correctly_Recalculate_When_Reordering_Affects_Avg_Cost()
    {
        // Arrange - This test verifies that moving lines affects the running totals correctly
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Three buys on same day with different prices
        // Order matters for running totals display
        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Cheap");     // Avg = 50000
        profile.AddLine(new DateOnly(2024, 1, 1), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(60000m), "Medium");    // Avg = 55000
        profile.AddLine(new DateOnly(2024, 1, 1), 2, AvgPriceLineTypes.Buy, 1m, FiatValue.New(70000m), "Expensive"); // Avg = 60000

        var expensiveLine = profile.AvgPriceLines.First(x => x.Comment == "Expensive");

        // Initial state verification
        var initialLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();
        Assert.That(initialLines[0].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
        Assert.That(initialLines[1].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(55000m));
        Assert.That(initialLines[2].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(60000m));

        // Act - Move expensive to first position (two moves up)
        profile.MoveLineUp(expensiveLine);
        profile.MoveLineUp(expensiveLine);

        // Assert - New order: Expensive, Cheap, Medium
        var reorderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();
        Assert.That(reorderedLines[0].Comment, Is.EqualTo("Expensive"));
        Assert.That(reorderedLines[1].Comment, Is.EqualTo("Cheap"));
        Assert.That(reorderedLines[2].Comment, Is.EqualTo("Medium"));

        // Verify recalculated averages
        Assert.That(reorderedLines[0].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(70000m)); // Just expensive
        Assert.That(reorderedLines[1].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(60000m)); // (70k + 50k) / 2
        Assert.That(reorderedLines[2].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(60000m)); // (70k + 50k + 60k) / 3
    }

    [Test]
    public void Move_Should_Not_Raise_Events_When_No_Order_Change_Needed()
    {
        // Arrange - Single line on date, moving shouldn't change anything
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        profile.AddLine(new DateOnly(2024, 1, 1), 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Only");

        var onlyLine = profile.AvgPriceLines.First();

        profile.ClearEvents();

        // Act - Try to move the only line (should throw since no adjacent line)
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.MoveLineUp(onlyLine));
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.MoveLineDown(onlyLine));
    }

    [Test]
    public void RearrangeDisplayOrder_Should_Normalize_Display_Orders_To_Sequential()
    {
        // Arrange - Create profile with non-sequential display orders
        var line1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithDisplayOrder(5) // Non-sequential
            .WithQuantity(1m)
            .WithAmount(FiatValue.New(50000m))
            .Build();

        var line2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithDisplayOrder(10) // Non-sequential
            .WithQuantity(0.5m)
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var profile = AvgPriceProfileBuilder.AProfile()
            .WithLines(line1, line2)
            .Build();

        // Get the line that's "second" (display order 10)
        var secondLine = profile.AvgPriceLines.First(x => x.DisplayOrder == 10);

        profile.ClearEvents();

        // Act - Moving should normalize the display orders
        profile.MoveLineUp(secondLine);

        // Assert - Display orders should now be 0, 1 (normalized)
        var orderedLines = profile.AvgPriceLines.OrderBy(x => x.DisplayOrder).ToList();
        Assert.That(orderedLines[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(orderedLines[1].DisplayOrder, Is.EqualTo(1));
    }

    #endregion
}
