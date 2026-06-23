using NSubstitute;
using Valt.App.Modules.Assets.Commands.CreateBasicAsset;
using Valt.App.Modules.Assets.Commands.CreateBtcLending;
using Valt.App.Modules.Assets.Commands.CreateBtcLoan;
using Valt.App.Modules.Assets.Commands.CreateLeveragedPosition;
using Valt.App.Modules.Assets.Commands.CreateRealEstateAsset;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Infra.Modules.Assets.PriceProviders;
using Valt.UI.Services;
using Valt.UI.Services.Exceptions;

namespace Valt.Tests.UI.Services;

[TestFixture]
public class AssetFormBuilderTests
{
    private AssetFormBuilder _builder = null!;
    private IAssetPriceProviderSelector _priceProviderSelector = null!;

    [SetUp]
    public void SetUp()
    {
        _priceProviderSelector = Substitute.For<IAssetPriceProviderSelector>();
        _builder = new AssetFormBuilder(_priceProviderSelector);
    }

    #region Snapshots

    private static AssetFormSnapshot BasicSnapshot(string assetType = "Stock", string priceSource = "Manual") => new(
        Name: "AAPL",
        SelectedAssetType: assetType,
        SelectedCurrency: "USD",
        IncludeInNetWorth: true,
        Visible: true,
        Symbol: "AAPL",
        Quantity: 10,
        CurrentPriceFiat: FiatValue.New(200m),
        SelectedPriceSource: priceSource,
        Address: string.Empty,
        CurrentValueFiat: FiatValue.Empty,
        MonthlyRentalIncomeFiat: FiatValue.Empty,
        AcquisitionDate: new DateTime(2024, 1, 15),
        AcquisitionPriceFiat: FiatValue.New(150m),
        IsBitcoinUnderlyingAsset: false,
        CollateralFiat: FiatValue.Empty,
        EntryPriceFiat: FiatValue.Empty,
        Leverage: 1,
        LiquidationPriceFiat: FiatValue.Empty,
        IsLong: true,
        UseExactPosition: false,
        PositionSize: 0,
        PlatformName: string.Empty,
        CollateralSats: 0,
        LoanAmountFiat: FiatValue.Empty,
        AprPercentage: 0,
        InitialLtvPercentage: 0,
        LiquidationLtvPercentage: 0,
        MarginCallLtvPercentage: 0,
        FeesFiat: FiatValue.Empty,
        LoanStartDate: null,
        RepaymentDateOffset: null,
        IsIndefiniteLoan: false,
        UseFixedTotalDebt: false,
        FixedTotalDebtFiat: FiatValue.Empty,
        BorrowerOrPlatformName: string.Empty,
        AmountLentFiat: FiatValue.Empty,
        LendingAprPercentage: 0,
        LendingStartDateOffset: null,
        ExpectedRepaymentDateOffset: null,
        IsIndefiniteLending: false);

    private static AssetFormSnapshot RealEstateSnapshot() => new(
        Name: "Beach House",
        SelectedAssetType: AssetTypes.RealEstate.ToString(),
        SelectedCurrency: "USD",
        IncludeInNetWorth: true,
        Visible: true,
        Symbol: string.Empty,
        Quantity: 0,
        CurrentPriceFiat: FiatValue.Empty,
        SelectedPriceSource: AssetPriceSource.Manual.ToString(),
        Address: "123 Ocean Dr",
        CurrentValueFiat: FiatValue.New(500_000m),
        MonthlyRentalIncomeFiat: FiatValue.New(2000m),
        AcquisitionDate: new DateTime(2020, 6, 1),
        AcquisitionPriceFiat: FiatValue.New(400_000m),
        IsBitcoinUnderlyingAsset: false,
        CollateralFiat: FiatValue.Empty,
        EntryPriceFiat: FiatValue.Empty,
        Leverage: 1,
        LiquidationPriceFiat: FiatValue.Empty,
        IsLong: true,
        UseExactPosition: false,
        PositionSize: 0,
        PlatformName: string.Empty,
        CollateralSats: 0,
        LoanAmountFiat: FiatValue.Empty,
        AprPercentage: 0,
        InitialLtvPercentage: 0,
        LiquidationLtvPercentage: 0,
        MarginCallLtvPercentage: 0,
        FeesFiat: FiatValue.Empty,
        LoanStartDate: null,
        RepaymentDateOffset: null,
        IsIndefiniteLoan: false,
        UseFixedTotalDebt: false,
        FixedTotalDebtFiat: FiatValue.Empty,
        BorrowerOrPlatformName: string.Empty,
        AmountLentFiat: FiatValue.Empty,
        LendingAprPercentage: 0,
        LendingStartDateOffset: null,
        ExpectedRepaymentDateOffset: null,
        IsIndefiniteLending: false);

