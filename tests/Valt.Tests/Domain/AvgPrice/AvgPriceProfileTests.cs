using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.AvgPrice;
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
            .WithBtcAmount(BtcValue.ParseBitcoin(1m))
            .WithBitcoinUnitPrice(FiatValue.New(50000m))
            .Build();

        var line2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithBtcAmount(BtcValue.ParseBitcoin(0.5m))
            .WithBitcoinUnitPrice(FiatValue.New(60000m))
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
            BtcValue.ParseBitcoin(1m),
            FiatValue.New(50000m),
            "First buy");

        // Assert
        Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(1));
        var firstLine = profile.AvgPriceLines.First();
        Assert.That(firstLine.Totals.TotalCost, Is.EqualTo(50000m));
        Assert.That(firstLine.Totals.BtcAmount.Btc, Is.EqualTo(1m));
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
            BtcValue.ParseBitcoin(0.5m),
            FiatValue.New(60000m),
            "Second buy");

        profile.AddLine(
            new DateOnly(2024, 1, 1),
            1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(1m),
            FiatValue.New(50000m),
            "First buy");

        // Assert
        Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(2));

        // Lines should be calculated in date order
        var orderedLines = profile.AvgPriceLines.OrderBy(x => x.Date).ToList();

        // First line (Jan 1): Total = 50000, BTC = 1, Avg = 50000
        Assert.That(orderedLines[0].Totals.TotalCost, Is.EqualTo(50000m));
        Assert.That(orderedLines[0].Totals.BtcAmount.Btc, Is.EqualTo(1m));
        Assert.That(orderedLines[0].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));

        // Second line (Jan 2): Total = 50000 + 30000 = 80000, BTC = 1.5, Avg = 53333.33
        Assert.That(orderedLines[1].Totals.TotalCost, Is.EqualTo(80000m));
        Assert.That(orderedLines[1].Totals.BtcAmount.Btc, Is.EqualTo(1.5m));
        Assert.That(orderedLines[1].Totals.AvgCostOfAcquisition.Value, Is.EqualTo(53333.33m));
    }

    [Test]
    public void Should_Remove_Line_And_Recalculate_Totals()
    {
        // Arrange
        var line1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithDisplayOrder(1)
            .WithBtcAmount(BtcValue.ParseBitcoin(1m))
            .WithBitcoinUnitPrice(FiatValue.New(50000m))
            .Build();

        var line2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithDisplayOrder(1)
            .WithBtcAmount(BtcValue.ParseBitcoin(0.5m))
            .WithBitcoinUnitPrice(FiatValue.New(60000m))
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
        Assert.That(remainingLine.Totals.BtcAmount.Btc, Is.EqualTo(0.5m));
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
            BtcValue.ParseBitcoin(1m),
            FiatValue.New(50000m),
            "Buy");

        // Sell 0.5 BTC
        profile.AddLine(
            new DateOnly(2024, 1, 2),
            1,
            AvgPriceLineTypes.Sell,
            BtcValue.ParseBitcoin(0.5m),
            FiatValue.New(70000m), // Sell price doesn't affect avg in Brazilian rule
            "Sell");

        // Assert
        Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(2));

        var sellLine = profile.AvgPriceLines.First(x => x.Type == AvgPriceLineTypes.Sell);
        // After selling 50%: Total = 25000, BTC = 0.5, Avg = 50000 (unchanged)
        Assert.That(sellLine.Totals.TotalCost, Is.EqualTo(25000m));
        Assert.That(sellLine.Totals.BtcAmount.Btc, Is.EqualTo(0.5m));
        Assert.That(sellLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
    }

    [Test]
    public void Should_Handle_Display_Order_For_Same_Date()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Add two lines on the same day with different display orders
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            2, // Second in order
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(0.5m),
            FiatValue.New(60000m),
            "Second buy");

        profile.AddLine(
            new DateOnly(2024, 1, 1),
            1, // First in order
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(1m),
            FiatValue.New(50000m),
            "First buy");

        // Assert
        var orderedLines = profile.AvgPriceLines
            .OrderBy(x => x.Date)
            .ThenBy(x => x.DisplayOrder)
            .ToList();

        // First line (order 1): Total = 50000, BTC = 1
        Assert.That(orderedLines[0].DisplayOrder, Is.EqualTo(1));
        Assert.That(orderedLines[0].Totals.TotalCost, Is.EqualTo(50000m));

        // Second line (order 2): Total = 80000, BTC = 1.5
        Assert.That(orderedLines[1].DisplayOrder, Is.EqualTo(2));
        Assert.That(orderedLines[1].Totals.TotalCost, Is.EqualTo(80000m));
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
            BtcValue.ParseBitcoin(2m),
            FiatValue.New(250000m), // 250,000 BRL avg
            "Initial position");

        // Assert
        var setupLine = profile.AvgPriceLines.First();
        Assert.That(setupLine.Totals.BtcAmount.Btc, Is.EqualTo(2m));
        Assert.That(setupLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(250000m));
        Assert.That(setupLine.Totals.TotalCost, Is.EqualTo(500000m)); // 2 * 250000
    }
}
