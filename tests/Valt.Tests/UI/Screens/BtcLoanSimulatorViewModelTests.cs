using System.Globalization;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Kernel.Factories;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.UI.State;
using Valt.UI.Views.Main.Modals.BtcLoanSimulator;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class BtcLoanSimulatorViewModelTests
{
    private RatesState _ratesState = null!;
    private CurrencySettings _currencySettings = null!;
    private IConfigurationManager _configManager = null!;
    private IClock _clock = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp() => IdGenerator.Configure(new LiteDbIdProvider());

    [SetUp]
    public void SetUp()
    {
        _ratesState = new RatesState();
        _currencySettings = new CurrencySettings(
            Substitute.For<ILocalDatabase>(),
            Substitute.For<INotificationPublisher>());
        _configManager = Substitute.For<IConfigurationManager>();
        _configManager.GetAvailableFiatCurrencies().Returns(new List<string> { "USD", "BRL" });
        _clock = Substitute.For<IClock>();
        _clock.GetCurrentLocalDate().Returns(new DateOnly(2024, 1, 1));
    }

    [TearDown]
    public void TearDown()
    {
        _ratesState?.Dispose();
    }

    private BtcLoanSimulatorViewModel CreateViewModel()
        => new(_currencySettings, _ratesState, _configManager, _clock);

    [Test]
    public void DefaultInterestMode_IsCompound()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.IsSimple, Is.False);
    }

    [Test]
    public void Recalculate_WithValidInputs_ShowsTotalRepay()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        // Act
        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "12";
        viewModel.FeesFiatValue = FiatValue.New(100m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);
        viewModel.IsSimple = true;

        // Assert
        Assert.That(viewModel.HasResults, Is.True);
        Assert.That(viewModel.TotalRepayFiat, Does.Contain("25"));
    }

    [Test]
    public void Recalculate_ProvidesBreakdownAndLiquidationPrice()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        // Act
        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "12";
        viewModel.FeesFiatValue = FiatValue.New(100m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);
        viewModel.IsSimple = true;

        // Assert
        Assert.That(viewModel.TotalRepayFiat, Is.Not.Empty);
        Assert.That(viewModel.PrincipalFiat, Does.Contain("25"));
        Assert.That(viewModel.InterestFiat, Is.Not.Empty);
        Assert.That(viewModel.FeesFiat, Does.Contain("100"));
        Assert.That(viewModel.LiquidationPriceFiat, Does.Contain("31"));
    }

    [Test]
    public void Recalculate_WhenBtcPriceAvailable_ShowsSatsAndConversionBasis()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        // Act
        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "12";
        viewModel.FeesFiatValue = FiatValue.New(0m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);

        // Assert
        Assert.That(viewModel.IsBtcPriceAvailable, Is.True);
        Assert.That(viewModel.TotalRepaySats, Is.Not.Empty);
        Assert.That(viewModel.PrincipalSats, Is.Not.Empty);
        Assert.That(viewModel.InterestSats, Is.Not.Empty);
        Assert.That(viewModel.ConversionBasis, Does.Contain("100,000").Or.Contains("100000"));
    }

    [Test]
    public void Recalculate_WhenBtcPriceUnavailable_HidesSatsAndShowsFallback()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = null;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        // Act
        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "12";
        viewModel.FeesFiatValue = FiatValue.New(0m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);

        // Assert
        Assert.That(viewModel.IsBtcPriceAvailable, Is.False);
        Assert.That(viewModel.TotalRepaySats, Is.Empty);
        Assert.That(viewModel.PrincipalSats, Is.Empty);
        Assert.That(viewModel.ConversionBasis, Does.Contain("unavailable"));
    }

    [Test]
    public void Recalculate_WithInvalidInputs_ClearsResults()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        // Act
        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.Empty;
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "12";
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);

        // Assert
        Assert.That(viewModel.HasResults, Is.False);
        Assert.That(viewModel.TotalRepayFiat, Is.Empty);
    }

    #region SIM-08 Effective APR

    [Test]
    public void EffectiveApr_WithZeroFeesAnd365DayTerm_IsNominalApr()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "12";
        viewModel.FeesFiatValue = FiatValue.New(0m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 12, 31);
        viewModel.IsSimple = true;

        // Assert
        Assert.That(viewModel.EffectiveAprText, Is.EqualTo("12.00%"));
    }

    #endregion

    #region SIM-09 Distance to liquidation

    [Test]
    public void DistanceToLiquidation_WhenPriceAboveLiquidation_ShowsPositivePercentage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "0";
        viewModel.FeesFiatValue = FiatValue.New(0m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);

        // Liquidation price = 25,000 / (1 * 0.8) = 31,250
        // Distance = (100,000 - 31,250) / 31,250 = 2.20 => +220.00%
        Assert.That(viewModel.DistanceToLiquidation, Does.StartWith("+"));
        Assert.That(viewModel.DistanceToLiquidation, Does.Contain("220"));
    }

    [Test]
    public void DistanceToLiquidationColor_IsRed_WhenPriceAtOrBelowLiquidation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 31_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "0";
        viewModel.FeesFiatValue = FiatValue.New(0m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);

        Assert.That(viewModel.DistanceToLiquidationColor, Is.EqualTo("#F44336"));
    }

    [Test]
    public void DistanceToLiquidationColor_IsGreen_WhenPriceAboveLiquidation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "0";
        viewModel.FeesFiatValue = FiatValue.New(0m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);

        Assert.That(viewModel.DistanceToLiquidationColor, Is.EqualTo("#4CAF50"));
    }

    #endregion

    #region Edge-case live recalc

    [Test]
    public void CurrencyChange_TriggersRecalculate_AndUpdatesCurrencyCode()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal>
        {
            ["USD"] = 1m,
            ["BRL"] = 5m
        };

        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "12";
        viewModel.FeesFiatValue = FiatValue.New(0m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);

        var usdTotal = viewModel.TotalRepayFiat;

        // Act
        viewModel.SelectedCurrency = FiatCurrency.Brl;

        // Assert
        Assert.That(viewModel.CurrencyCode, Is.EqualTo("BRL"));
        Assert.That(viewModel.CurrencySymbol, Is.EqualTo("R$"));
        Assert.That(viewModel.TotalRepayFiat, Is.Not.EqualTo(usdTotal));
    }

    [Test]
    public void InterestModeToggle_CompoundProducesHigherTotalThanSimple()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "12";
        viewModel.FeesFiatValue = FiatValue.New(0m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 4, 1); // 90 days
        viewModel.IsSimple = true;

        var simpleTotal = viewModel.TotalRepayFiat;

        // Act
        viewModel.IsSimple = false;

        // Assert
        Assert.That(viewModel.TotalRepayFiat, Is.Not.EqualTo(simpleTotal));
        var simpleValue = decimal.Parse(simpleTotal.Replace("$", "").Replace(",", "").Trim(), CultureInfo.InvariantCulture);
        var compoundValue = decimal.Parse(viewModel.TotalRepayFiat.Replace("$", "").Replace(",", "").Trim(), CultureInfo.InvariantCulture);
        Assert.That(compoundValue, Is.GreaterThan(simpleValue));
    }

    [Test]
    public void InvalidInputs_ClearResults([Values] InvalidInputScenario scenario)
    {
        // Arrange
        var viewModel = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        viewModel.SelectedCurrency = FiatCurrency.Usd;
        viewModel.CollateralBtcValue = BtcValue.ParseBitcoin(1m);
        viewModel.AmountTakenFiatValue = FiatValue.New(25_000m);
        viewModel.LiquidationLtvText = "80";
        viewModel.AprText = "12";
        viewModel.FeesFiatValue = FiatValue.New(0m);
        viewModel.StartDate = new DateTime(2024, 1, 1);
        viewModel.EndDate = new DateTime(2024, 1, 31);

        Assert.That(viewModel.HasResults, Is.True, "precondition: valid inputs should yield results");

        // Act
        switch (scenario)
        {
            case InvalidInputScenario.EmptyCollateral:
                viewModel.CollateralBtcValue = BtcValue.Empty;
                break;
            case InvalidInputScenario.EmptyAmount:
                viewModel.AmountTakenFiatValue = FiatValue.Empty;
                break;
            case InvalidInputScenario.EndDateBeforeStart:
                viewModel.EndDate = viewModel.StartDate;
                break;
            case InvalidInputScenario.LtvOutOfRange:
                viewModel.LiquidationLtvText = "101";
                break;
        }

        // Assert
        Assert.That(viewModel.HasResults, Is.False);
    }

    #endregion
}

public enum InvalidInputScenario
{
    EmptyCollateral,
    EmptyAmount,
    EndDateBeforeStart,
    LtvOutOfRange
}