    private static AssetFormSnapshot LeveragedSnapshot(bool useExactPosition = false, string priceSource = "Manual") => new(
        Name: "BTC-PERP",
        SelectedAssetType: AssetTypes.LeveragedPosition.ToString(),
        SelectedCurrency: "USD",
        IncludeInNetWorth: true,
        Visible: true,
        Symbol: "BTC-PERP",
        Quantity: 0,
        CurrentPriceFiat: FiatValue.New(60_000m),
        SelectedPriceSource: priceSource,
        Address: string.Empty,
        CurrentValueFiat: FiatValue.Empty,
        MonthlyRentalIncomeFiat: FiatValue.Empty,
        AcquisitionDate: null,
        AcquisitionPriceFiat: FiatValue.Empty,
        IsBitcoinUnderlyingAsset: false,
        CollateralFiat: FiatValue.New(10_000m),
        EntryPriceFiat: FiatValue.New(50_000m),
        Leverage: 5,
        LiquidationPriceFiat: FiatValue.New(40_000m),
        IsLong: true,
        UseExactPosition: useExactPosition,
        PositionSize: useExactPosition ? 1m : 0,
        PlatformName: string.Empty,
        CollateralSats: 0,
        LoanAmountFiat: FiatValue.Empty,
        AprPercentage: 0,
        InitialLtvPercentage: 0,
        LiquidationLtvPercentage: 0,
        MarginCallLtvPercentage: 0,
        FeesFiat: FiatValue.Empty,
        LoanStartDate: null,
        RepaymentDateOffset: null,
        IsIndefiniteLoan: false,
        UseFixedTotalDebt: false,
        FixedTotalDebtFiat: FiatValue.Empty,
        BorrowerOrPlatformName: string.Empty,
        AmountLentFiat: FiatValue.Empty,
        LendingAprPercentage: 0,
        LendingStartDateOffset: null,
        ExpectedRepaymentDateOffset: null,
        IsIndefiniteLending: false);

    private static AssetFormSnapshot BtcLoanSnapshot(bool useFixedTotalDebt = false) => new(
        Name: "HodlHodl Loan",
        SelectedAssetType: AssetTypes.BtcLoan.ToString(),
        SelectedCurrency: "USD",
        IncludeInNetWorth: true,
        Visible: true,
        Symbol: string.Empty,
        Quantity: 0,
        CurrentPriceFiat: FiatValue.Empty,
        SelectedPriceSource: AssetPriceSource.Manual.ToString(),
        Address: string.Empty,
        CurrentValueFiat: FiatValue.Empty,
        MonthlyRentalIncomeFiat: FiatValue.Empty,
        AcquisitionDate: null,
        AcquisitionPriceFiat: FiatValue.Empty,
        IsBitcoinUnderlyingAsset: false,
        CollateralFiat: FiatValue.Empty,
        EntryPriceFiat: FiatValue.Empty,
        Leverage: 1,
        LiquidationPriceFiat: FiatValue.Empty,
        IsLong: true,
        UseExactPosition: false,
        PositionSize: 0,
        PlatformName: "HodlHodl",
        CollateralSats: 1_000_000,
        LoanAmountFiat: FiatValue.New(50_000m),
        AprPercentage: 12m,
        InitialLtvPercentage: 50m,
        LiquidationLtvPercentage: 80m,
        MarginCallLtvPercentage: 70m,
        FeesFiat: FiatValue.New(500m),
        LoanStartDate: new DateTime(2024, 1, 1),
        RepaymentDateOffset: useFixedTotalDebt ? new DateTime(2024, 12, 31) : null,
        IsIndefiniteLoan: !useFixedTotalDebt,
        UseFixedTotalDebt: useFixedTotalDebt,
        FixedTotalDebtFiat: useFixedTotalDebt ? FiatValue.New(55_000m) : FiatValue.Empty,
        BorrowerOrPlatformName: string.Empty,
        AmountLentFiat: FiatValue.Empty,
        LendingAprPercentage: 0,
        LendingStartDateOffset: null,
        ExpectedRepaymentDateOffset: null,
        IsIndefiniteLending: false);

