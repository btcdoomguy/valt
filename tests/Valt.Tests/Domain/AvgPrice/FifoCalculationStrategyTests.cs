using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.CalculationStrategies;
using Valt.Infra.Kernel;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.AvgPrice;

[TestFixture]
public class FifoCalculationStrategyTests
{
    private FifoCalculationStrategy _strategy = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        var profile = AvgPriceProfileBuilder.AFifoProfile()
            .WithIcon(Icon.Empty)
            .WithName("Test")
            .Build();

        _strategy = new FifoCalculationStrategy(profile);
    }

    #region Single Buy Tests

    [Test]
    public void Should_Calculate_Avg_Price_For_Single_Buy()
    {
        // Arrange: Buy 0.1 BTC at $50,000
        var line = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((0.1m))
            .WithAmount(FiatValue.New(5000m))
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
    public void Should_Calculate_With_Small_Bitcoin_Amounts()
    {
        // Arrange: Buy 0.001 BTC (100,000 sats) at $50,000
        var line = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((0.001m))
            .WithAmount(FiatValue.New(50m))
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

    #endregion

    #region Multiple Buys Tests

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
            .WithAmount(FiatValue.New(5000m))
            .Build();

        var line2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithDisplayOrder(1)
            .WithQuantity((0.2m))
            .WithAmount(FiatValue.New(12000m))
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
    public void Should_Handle_Multiple_Buys_Same_Day_With_Display_Order()
    {
        // Arrange: Two buys on the same day, ordered by displayOrder
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithDisplayOrder(1)
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(25000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithDisplayOrder(2)
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(30000m))
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

    #endregion

    #region FIFO-Specific Sell Tests

    [Test]
    public void Should_Sell_From_First_Lot_In_Fifo_Order()
    {
        // Arrange:
        // Buy 1: 1 BTC at $40,000 (first lot)
        // Buy 2: 1 BTC at $60,000 (second lot)
        // Sell: 0.5 BTC (should come from first lot at $40,000)
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(40000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(70000m)) // Sell price doesn't affect cost basis in FIFO
            .Build();

        var lines = new List<AvgPriceLine> { buy1, buy2, sell };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After sell in FIFO:
        // - First lot: 0.5 BTC remains at $40,000 (cost = 0.5 * 40000 = 20000)
        // - Second lot: 1 BTC at $60,000 (cost = 60000)
        // Total cost = 20000 + 60000 = 80000
        // BTC = 1.5
        // Avg = 80000 / 1.5 = 53333.33
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(80000m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(1.5m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(53333.33m));
    }

    [Test]
    public void Should_Consume_Entire_First_Lot_And_Partial_Second()
    {
        // Arrange:
        // Buy 1: 1 BTC at $40,000
        // Buy 2: 1 BTC at $60,000
        // Sell: 1.5 BTC (entire first lot + 0.5 from second lot)
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(40000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((1.5m))
            .WithAmount(FiatValue.New(70000m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, buy2, sell };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After sell in FIFO:
        // - First lot: fully consumed
        // - Second lot: 0.5 BTC remains at $60,000 (cost = 0.5 * 60000 = 30000)
        // Total cost = 30000
        // BTC = 0.5
        // Avg = 30000 / 0.5 = 60000
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(30000m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(60000m));
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
            .WithAmount(FiatValue.New(50000m))
            .Build();

        var sellLine = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var lines = new List<AvgPriceLine> { buyLine, sellLine };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After buy: Total = 50000, BTC = 1, Avg = 50000
        Assert.That(buyLine.Totals.TotalCost, Is.EqualTo(50000m));
        Assert.That(buyLine.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(buyLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));

        // After sell (single lot, same as Brazilian rule in this case):
        // Remaining: 0.5 BTC at $50,000 (cost = 25000)
        Assert.That(sellLine.Totals.TotalCost, Is.EqualTo(25000m));
        Assert.That(sellLine.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(sellLine.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
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
            .WithAmount(FiatValue.New(50000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(35000m))
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

    #endregion

    #region Buy-Sell-Buy Sequence Tests

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
            .WithAmount(FiatValue.New(40000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(25000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((0.3m))
            .WithAmount(FiatValue.New(18000m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, sell, buy2 };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After buy1: Total = 40000, BTC = 1, Avg = 40000
        Assert.That(buy1.Totals.TotalCost, Is.EqualTo(40000m));
        Assert.That(buy1.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(buy1.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(40000m));

        // After sell: 0.5 BTC remains at $40,000
        // Total = 0.5 * 40000 = 20000, BTC = 0.5, Avg = 40000
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(20000m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(40000m));

        // After buy2:
        // - First lot: 0.5 BTC at $40,000 (cost = 20000)
        // - Second lot: 0.3 BTC at $60,000 (cost = 18000)
        // Total = 38000, BTC = 0.8, Avg = 38000/0.8 = 47500
        Assert.That(buy2.Totals.TotalCost, Is.EqualTo(38000m));
        Assert.That(buy2.Totals.Quantity, Is.EqualTo(0.8m));
        Assert.That(buy2.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(47500m));
    }

    [Test]
    public void Should_Consume_Multiple_Lots_In_Fifo_Order()
    {
        // Arrange:
        // Buy 1: 0.5 BTC at $30,000
        // Buy 2: 0.5 BTC at $40,000
        // Buy 3: 0.5 BTC at $50,000
        // Sell: 1 BTC (consumes first two lots completely)
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(15000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(20000m))
            .Build();

        var buy3 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(25000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 4))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, buy2, buy3, sell };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After sell: only third lot remains (0.5 BTC at $50,000)
        // Total = 25000, BTC = 0.5, Avg = 50000
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(25000m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
    }

    #endregion

    #region Setup Line Tests

    [Test]
    public void Should_Override_Values_With_Setup_Line()
    {
        // Arrange:
        // Buy: 1 BTC at $50,000
        // Setup: Override with 2 BTC at $45,000 avg
        var buy = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(50000m))
            .Build();

        var setup = AvgPriceLineBuilder.ASetupLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((2m)) // New BTC amount
            .WithAmount(FiatValue.New(45000m)) // New avg price per unit
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
            .WithAmount(FiatValue.New(40000m))
            .Build();

        var buy = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(30000m))
            .Build();

        var lines = new List<AvgPriceLine> { setup, buy };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After setup: Total = 40000, BTC = 1, Avg = 40000
        Assert.That(setup.Totals.TotalCost, Is.EqualTo(40000m));
        Assert.That(setup.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(setup.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(40000m));

        // After buy:
        // - Setup lot: 1 BTC at $40,000 (cost = 40000)
        // - Buy lot: 0.5 BTC at $60,000 (cost = 30000)
        // Total = 70000, BTC = 1.5, Avg = 70000/1.5 = 46666.67
        Assert.That(buy.Totals.TotalCost, Is.EqualTo(70000m));
        Assert.That(buy.Totals.Quantity, Is.EqualTo(1.5m));
        Assert.That(buy.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(46666.67m));
    }

    [Test]
    public void Should_Handle_Setup_Then_Sell_In_Fifo_Order()
    {
        // Arrange:
        // Setup: 1 BTC at $40,000
        // Buy: 1 BTC at $60,000
        // Sell: 0.5 BTC (should come from setup lot)
        var setup = AvgPriceLineBuilder.ASetupLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(40000m))
            .Build();

        var buy = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(70000m))
            .Build();

        var lines = new List<AvgPriceLine> { setup, buy, sell };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After sell:
        // - Setup lot: 0.5 BTC remains at $40,000 (cost = 20000)
        // - Buy lot: 1 BTC at $60,000 (cost = 60000)
        // Total = 80000, BTC = 1.5, Avg = 80000/1.5 = 53333.33
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(80000m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(1.5m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(53333.33m));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Should_Handle_Empty_List()
    {
        // Arrange
        var lines = new List<AvgPriceLine>();

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => _strategy.CalculateTotals(lines));
    }

    [Test]
    public void Should_Handle_Sell_Exactly_Matching_First_Lot()
    {
        // Arrange:
        // Buy 1: 1 BTC at $40,000
        // Buy 2: 1 BTC at $60,000
        // Sell: 1 BTC (exactly the first lot)
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(40000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(70000m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, buy2, sell };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After sell: only second lot remains (1 BTC at $60,000)
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(60000m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(60000m));
    }

    [Test]
    public void Should_Handle_Multiple_Sells_Consuming_Lots_Progressively()
    {
        // Arrange:
        // Buy 1: 1 BTC at $30,000
        // Buy 2: 1 BTC at $50,000
        // Sell 1: 0.5 BTC (from first lot)
        // Sell 2: 0.5 BTC (remaining first lot)
        // Sell 3: 0.5 BTC (from second lot)
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(30000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(50000m))
            .Build();

        var sell1 = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithDisplayOrder(1)
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var sell2 = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithDisplayOrder(2)
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var sell3 = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithDisplayOrder(3)
            .WithQuantity((0.5m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, buy2, sell1, sell2, sell3 };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // After sell1: first lot has 0.5 BTC at $30,000, second lot has 1 BTC at $50,000
        // Total = 15000 + 50000 = 65000, BTC = 1.5, Avg = 65000/1.5 = 43333.33
        Assert.That(sell1.Totals.TotalCost, Is.EqualTo(65000m));
        Assert.That(sell1.Totals.Quantity, Is.EqualTo(1.5m));
        Assert.That(sell1.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(43333.33m));

        // After sell2: first lot fully consumed, second lot has 1 BTC at $50,000
        Assert.That(sell2.Totals.TotalCost, Is.EqualTo(50000m));
        Assert.That(sell2.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(sell2.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));

        // After sell3: second lot has 0.5 BTC at $50,000
        Assert.That(sell3.Totals.TotalCost, Is.EqualTo(25000m));
        Assert.That(sell3.Totals.Quantity, Is.EqualTo(0.5m));
        Assert.That(sell3.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));
    }

    [Test]
    public void Should_Demonstrate_Difference_From_Brazilian_Rule()
    {
        // This test demonstrates the key difference between FIFO and Brazilian Rule
        // In Brazilian Rule, the average stays constant after a sell
        // In FIFO, the average changes based on which lots are sold

        // Arrange:
        // Buy 1: 1 BTC at $20,000 (cheap lot first)
        // Buy 2: 1 BTC at $80,000 (expensive lot second)
        // Sell: 1 BTC
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(20000m))
            .Build();

        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(80000m))
            .Build();

        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity((1m))
            .WithAmount(FiatValue.New(60000m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, buy2, sell };

        // Act
        _strategy.CalculateTotals(lines);

        // Assert
        // Before sell: Total = 100000, BTC = 2, Avg = 50000
        Assert.That(buy2.Totals.TotalCost, Is.EqualTo(100000m));
        Assert.That(buy2.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(50000m));

        // After sell in FIFO: the cheap lot ($20,000) is sold first
        // Remaining: 1 BTC at $80,000
        // Total = 80000, BTC = 1, Avg = 80000
        //
        // In Brazilian Rule, it would be:
        // Total = 100000 - 50000 = 50000, BTC = 1, Avg = 50000 (stays same)
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(80000m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(1m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(80000m));
    }

    #endregion

    #region Non-Bitcoin Asset Tests (Precision 2)

    [Test]
    public void Should_Calculate_Avg_Price_For_Stock_Asset_With_Precision_2()
    {
        // Arrange: Create a stock profile with precision 2 (e.g., NVDA stock)
        var stockAsset = new AvgPriceAsset("NVDA", 2);
        var profile = AvgPriceProfileBuilder.AFifoProfile()
            .WithAsset(stockAsset)
            .WithIcon(Icon.Empty)
            .WithName("NVDA Stock")
            .Build();

        var strategy = new FifoCalculationStrategy(profile);

        // Buy 10 shares at $500.00
        var line = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity(10m)
            .WithAmount(FiatValue.New(5000m))
            .Build();

        var lines = new List<AvgPriceLine> { line };

        // Act
        strategy.CalculateTotals(lines);

        // Assert
        // Total cost = 10 * 500 = 5000
        // Quantity = 10
        // Avg = 5000 / 10 = 500
        Assert.That(line.Totals.TotalCost, Is.EqualTo(5000m));
        Assert.That(line.Totals.Quantity, Is.EqualTo(10m));
        Assert.That(line.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(500m));
    }

    [Test]
    public void Should_Round_To_Precision_2_For_Stock_Assets()
    {
        // Arrange: Stock with precision 2
        var stockAsset = new AvgPriceAsset("AAPL", 2);
        var profile = AvgPriceProfileBuilder.AFifoProfile()
            .WithAsset(stockAsset)
            .WithIcon(Icon.Empty)
            .WithName("AAPL Stock")
            .Build();

        var strategy = new FifoCalculationStrategy(profile);

        // Buy 3 shares at $175.33
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity(3m)
            .WithAmount(FiatValue.New(525.99m))
            .Build();

        // Buy 7 shares at $180.67
        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity(7m)
            .WithAmount(FiatValue.New(1264.69m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, buy2 };

        // Act
        strategy.CalculateTotals(lines);

        // Assert
        // After buy1: Total = 3 * 175.33 = 525.99, Qty = 3, Avg = 175.33
        Assert.That(buy1.Totals.TotalCost, Is.EqualTo(525.99m));
        Assert.That(buy1.Totals.Quantity, Is.EqualTo(3m));
        Assert.That(buy1.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(175.33m));

        // After buy2: Total = 525.99 + 1264.69 = 1790.68, Qty = 10
        // Avg = 1790.68 / 10 = 179.068 -> rounded to 179.07
        Assert.That(buy2.Totals.TotalCost, Is.EqualTo(1790.68m));
        Assert.That(buy2.Totals.Quantity, Is.EqualTo(10m));
        Assert.That(buy2.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(179.07m));
    }

    [Test]
    public void Should_Handle_Fifo_Sell_For_Stock_Assets()
    {
        // Arrange: Stock with precision 2
        var stockAsset = new AvgPriceAsset("MSFT", 2);
        var profile = AvgPriceProfileBuilder.AFifoProfile()
            .WithAsset(stockAsset)
            .WithIcon(Icon.Empty)
            .WithName("MSFT Stock")
            .Build();

        var strategy = new FifoCalculationStrategy(profile);

        // Buy 100 shares at $300.00 (first lot)
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity(100m)
            .WithAmount(FiatValue.New(30000m))
            .Build();

        // Buy 50 shares at $350.00 (second lot)
        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity(50m)
            .WithAmount(FiatValue.New(17500m))
            .Build();

        // Sell 80 shares (should come from first lot in FIFO)
        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity(80m)
            .WithAmount(FiatValue.New(32000m)) // Sell price doesn't affect cost basis
            .Build();

        var lines = new List<AvgPriceLine> { buy1, buy2, sell };

        // Act
        strategy.CalculateTotals(lines);

        // Assert
        // After sell in FIFO:
        // - First lot: 20 shares remain at $300 (cost = 6000)
        // - Second lot: 50 shares at $350 (cost = 17500)
        // Total cost = 6000 + 17500 = 23500
        // Quantity = 70
        // Avg = 23500 / 70 = 335.714... -> rounded to 335.71
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(23500m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(70m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(335.71m));
    }

    [Test]
    public void Should_Handle_Buy_Sell_Buy_Sequence_For_Stock_Assets()
    {
        // Arrange: Stock with precision 2
        var stockAsset = new AvgPriceAsset("GOOG", 2);
        var profile = AvgPriceProfileBuilder.AFifoProfile()
            .WithAsset(stockAsset)
            .WithIcon(Icon.Empty)
            .WithName("GOOG Stock")
            .Build();

        var strategy = new FifoCalculationStrategy(profile);

        // Buy 50 shares at $140.50
        var buy1 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity(50m)
            .WithAmount(FiatValue.New(7025m))
            .Build();

        // Sell 30 shares
        var sell = AvgPriceLineBuilder.ASellLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity(30m)
            .WithAmount(FiatValue.New(4500m))
            .Build();

        // Buy 25 shares at $145.75
        var buy2 = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 3))
            .WithQuantity(25m)
            .WithAmount(FiatValue.New(3643.75m))
            .Build();

        var lines = new List<AvgPriceLine> { buy1, sell, buy2 };

        // Act
        strategy.CalculateTotals(lines);

        // Assert
        // After buy1: Total = 50 * 140.50 = 7025, Qty = 50, Avg = 140.50
        Assert.That(buy1.Totals.TotalCost, Is.EqualTo(7025m));
        Assert.That(buy1.Totals.Quantity, Is.EqualTo(50m));
        Assert.That(buy1.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(140.50m));

        // After sell: 20 shares remain at $140.50
        // Total = 20 * 140.50 = 2810, Qty = 20, Avg = 140.50
        Assert.That(sell.Totals.TotalCost, Is.EqualTo(2810m));
        Assert.That(sell.Totals.Quantity, Is.EqualTo(20m));
        Assert.That(sell.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(140.50m));

        // After buy2:
        // - First lot: 20 shares at $140.50 (cost = 2810)
        // - Second lot: 25 shares at $145.75 (cost = 3643.75)
        // Total = 2810 + 3643.75 = 6453.75, Qty = 45
        // Avg = 6453.75 / 45 = 143.416... -> rounded to 143.42
        Assert.That(buy2.Totals.TotalCost, Is.EqualTo(6453.75m));
        Assert.That(buy2.Totals.Quantity, Is.EqualTo(45m));
        Assert.That(buy2.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(143.42m));
    }

    [Test]
    public void Should_Handle_Setup_Line_For_Stock_Assets()
    {
        // Arrange: Stock with precision 2
        var stockAsset = new AvgPriceAsset("AMZN", 2);
        var profile = AvgPriceProfileBuilder.AFifoProfile()
            .WithAsset(stockAsset)
            .WithIcon(Icon.Empty)
            .WithName("AMZN Stock")
            .Build();

        var strategy = new FifoCalculationStrategy(profile);

        // Setup: 200 shares at $175.50 avg
        var setup = AvgPriceLineBuilder.ASetupLine()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithQuantity(200m)
            .WithAmount(FiatValue.New(175.50m)) // Avg price per unit
            .Build();

        // Buy 50 shares at $180.25
        var buy = AvgPriceLineBuilder.ABuyLine()
            .WithDate(new DateOnly(2024, 1, 2))
            .WithQuantity(50m)
            .WithAmount(FiatValue.New(9012.5m))
            .Build();

        var lines = new List<AvgPriceLine> { setup, buy };

        // Act
        strategy.CalculateTotals(lines);

        // Assert
        // After setup: Total = 200 * 175.50 = 35100, Qty = 200, Avg = 175.50
        Assert.That(setup.Totals.TotalCost, Is.EqualTo(35100m));
        Assert.That(setup.Totals.Quantity, Is.EqualTo(200m));
        Assert.That(setup.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(175.50m));

        // After buy:
        // - Setup lot: 200 shares at $175.50 (cost = 35100)
        // - Buy lot: 50 shares at $180.25 (cost = 9012.50)
        // Total = 35100 + 9012.50 = 44112.50, Qty = 250
        // Avg = 44112.50 / 250 = 176.45
        Assert.That(buy.Totals.TotalCost, Is.EqualTo(44112.50m));
        Assert.That(buy.Totals.Quantity, Is.EqualTo(250m));
        Assert.That(buy.Totals.AvgCostOfAcquisition.Value, Is.EqualTo(176.45m));
    }

    #endregion
}