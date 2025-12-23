using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.CalculationStrategies;
using Valt.Infra.Kernel;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.AvgPrice;

[TestFixture]
public class BrazilianRuleCalculationStrategyTests
{
    private BrazilianRuleCalculationStrategy _strategy = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        var profile = new AvgPriceProfileBuilder()
            .WithCalculationMethod(AvgPriceCalculationMethod.BrazilianRule)
            .WithCurrency(FiatCurrency.Brl)
            .WithIcon(Icon.Empty)
            .WithName("Test")
            .Build();
        
        _strategy = new BrazilianRuleCalculationStrategy(profile);
    }

    [Test]
    public void Should_Calculate_Avg_Price_For_Single_Buy()
    {
        // Arrange: Buy 0.1 BTC at $50,000
        var line = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((0.1m))
            .WithUnitPrice(FiatValue.New(50000m))
            .Build();

        var lines = new List<AvgPriceLine> { line };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // Total cost = 0.1 * 50000 = 5000
        // BTC amount = 0.1
        // Avg = 5000 / 0.1 = 50000
        Assert.That(line.Totals.TotalCost, Is.EqualTo(5000m));
        Assert.That(line.Totals.Quantity, Is.EqualTo(0.1m));
        Assert.That(line.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
    }

    [Test]
    public void Should_Calculate_Avg_Price_For_Multiple_Buys_At_Different_Prices()
    {
        // Arrange:
        // Buy 1: 0.1 BTC at $50,000
        // Buy 2: 0.2 BTC at $60,000
        var line1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithDisplayOrder(1)
            .WithQuantity((0.1m))
            .WithUnitPrice(FiatValue.New(50000m))
            .Build();

        var line2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithDisplayOrder(1)
            .WithQuantity((0.2m))
            .WithUnitPrice(FiatValue.New(60000m))
            .Build();

        var lines = new List<AvgPriceLine> { line1, line2 };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After line1: Total = 5000, BTC = 0.1, Avg = 50000
        Assert.That(line1.Totals.TotalCost, Is.EqualTo(5000m));
        Assert.That(line1.Totals.Quantity, Is.EqualTo(0.1m));
        Assert.That(line1.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));

        // After line2: Total = 5000 + 12000 = 17000, BTC = 0.3, Avg = 17000/0.3 = 56666.67
        Assert.That(line2.Totals.TotalCost, Is.EqualTo(17000m));
        Assert.That(line2.Totals.Quantity, Is.EqualTo(0.3m));
        Assert.That(line2.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(56666.67m));
    }

    [Test]
    public void Should_Calculate_Avg_Price_After_Partial_Sell()
    {
        // Arrange:
        // Buy: 1 BTC at $50,000
        // Sell: 0.5 BTC
        var buyLine = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithUnitPrice(FiatValue.New(50000m))
            .Build();

        var sellLine = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((0.5m))
            .WithUnitPrice(FiatValue.New(60000m)) // Sell price doesn't affect avg in Brazilian rule
            .Build();

        var lines = new List<AvgPriceLine> { buyLine, sellLine };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After buy: Total = 50000, BTC = 1, Avg = 50000
        Assert.That(buyLine.Totals.TotalCost, Is.EqualTo(50000m));
        Assert.That(buyLine.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(buyLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));

        // After sell (Brazilian rule: reduce cost proportionally):
        // Proportion sold = 0.5 / 1 = 0.5
        // New total cost = 50000 - (50000 * 0.5) = 25000
        // New BTC = 1 - 0.5 = 0.5
        // Avg = 25000 / 0.5 = 50000 (stays the same in Brazilian rule)
        Assert.That(sellLine.Totals.TotalCost, Is.EqualTo(25000m));
        Assert.That(sellLine.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(sellLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
    }

    [Test]
    public void Should_Calculate_Avg_Price_For_Buy_Sell_Buy_Sequence()
    {
        // Arrange:
        // Buy: 1 BTC at $40,000
        // Sell: 0.5 BTC
        // Buy: 0.3 BTC at $60,000
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithUnitPrice(FiatValue.New(40000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((0.5m))
            .WithUnitPrice(FiatValue.New(50000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((0.3m))
            .WithUnitPrice(FiatValue.New(60000m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, sell, buy2 };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After buy1: Total = 40000, BTC = 1, Avg = 40000
        Assert.That(buy1.Totals.TotalCost, Is.EqualTo(40000m));
        Assert.That(buy1.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(buy1.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(40000m));

        // After sell: Total = 40000 - 20000 = 20000, BTC = 0.5, Avg = 40000
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(20000m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(40000m));

        // After buy2: Total = 20000 + 18000 = 38000, BTC = 0.8, Avg = 38000/0.8 = 47500
        Assert.That(buy2.Totals.TotalCost, Is.EqualTo(38000m));
        Assert.That(buy2.Totals.Quantity, Is.EqualTo(0.8m));
        Assert.That(buy2.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(47500m));
    }

    [Test]
    public void Should_Handle_Sell_All_And_Then_Buy_Again()
    {
        // Arrange:
        // Buy: 1 BTC at $50,000
        // Sell: 1 BTC (all)
        // Buy: 0.5 BTC at $70,000
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithUnitPrice(FiatValue.New(50000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((1m))
            .WithUnitPrice(FiatValue.New(60000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((0.5m))
            .WithUnitPrice(FiatValue.New(70000m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, sell, buy2 };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After sell all: Total = 0, BTC = 0, Avg = 0
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(0m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(0m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(0m));

        // After buy2: Total = 35000, BTC = 0.5, Avg = 70000
        Assert.That(buy2.Totals.TotalCost, Is.EqualTo(35000m));
        Assert.That(buy2.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(buy2.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(70000m));
    }

    [Test]
    public void Should_Override_Values_With_Setup_Line()
    {
        // Arrange:
        // Buy: 1 BTC at $50,000
        // Setup: Override with 2 BTC at $45,000 avg
        var buy = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithUnitPrice(FiatValue.New(50000m))
            .Build();

        var setup = AvgPriceLineBuilder.ASetupLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((2m)) // New BTC amount
            .WithUnitPrice(FiatValue.New(45000m)) // New avg price
            .Build();

        var lines = new List<AvgPriceLine> { buy, setup };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After buy: normal calculation
        Assert.That(buy.Totals.TotalCost, Is.EqualTo(50000m));
        Assert.That(buy.Totals.Quantity, Is.EqualTo(1m));

        // After setup: values are overridden
        // Total cost = 2 * 45000 = 90000
        // BTC = 2
        // Avg = 45000
        Assert.That(setup.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(45000m));
        Assert.That(setup.Totals.Quantity, Is.EqualTo(2m));
        Assert.That(setup.Totals.TotalCost, Is.EqualTo(90000m));
    }

    [Test]
    public void Should_Continue_Calculations_After_Setup()
    {
        // Arrange:
        // Setup: 1 BTC at $40,000 avg
        // Buy: 0.5 BTC at $60,000
        var setup = AvgPriceLineBuilder.ASetupLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithUnitPrice(FiatValue.New(40000m))
            .Build();

        var buy = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((0.5m))
            .WithUnitPrice(FiatValue.New(60000m))
            .Build();

        var lines = new List<AvgPriceLine> { setup, buy };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After setup: Total = 40000, BTC = 1, Avg = 40000
        Assert.That(setup.Totals.TotalCost, Is.EqualTo(40000m));
        Assert.That(setup.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(setup.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(40000m));

        // After buy: Total = 40000 + 30000 = 70000, BTC = 1.5, Avg = 70000/1.5 = 46666.67
        Assert.That(buy.Totals.TotalCost, Is.EqualTo(70000m));
        Assert.That(buy.Totals.Quantity, Is.EqualTo(1.5m));
        Assert.That(buy.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(46666.67m));
    }

    [Test]
    public void Should_Handle_Multiple_Buys_Same_Day_With_Display_Order()
    {
        // Arrange: Two buys on the same day, ordered by displayOrder
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithDisplayOrder(1)
            .WithQuantity((0.5m))
            .WithUnitPrice(FiatValue.New(50000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithDisplayOrder(2)
            .WithQuantity((0.5m))
            .WithUnitPrice(FiatValue.New(60000m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, buy2 };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After buy1: Total = 25000, BTC = 0.5, Avg = 50000
        Assert.That(buy1.Totals.TotalCost, Is.EqualTo(25000m));
        Assert.That(buy1.Totals.Quantity, Is.EqualTo(0.5m));

        // After buy2: Total = 25000 + 30000 = 55000, BTC = 1, Avg = 55000
        Assert.That(buy2.Totals.TotalCost, Is.EqualTo(55000m));
        Assert.That(buy2.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(buy2.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(55000m));
    }

    [Test]
    public void Should_Handle_Empty_List()
    {
        // Arrange
        var lines = new List<AvgPriceLine>();

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => _strategy.CalculateTotals(lines));
    }

    [Test]
    public void Should_Calculate_With_Small_Bitcoin_Amounts()
    {
        // Arrange: Buy 0.001 BTC (100,000 sats) at $50,000
        var line = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((0.001m))
            .WithUnitPrice(FiatValue.New(50000m))
            .Build();

        var lines = new List<AvgPriceLine> { line };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // Total cost = 0.001 * 50000 = 50
        Assert.That(line.Totals.TotalCost, Is.EqualTo(50m));
        Assert.That(line.Totals.Quantity, Is.EqualTo(0.001m));
        Assert.That(line.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
    }
}