    private static AssetFormSnapshot BtcLendingSnapshot() => new(
        Name: "Lending Position",
        SelectedAssetType: AssetTypes.BtcLending.ToString(),
        SelectedCurrency: "USD",
        IncludeInNetWorth: true,
        Visible: true,
        Symbol: string.Empty,
        Quantity: 0,
        CurrentPriceFiat: FiatValue.Empty,
        SelectedPriceSource: AssetPriceSource.Manual.ToString(),
        Address: string.Empty,
        CurrentValueFiat: FiatValue.Empty,
        MonthlyRentalIncomeFiat: FiatValue.Empty,
        AcquisitionDate: null,
        AcquisitionPriceFiat: FiatValue.Empty,
        IsBitcoinUnderlyingAsset: false,
        CollateralFiat: FiatValue.Empty,
        EntryPriceFiat: FiatValue.Empty,
        Leverage: 1,
        LiquidationPriceFiat: FiatValue.Empty,
        IsLong: true,
        UseExactPosition: false,
        PositionSize: 0,
        PlatformName: string.Empty,
        CollateralSats: 0,
        LoanAmountFiat: FiatValue.Empty,
        AprPercentage: 0,
        InitialLtvPercentage: 0,
        LiquidationLtvPercentage: 0,
        MarginCallLtvPercentage: 0,
        FeesFiat: FiatValue.Empty,
        LoanStartDate: null,
        RepaymentDateOffset: null,
        IsIndefiniteLoan: false,
        UseFixedTotalDebt: false,
        FixedTotalDebtFiat: FiatValue.Empty,
        BorrowerOrPlatformName: "Borrower",
        AmountLentFiat: FiatValue.New(10_000m),
        LendingAprPercentage: 10m,
        LendingStartDateOffset: new DateTime(2024, 1, 1),
        ExpectedRepaymentDateOffset: new DateTime(2024, 12, 31),
        IsIndefiniteLending: false);

    #endregion

    #region BuildCreateCommandAsync Tests

    [Test]
    public async Task BuildCreateCommandAsync_Stock_ReturnsBasicAssetCommand()
    {
        var snapshot = BasicSnapshot("Stock");

        var envelope = await _builder.BuildCreateCommandAsync(snapshot);

        Assert.That(envelope, Is.TypeOf<BasicAssetCommandEnvelope>());
        var command = ((BasicAssetCommandEnvelope)envelope).Command;
        Assert.That(command.AssetType, Is.EqualTo((int)AssetTypes.Stock));
        Assert.That(command.Symbol, Is.EqualTo("AAPL"));
        Assert.That(command.Quantity, Is.EqualTo(10));
        Assert.That(command.CurrentPrice, Is.EqualTo(200m));
        Assert.That(command.PriceSource, Is.EqualTo((int)AssetPriceSource.Manual));
        Assert.That(command.AcquisitionDate, Is.EqualTo(new DateOnly(2024, 1, 15)));
        Assert.That(command.AcquisitionPrice, Is.EqualTo(150m));
    }

    [Test]
    public async Task BuildCreateCommandAsync_BasicFetchesPrice_WhenNotManual()
    {
        var snapshot = BasicSnapshot("Stock", "YahooFinance");
        _priceProviderSelector.GetPriceAsync(AssetPriceSource.YahooFinance, "AAPL", "USD")
            .Returns(new AssetPriceResult(210m, "USD", DateTime.UtcNow));

        var envelope = await _builder.BuildCreateCommandAsync(snapshot);

        var command = ((BasicAssetCommandEnvelope)envelope).Command;
        Assert.That(command.CurrentPrice, Is.EqualTo(210m));
    }

