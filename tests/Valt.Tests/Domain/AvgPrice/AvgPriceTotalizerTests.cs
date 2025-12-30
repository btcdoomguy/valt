using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Core.Modules.AvgPrice.Exceptions;
using Valt.Infra.Modules.AvgPrice;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.AvgPrice;

[TestFixture]
public class AvgPriceTotalizerTests : DatabaseTest
{
    private AvgPriceTotalizer _totalizer = null!;
    private AvgPriceRepository _repository = null!;

    [SetUp]
    public new Task SetUp()
    {
        base.SetUp();
        _totalizer = new AvgPriceTotalizer(_localDatabase);
        _repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);
        return Task.CompletedTask;
    }

    #region Empty and Basic Cases

    [Test]
    public async Task GetTotals_Should_Return_Empty_Result_When_No_Profiles_Provided()
    {
        // Arrange
        var profileIds = Enumerable.Empty<AvgPriceProfileId>();

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, profileIds);

        // Assert
        Assert.That(result.Year, Is.EqualTo(2024));
        Assert.That(result.MonthlyTotals.Count(), Is.EqualTo(12));
        Assert.That(result.YearlyTotals.AmountBought, Is.EqualTo(0));
        Assert.That(result.YearlyTotals.AmountSold, Is.EqualTo(0));
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(0));
        Assert.That(result.YearlyTotals.Volume, Is.EqualTo(0));
    }

    [Test]
    public async Task GetTotals_Should_Return_Empty_Result_When_Profile_Has_No_Lines()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Empty Profile",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        Assert.That(result.Year, Is.EqualTo(2024));
        Assert.That(result.YearlyTotals.AmountBought, Is.EqualTo(0));
        Assert.That(result.YearlyTotals.AmountSold, Is.EqualTo(0));
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(0));
        Assert.That(result.YearlyTotals.Volume, Is.EqualTo(0));
    }

    #endregion

    #region Currency Validation

    [Test]
    public void GetTotals_Should_Throw_When_Profiles_Have_Different_Currencies()
    {
        // Arrange
        var usdProfile = AvgPriceProfile.New(
            "USD Profile",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        var brlProfile = AvgPriceProfile.New(
            "BRL Profile",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        _repository.SaveAvgPriceProfileAsync(usdProfile).Wait();
        _repository.SaveAvgPriceProfileAsync(brlProfile).Wait();

        // Act & Assert
        Assert.ThrowsAsync<MixedCurrencyException>(async () =>
            await _totalizer.GetTotalsAsync(2024, new[] { usdProfile.Id, brlProfile.Id }));
    }

    [Test]
    public async Task GetTotals_Should_Work_When_Multiple_Profiles_Have_Same_Currency()
    {
        // Arrange
        var profile1 = AvgPriceProfile.New(
            "Profile 1",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        var profile2 = AvgPriceProfile.New(
            "Profile 2",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        await _repository.SaveAvgPriceProfileAsync(profile1);
        await _repository.SaveAvgPriceProfileAsync(profile2);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile1.Id, profile2.Id });

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Year, Is.EqualTo(2024));
    }

    #endregion

    #region Buy Operations

    [Test]
    public async Task GetTotals_Should_Calculate_AmountBought_For_Single_Buy()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Buy Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 3, 15),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(50000m),
            "March buy");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        Assert.That(result.YearlyTotals.AmountBought, Is.EqualTo(50000m));
        Assert.That(result.YearlyTotals.AmountSold, Is.EqualTo(0));
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(0));
        Assert.That(result.YearlyTotals.Volume, Is.EqualTo(50000m));
    }

    [Test]
    public async Task GetTotals_Should_Calculate_AmountBought_For_Multiple_Buys()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Multiple Buys",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 1, 10),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(40000m),
            "January buy");

        profile.AddLine(
            new DateOnly(2024, 3, 20),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            0.5m,
            FiatValue.New(25000m),
            "March buy");

        profile.AddLine(
            new DateOnly(2024, 6, 5),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            2m,
            FiatValue.New(90000m),
            "June buy");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        Assert.That(result.YearlyTotals.AmountBought, Is.EqualTo(155000m));
        Assert.That(result.YearlyTotals.Volume, Is.EqualTo(155000m));
    }

    #endregion

    #region Sell Operations

    [Test]
    public async Task GetTotals_Should_Calculate_AmountSold_For_Single_Sell()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Sell Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            2m,
            FiatValue.New(80000m),
            "Initial buy");

        profile.AddLine(
            new DateOnly(2024, 6, 15),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(55000m),
            "June sell");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        Assert.That(result.YearlyTotals.AmountBought, Is.EqualTo(80000m));
        Assert.That(result.YearlyTotals.AmountSold, Is.EqualTo(55000m));
        Assert.That(result.YearlyTotals.Volume, Is.EqualTo(135000m));
    }

    #endregion

    #region Profit/Loss Calculations

    [Test]
    public async Task GetTotals_Should_Calculate_Profit_When_Selling_At_Higher_Price()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Profit Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        // Buy 1 BTC at $40,000 (avg cost = $40,000)
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(40000m),
            "Initial buy");

        // Sell 1 BTC at $50,000 (profit = $50,000 - $40,000 = $10,000)
        profile.AddLine(
            new DateOnly(2024, 6, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(50000m),
            "Sell at profit");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(10000m));
    }

    [Test]
    public async Task GetTotals_Should_Calculate_Loss_When_Selling_At_Lower_Price()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Loss Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        // Buy 1 BTC at $50,000 (avg cost = $50,000)
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(50000m),
            "Initial buy");

        // Sell 1 BTC at $35,000 (loss = $35,000 - $50,000 = -$15,000)
        profile.AddLine(
            new DateOnly(2024, 6, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(35000m),
            "Sell at loss");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(-15000m));
    }

    [Test]
    public async Task GetTotals_Should_Calculate_Profit_With_Multiple_Buys_And_Partial_Sell()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Partial Sell Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        // Buy 1 BTC at $40,000
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(40000m),
            "First buy");

        // Buy 1 BTC at $60,000 (avg cost = ($40,000 + $60,000) / 2 = $50,000)
        profile.AddLine(
            new DateOnly(2024, 2, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(60000m),
            "Second buy");

        // Sell 1 BTC at $55,000 (profit = $55,000 - $50,000 = $5,000)
        profile.AddLine(
            new DateOnly(2024, 3, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(55000m),
            "Partial sell");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(5000m));
    }

    #endregion

    #region Monthly Totals

    [Test]
    public async Task GetTotals_Should_Return_Monthly_Breakdown()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Monthly Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 1, 15),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(40000m),
            "January");

        profile.AddLine(
            new DateOnly(2024, 3, 10),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            0.5m,
            FiatValue.New(25000m),
            "March");

        profile.AddLine(
            new DateOnly(2024, 6, 20),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            2m,
            FiatValue.New(100000m),
            "June");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        var monthlyList = result.MonthlyTotals.ToList();
        Assert.That(monthlyList.Count, Is.EqualTo(12));

        var january = monthlyList.First(m => m.Month.Month == 1);
        Assert.That(january.Values.AmountBought, Is.EqualTo(40000m));

        var march = monthlyList.First(m => m.Month.Month == 3);
        Assert.That(march.Values.AmountBought, Is.EqualTo(25000m));

        var june = monthlyList.First(m => m.Month.Month == 6);
        Assert.That(june.Values.AmountBought, Is.EqualTo(100000m));

        // Other months should be zero
        var february = monthlyList.First(m => m.Month.Month == 2);
        Assert.That(february.Values.AmountBought, Is.EqualTo(0));
    }

    [Test]
    public async Task GetTotals_Should_Calculate_Monthly_Profit_Loss()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Monthly P/L Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        // Buy in January
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            2m,
            FiatValue.New(80000m),
            "January buy");

        // Sell in March at profit
        profile.AddLine(
            new DateOnly(2024, 3, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(50000m),
            "March sell");

        // Sell in June at profit
        profile.AddLine(
            new DateOnly(2024, 6, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(45000m),
            "June sell");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        var monthlyList = result.MonthlyTotals.ToList();

        // Avg cost = $80,000 / 2 = $40,000
        // March: profit = $50,000 - $40,000 = $10,000
        var march = monthlyList.First(m => m.Month.Month == 3);
        Assert.That(march.Values.TotalProfitLoss, Is.EqualTo(10000m));

        // June: profit = $45,000 - $40,000 = $5,000
        var june = monthlyList.First(m => m.Month.Month == 6);
        Assert.That(june.Values.TotalProfitLoss, Is.EqualTo(5000m));

        // Total yearly profit = $15,000
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(15000m));
    }

    #endregion

    #region Year Filtering

    [Test]
    public async Task GetTotals_Should_Only_Include_Lines_From_Specified_Year()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Year Filter Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2023, 6, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(30000m),
            "2023 buy");

        profile.AddLine(
            new DateOnly(2024, 3, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            2m,
            FiatValue.New(80000m),
            "2024 buy");

        profile.AddLine(
            new DateOnly(2025, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            0.5m,
            FiatValue.New(50000m),
            "2025 buy");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert - Only 2024 values should be included
        Assert.That(result.Year, Is.EqualTo(2024));
        Assert.That(result.YearlyTotals.AmountBought, Is.EqualTo(80000m));
    }

    [Test]
    public async Task GetTotals_Should_Use_Historical_Data_For_ProfitLoss_Calculation()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Historical Avg Cost Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        // Buy in 2023 - this establishes avg cost for 2024 sells
        profile.AddLine(
            new DateOnly(2023, 6, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            2m,
            FiatValue.New(60000m),
            "2023 buy");

        // Sell in 2024 - should use avg cost from 2023 buy ($30,000)
        profile.AddLine(
            new DateOnly(2024, 3, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(50000m),
            "2024 sell");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        // 2024 should not include the 2023 buy amount
        Assert.That(result.YearlyTotals.AmountBought, Is.EqualTo(0));
        Assert.That(result.YearlyTotals.AmountSold, Is.EqualTo(50000m));

        // Profit should be calculated using historical avg cost ($30,000)
        // Profit = $50,000 - $30,000 = $20,000
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(20000m));
    }

    #endregion

    #region Multiple Profiles

    [Test]
    public async Task GetTotals_Should_Aggregate_Multiple_Profiles_With_Same_Currency()
    {
        // Arrange
        var profile1 = AvgPriceProfile.New(
            "Profile 1",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        profile1.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(40000m),
            "Profile 1 buy");

        var profile2 = AvgPriceProfile.New(
            "Profile 2",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        profile2.AddLine(
            new DateOnly(2024, 2, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            2m,
            FiatValue.New(100000m),
            "Profile 2 buy");

        await _repository.SaveAvgPriceProfileAsync(profile1);
        await _repository.SaveAvgPriceProfileAsync(profile2);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile1.Id, profile2.Id });

        // Assert
        Assert.That(result.YearlyTotals.AmountBought, Is.EqualTo(140000m));
        Assert.That(result.YearlyTotals.Volume, Is.EqualTo(140000m));
    }

    [Test]
    public async Task GetTotals_Should_Track_ProfitLoss_Per_Profile_Independently()
    {
        // Arrange
        var profile1 = AvgPriceProfile.New(
            "Profile 1",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        // Profile 1: Buy at $40k, sell at $50k = $10k profit
        profile1.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(40000m),
            "P1 buy");

        profile1.AddLine(
            new DateOnly(2024, 2, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(50000m),
            "P1 sell");

        var profile2 = AvgPriceProfile.New(
            "Profile 2",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        // Profile 2: Buy at $60k, sell at $55k = -$5k loss
        profile2.AddLine(
            new DateOnly(2024, 3, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Buy,
            1m,
            FiatValue.New(60000m),
            "P2 buy");

        profile2.AddLine(
            new DateOnly(2024, 4, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(55000m),
            "P2 sell");

        await _repository.SaveAvgPriceProfileAsync(profile1);
        await _repository.SaveAvgPriceProfileAsync(profile2);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile1.Id, profile2.Id });

        // Assert
        // Total P/L = $10k - $5k = $5k
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(5000m));
    }

    #endregion

    #region Setup Line Operations

    [Test]
    public async Task GetTotals_Should_Treat_Setup_As_Buy_For_AmountBought()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Setup Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Setup,
            2m,
            FiatValue.New(80000m),
            "Initial setup");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        Assert.That(result.YearlyTotals.AmountBought, Is.EqualTo(80000m));
        Assert.That(result.YearlyTotals.Volume, Is.EqualTo(80000m));
    }

    [Test]
    public async Task GetTotals_Should_Use_Setup_For_AvgCost_Calculation()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Setup Avg Cost Test",
            asset: AvgPriceAsset.Bitcoin,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        // Setup 2 BTC at avg $40k each (total $80k / 2 = $40k avg)
        profile.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Setup,
            2m,
            FiatValue.New(80000m),
            "Setup");

        // Sell 1 BTC at $50k (profit = $50k - $40k = $10k)
        profile.AddLine(
            new DateOnly(2024, 2, 1),
            displayOrder: 0,
            AvgPriceLineTypes.Sell,
            1m,
            FiatValue.New(50000m),
            "Sell");

        await _repository.SaveAvgPriceProfileAsync(profile);

        // Act
        var result = await _totalizer.GetTotalsAsync(2024, new[] { profile.Id });

        // Assert
        Assert.That(result.YearlyTotals.TotalProfitLoss, Is.EqualTo(10000m));
    }

    #endregion
}
