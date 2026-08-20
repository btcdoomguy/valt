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
}