    [Test]
    public async Task BuildCreateCommandAsync_RealEstate_ReturnsRealEstateCommand()
    {
        var snapshot = RealEstateSnapshot();

        var envelope = await _builder.BuildCreateCommandAsync(snapshot);

        Assert.That(envelope, Is.TypeOf<RealEstateAssetCommandEnvelope>());
        var command = ((RealEstateAssetCommandEnvelope)envelope).Command;
        Assert.That(command.CurrentValue, Is.EqualTo(500_000m));
        Assert.That(command.Address, Is.EqualTo("123 Ocean Dr"));
        Assert.That(command.MonthlyRentalIncome, Is.EqualTo(2000m));
        Assert.That(command.AcquisitionDate, Is.EqualTo(new DateOnly(2020, 6, 1)));
        Assert.That(command.AcquisitionPrice, Is.EqualTo(400_000m));
    }

    [Test]
    public async Task BuildCreateCommandAsync_LeveragedCollateral_ReturnsLeveragedCommand()
    {
        var snapshot = LeveragedSnapshot();

        var envelope = await _builder.BuildCreateCommandAsync(snapshot);

        Assert.That(envelope, Is.TypeOf<LeveragedPositionCommandEnvelope>());
        var command = ((LeveragedPositionCommandEnvelope)envelope).Command;
        Assert.That(command.Symbol, Is.EqualTo("BTC-PERP"));
        Assert.That(command.Collateral, Is.EqualTo(10_000m));
        Assert.That(command.EntryPrice, Is.EqualTo(50_000m));
        Assert.That(command.CurrentPrice, Is.EqualTo(60_000m));
        Assert.That(command.Leverage, Is.EqualTo(5));
        Assert.That(command.LiquidationPrice, Is.EqualTo(40_000m));
        Assert.That(command.IsLong, Is.True);
        Assert.That(command.InputMode, Is.EqualTo(0));
        Assert.That(command.PositionSize, Is.Null);
    }

    [Test]
    public async Task BuildCreateCommandAsync_LeveragedExactPosition_ReturnsLeveragedCommandWithPositionSize()
    {
        var snapshot = LeveragedSnapshot(useExactPosition: true);

        var envelope = await _builder.BuildCreateCommandAsync(snapshot);

        var command = ((LeveragedPositionCommandEnvelope)envelope).Command;
        Assert.That(command.InputMode, Is.EqualTo(1));
        Assert.That(command.PositionSize, Is.EqualTo(1m));
    }

    [Test]
    public async Task BuildCreateCommandAsync_LeveragedFetchesPrice_WhenNotManual()
    {
        var snapshot = LeveragedSnapshot(priceSource: "YahooFinance");
        _priceProviderSelector.GetPriceAsync(AssetPriceSource.YahooFinance, "BTC-PERP", "USD")
            .Returns(new AssetPriceResult(65_000m, "USD", DateTime.UtcNow));

        var envelope = await _builder.BuildCreateCommandAsync(snapshot);

        var command = ((LeveragedPositionCommandEnvelope)envelope).Command;
        Assert.That(command.CurrentPrice, Is.EqualTo(65_000m));
    }

    [Test]
    public async Task BuildCreateCommandAsync_BtcLoan_ReturnsBtcLoanCommand()
    {
        var snapshot = BtcLoanSnapshot();
        _priceProviderSelector.GetPriceAsync(AssetPriceSource.LivePrice, "BTC", "USD")
            .Returns(new AssetPriceResult(100_000m, "USD", DateTime.UtcNow));

        var envelope = await _builder.BuildCreateCommandAsync(snapshot);

        Assert.That(envelope, Is.TypeOf<BtcLoanCommandEnvelope>());
        var command = ((BtcLoanCommandEnvelope)envelope).Command;
        Assert.That(command.PlatformName, Is.EqualTo("HodlHodl"));
        Assert.That(command.CollateralSats, Is.EqualTo(1_000_000));
        Assert.That(command.LoanAmount, Is.EqualTo(50_000m));
        Assert.That(command.Apr, Is.EqualTo(0.12m));
        Assert.That(command.InitialLtv, Is.EqualTo(50m));
        Assert.That(command.LiquidationLtv, Is.EqualTo(80m));
        Assert.That(command.MarginCallLtv, Is.EqualTo(70m));
        Assert.That(command.Fees, Is.EqualTo(500m));
        Assert.That(command.LoanStartDate, Is.EqualTo(new DateOnly(2024, 1, 1)));
        Assert.That(command.RepaymentDate, Is.Null);
        Assert.That(command.CurrentBtcPrice, Is.EqualTo(100_000m));
        Assert.That(command.FixedTotalDebt, Is.Null);
    }

    [Test]
    public async Task BuildCreateCommandAsync_BtcLoanFixedTotalDebt_SetsAprToZero()
    {
        var snapshot = BtcLoanSnapshot(useFixedTotalDebt: true);
        _priceProviderSelector.GetPriceAsync(AssetPriceSource.LivePrice, "BTC", "USD")
            .Returns(new AssetPriceResult(100_000m, "USD", DateTime.UtcNow));

        var envelope = await _builder.BuildCreateCommandAsync(snapshot);

        var command = ((BtcLoanCommandEnvelope)envelope).Command;
        Assert.That(command.Apr, Is.EqualTo(0m));
        Assert.That(command.FixedTotalDebt, Is.EqualTo(55_000m));
        Assert.That(command.RepaymentDate, Is.EqualTo(new DateOnly(2024, 12, 31)));
    }

    [Test]
    public async Task BuildCreateCommandAsync_BtcLending_ReturnsBtcLendingCommand()
    {
        var snapshot = BtcLendingSnapshot();

        var envelope = await _builder.BuildCreateCommandAsync(snapshot);

        Assert.That(envelope, Is.TypeOf<BtcLendingCommandEnvelope>());
        var command = ((BtcLendingCommandEnvelope)envelope).Command;
        Assert.That(command.AmountLent, Is.EqualTo(10_000m));
        Assert.That(command.Apr, Is.EqualTo(0.10m));
        Assert.That(command.BorrowerOrPlatformName, Is.EqualTo("Borrower"));
        Assert.That(command.LendingStartDate, Is.EqualTo(new DateOnly(2024, 1, 1)));
        Assert.That(command.ExpectedRepaymentDate, Is.EqualTo(new DateOnly(2024, 12, 31)));
    }

    [Test]
    public void BuildCreateCommandAsync_UnknownAssetType_ThrowsException()
    {
        var snapshot = BasicSnapshot("Stock");
        snapshot = snapshot with { SelectedAssetType = "InvalidType" };

        Assert.ThrowsAsync<AssetFormBuildException>(async () => await _builder.BuildCreateCommandAsync(snapshot));
    }

    #endregion

    #region BuildEditDetailsAsync Tests

    [Test]
    public async Task BuildEditDetailsAsync_Crypto_ReturnsBasicAssetDetailsInputDTO()
    {
        var snapshot = BasicSnapshot("Crypto");

        var details = await _builder.BuildEditDetailsAsync(snapshot);

        Assert.That(details, Is.TypeOf<BasicAssetDetailsInputDTO>());
        var basic = (BasicAssetDetailsInputDTO)details;
        Assert.That(basic.AssetType, Is.EqualTo((int)AssetTypes.Crypto));
        Assert.That(basic.Symbol, Is.EqualTo("AAPL"));
        Assert.That(basic.Quantity, Is.EqualTo(10));
        Assert.That(basic.CurrentPrice, Is.EqualTo(200m));
        Assert.That(basic.PriceSource, Is.EqualTo((int)AssetPriceSource.Manual));
    }

    [Test]
    public async Task BuildEditDetailsAsync_RealEstate_ReturnsRealEstateDetailsInputDTO()
    {
        var snapshot = RealEstateSnapshot();

        var details = await _builder.BuildEditDetailsAsync(snapshot);

        Assert.That(details, Is.TypeOf<RealEstateAssetDetailsInputDTO>());
        var realEstate = (RealEstateAssetDetailsInputDTO)details;
        Assert.That(realEstate.CurrentValue, Is.EqualTo(500_000m));
        Assert.That(realEstate.Address, Is.EqualTo("123 Ocean Dr"));
    }

    [Test]
    public async Task BuildEditDetailsAsync_Leveraged_ReturnsLeveragedPositionDetailsInputDTO()
    {
        var snapshot = LeveragedSnapshot(useExactPosition: true);

        var details = await _builder.BuildEditDetailsAsync(snapshot);

        Assert.That(details, Is.TypeOf<LeveragedPositionDetailsInputDTO>());
        var leveraged = (LeveragedPositionDetailsInputDTO)details;
        Assert.That(leveraged.InputMode, Is.EqualTo(1));
        Assert.That(leveraged.PositionSize, Is.EqualTo(1m));
    }

    [Test]
    public async Task BuildEditDetailsAsync_BtcLoan_ReturnsBtcLoanDetailsInputDTO()
    {
        var snapshot = BtcLoanSnapshot();
        _priceProviderSelector.GetPriceAsync(AssetPriceSource.LivePrice, "BTC", "USD")
            .Returns(new AssetPriceResult(100_000m, "USD", DateTime.UtcNow));

        var details = await _builder.BuildEditDetailsAsync(snapshot);

        Assert.That(details, Is.TypeOf<BtcLoanDetailsInputDTO>());
        var loan = (BtcLoanDetailsInputDTO)details;
        Assert.That(loan.CollateralSats, Is.EqualTo(1_000_000));
        Assert.That(loan.LoanAmount, Is.EqualTo(50_000m));
        Assert.That(loan.Apr, Is.EqualTo(0.12m));
        Assert.That(loan.CurrentBtcPrice, Is.EqualTo(100_000m));
    }

    [Test]
    public async Task BuildEditDetailsAsync_BtcLoanFixedTotalDebt_SetsAprToZero()
    {
        var snapshot = BtcLoanSnapshot(useFixedTotalDebt: true);

        var details = await _builder.BuildEditDetailsAsync(snapshot);

        var loan = (BtcLoanDetailsInputDTO)details;
        Assert.That(loan.Apr, Is.EqualTo(0m));
        Assert.That(loan.FixedTotalDebt, Is.EqualTo(55_000m));
    }

    [Test]
    public async Task BuildEditDetailsAsync_BtcLending_ReturnsBtcLendingDetailsInputDTO()
    {
        var snapshot = BtcLendingSnapshot();

        var details = await _builder.BuildEditDetailsAsync(snapshot);

        Assert.That(details, Is.TypeOf<BtcLendingDetailsInputDTO>());
        var lending = (BtcLendingDetailsInputDTO)details;
        Assert.That(lending.AmountLent, Is.EqualTo(10_000m));
        Assert.That(lending.Apr, Is.EqualTo(0.10m));
    }

    #endregion

    #region LoadFromDto Tests

    private static AssetDTO BaseDto(AssetTypes assetType) => new()
    {
        Id = "asset-1",
        Name = "Test",
        AssetTypeId = (int)assetType,
        AssetTypeName = assetType.ToString(),
        Icon = string.Empty,
        IncludeInNetWorth = true,
        Visible = true,
        LastPriceUpdateAt = DateTime.Now,
        CreatedAt = DateTime.Now,
        DisplayOrder = 1,
        CurrentPrice = 0,
        CurrentValue = 0,
        CurrencyCode = "USD"
    };

    [Test]
    public void LoadFromDto_Stock_RoundTripsValues()
    {
        var dto = BaseDto(AssetTypes.Stock) with
        {
            CurrentPrice = 200m,
            Symbol = "AAPL",
            Quantity = 10,
            PriceSourceId = (int)AssetPriceSource.YahooFinance,
            AcquisitionDate = new DateOnly(2024, 1, 15),
            AcquisitionPrice = 150m
        };

        var values = _builder.LoadFromDto(dto);

        Assert.That(values.SelectedAssetType, Is.EqualTo(AssetTypes.Stock.ToString()));
        Assert.That(values.Symbol, Is.EqualTo("AAPL"));
        Assert.That(values.Quantity, Is.EqualTo(10));
        Assert.That(values.CurrentPriceFiat, Is.EqualTo(FiatValue.New(200m)));
        Assert.That(values.SelectedPriceSource, Is.EqualTo(AssetPriceSource.YahooFinance.ToString()));
        Assert.That(values.AcquisitionDate, Is.EqualTo(new DateTime(2024, 1, 15)));
        Assert.That(values.AcquisitionPriceFiat, Is.EqualTo(FiatValue.New(150m)));
    }

    [Test]
    public void LoadFromDto_RealEstate_RoundTripsValues()
    {
        var dto = BaseDto(AssetTypes.RealEstate) with
        {
            CurrentValue = 500_000m,
            Address = "123 Ocean Dr",
            MonthlyRentalIncome = 2000m,
            AcquisitionDate = new DateOnly(2020, 6, 1),
            AcquisitionPrice = 400_000m
        };

        var values = _builder.LoadFromDto(dto);

        Assert.That(values.SelectedAssetType, Is.EqualTo(AssetTypes.RealEstate.ToString()));
        Assert.That(values.Address, Is.EqualTo("123 Ocean Dr"));
        Assert.That(values.CurrentValueFiat, Is.EqualTo(FiatValue.New(500_000m)));
        Assert.That(values.MonthlyRentalIncomeFiat, Is.EqualTo(FiatValue.New(2000m)));
        Assert.That(values.AcquisitionDate, Is.EqualTo(new DateTime(2020, 6, 1)));
    }

    [Test]
    public void LoadFromDto_LeveragedPosition_RoundTripsValues()
    {
        var dto = BaseDto(AssetTypes.LeveragedPosition) with
        {
            CurrentPrice = 60_000m,
            Symbol = "BTC-PERP",
            Collateral = 10_000m,
            EntryPrice = 50_000m,
            Leverage = 5,
            LiquidationPrice = 40_000m,
            IsLong = false,
            PriceSourceId = (int)AssetPriceSource.LivePrice,
            InputModeId = 1,
            PositionSize = 1.5m
        };

        var values = _builder.LoadFromDto(dto);

        Assert.That(values.SelectedAssetType, Is.EqualTo(AssetTypes.LeveragedPosition.ToString()));
        Assert.That(values.Symbol, Is.EqualTo("BTC-PERP"));
        Assert.That(values.IsBitcoinUnderlyingAsset, Is.True);
        Assert.That(values.CollateralFiat, Is.EqualTo(FiatValue.New(10_000m)));
        Assert.That(values.EntryPriceFiat, Is.EqualTo(FiatValue.New(50_000m)));
        Assert.That(values.Leverage, Is.EqualTo(5));
        Assert.That(values.LiquidationPriceFiat, Is.EqualTo(FiatValue.New(40_000m)));
        Assert.That(values.IsLong, Is.False);
        Assert.That(values.UseExactPosition, Is.True);
        Assert.That(values.PositionSize, Is.EqualTo(1.5m));
    }

    [Test]
    public void LoadFromDto_LeveragedPositionBitcoinDetection_RequiresBtcPrefix()
    {
        var dto = BaseDto(AssetTypes.LeveragedPosition) with
        {
            Symbol = "ETH-PERP",
            PriceSourceId = (int)AssetPriceSource.LivePrice
        };

        var values = _builder.LoadFromDto(dto);

        Assert.That(values.IsBitcoinUnderlyingAsset, Is.False);
    }

    [Test]
    public void LoadFromDto_BtcLoan_RoundTripsValues()
    {
        var dto = BaseDto(AssetTypes.BtcLoan) with
        {
            PlatformName = "HodlHodl",
            CollateralSats = 1_000_000,
            LoanAmount = 50_000m,
            Apr = 0.12m,
            InitialLtv = 50m,
            LiquidationLtv = 80m,
            MarginCallLtv = 70m,
            Fees = 500m,
            LoanStartDate = new DateOnly(2024, 1, 1),
            RepaymentDate = new DateOnly(2024, 12, 31),
            FixedTotalDebt = 55_000m,
            HasFixedTotalDebt = true
        };

        var values = _builder.LoadFromDto(dto);

        Assert.That(values.SelectedAssetType, Is.EqualTo(AssetTypes.BtcLoan.ToString()));
        Assert.That(values.PlatformName, Is.EqualTo("HodlHodl"));
        Assert.That(values.CollateralSats, Is.EqualTo(1_000_000));
        Assert.That(values.LoanAmountFiat, Is.EqualTo(FiatValue.New(50_000m)));
        Assert.That(values.AprPercentage, Is.EqualTo(12m));
        Assert.That(values.InitialLtvPercentage, Is.EqualTo(50m));
        Assert.That(values.LiquidationLtvPercentage, Is.EqualTo(80m));
        Assert.That(values.MarginCallLtvPercentage, Is.EqualTo(70m));
        Assert.That(values.FeesFiat, Is.EqualTo(FiatValue.New(500m)));
        Assert.That(values.LoanStartDate, Is.EqualTo(new DateTime(2024, 1, 1)));
        Assert.That(values.RepaymentDateOffset, Is.EqualTo(new DateTime(2024, 12, 31)));
        Assert.That(values.IsIndefiniteLoan, Is.False);
        Assert.That(values.UseFixedTotalDebt, Is.True);
        Assert.That(values.FixedTotalDebtFiat, Is.EqualTo(FiatValue.New(55_000m)));
    }

    [Test]
    public void LoadFromDto_BtcLoanIndefinite_SetsFlag()
    {
        var dto = BaseDto(AssetTypes.BtcLoan) with
        {
            RepaymentDate = null
        };

        var values = _builder.LoadFromDto(dto);

        Assert.That(values.IsIndefiniteLoan, Is.True);
        Assert.That(values.RepaymentDateOffset, Is.Null);
    }

    [Test]
    public void LoadFromDto_BtcLending_RoundTripsValues()
    {
        var dto = BaseDto(AssetTypes.BtcLending) with
        {
            BorrowerOrPlatformName = "Borrower",
            AmountLent = 10_000m,
            Apr = 0.10m,
            LendingStartDate = new DateOnly(2024, 1, 1),
            RepaymentDate = new DateOnly(2024, 12, 31)
        };

        var values = _builder.LoadFromDto(dto);

        Assert.That(values.SelectedAssetType, Is.EqualTo(AssetTypes.BtcLending.ToString()));
        Assert.That(values.BorrowerOrPlatformName, Is.EqualTo("Borrower"));
        Assert.That(values.AmountLentFiat, Is.EqualTo(FiatValue.New(10_000m)));
        Assert.That(values.LendingAprPercentage, Is.EqualTo(10m));
        Assert.That(values.LendingStartDateOffset, Is.EqualTo(new DateTime(2024, 1, 1)));
        Assert.That(values.ExpectedRepaymentDateOffset, Is.EqualTo(new DateTime(2024, 12, 31)));
        Assert.That(values.IsIndefiniteLending, Is.False);
    }

    [Test]
    public void LoadFromDto_BtcLendingIndefinite_SetsFlag()
    {
        var dto = BaseDto(AssetTypes.BtcLending) with
        {
            RepaymentDate = null
        };

        var values = _builder.LoadFromDto(dto);

        Assert.That(values.IsIndefiniteLending, Is.True);
        Assert.That(values.ExpectedRepaymentDateOffset, Is.Null);
    }

    [Test]
    public void LoadFromDto_UnknownAssetType_ThrowsException()
    {
        var dto = BaseDto(AssetTypes.Stock) with
        {
            AssetTypeId = 999
        };

        Assert.Throws<AssetFormBuildException>(() => _builder.LoadFromDto(dto));
    }

    #endregion
}
