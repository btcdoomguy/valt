using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Builders;

/// <summary>
/// Builder for creating Asset instances for testing.
/// </summary>
public class AssetBuilder
{
    private AssetId _id = new();
    private AssetName _name = new("Test Asset");
    private IAssetDetails _details = new BasicAssetDetails(
        AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 100m, "USD");
    private Icon _icon = Icon.Empty;
    private bool _includeInNetWorth = true;
    private bool _visible = true;
    private DateTime _lastPriceUpdateAt = DateTime.UtcNow;
    private DateTime _createdAt = DateTime.UtcNow;
    private int _displayOrder = 0;
    private AssetGroupId? _groupId = null;
    private int _version = 1;

    public AssetBuilder WithId(AssetId id)
    {
        _id = id;
        return this;
    }

    public AssetBuilder WithName(string name)
    {
        _name = new AssetName(name);
        return this;
    }

    public AssetBuilder WithDetails(IAssetDetails details)
    {
        _details = details;
        return this;
    }

    public AssetBuilder WithBasicDetails(
        AssetTypes assetType = AssetTypes.Stock,
        decimal quantity = 10,
        string symbol = "AAPL",
        AssetPriceSource priceSource = AssetPriceSource.Manual,
        decimal currentPrice = 100m,
        string currencyCode = "USD")
    {
        _details = new BasicAssetDetails(assetType, quantity, symbol, priceSource, currentPrice, currencyCode);
        return this;
    }

    public AssetBuilder WithLeveragedDetails(
        decimal collateral = 1000m,
        decimal entryPrice = 50000m,
        decimal leverage = 10m,
        decimal liquidationPrice = 45000m,
        decimal currentPrice = 55000m,
        string currencyCode = "USD",
        string? symbol = "BTC",
        AssetPriceSource priceSource = AssetPriceSource.Manual,
        bool isLong = true,
        LeveragedPositionInputMode inputMode = LeveragedPositionInputMode.Collateral)
    {
        _details = new LeveragedPositionDetails(
            collateral, entryPrice, leverage, liquidationPrice, currentPrice, currencyCode, symbol, priceSource, isLong, inputMode);
        return this;
    }

    public AssetBuilder WithRealEstateDetails(
        decimal currentValue = 500000m,
        string currencyCode = "USD",
        string? address = null,
        decimal? monthlyRentalIncome = null)
    {
        _details = new RealEstateAssetDetails(currentValue, currencyCode, address, monthlyRentalIncome);
        return this;
    }

    public AssetBuilder WithIcon(Icon icon)
    {
        _icon = icon;
        return this;
    }

    public AssetBuilder WithIncludeInNetWorth(bool include)
    {
        _includeInNetWorth = include;
        return this;
    }

    public AssetBuilder WithVisible(bool visible)
    {
        _visible = visible;
        return this;
    }

    public AssetBuilder WithLastPriceUpdateAt(DateTime lastPriceUpdateAt)
    {
        _lastPriceUpdateAt = lastPriceUpdateAt;
        return this;
    }

    public AssetBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public AssetBuilder WithDisplayOrder(int displayOrder)
    {
        _displayOrder = displayOrder;
        return this;
    }

    public AssetBuilder WithGroupId(AssetGroupId? groupId)
    {
        _groupId = groupId;
        return this;
    }

    public AssetBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public Asset Build()
    {
        return Asset.Create(_id, _name, _details, _icon, _includeInNetWorth, _visible,
            _lastPriceUpdateAt, _createdAt, _displayOrder, _groupId, _version);
    }

    // Static factory methods
    public static AssetBuilder AnAsset() => new();

    public static AssetBuilder AStockAsset(string symbol = "AAPL", decimal price = 150m, decimal quantity = 10) =>
        new AssetBuilder()
            .WithName($"{symbol} Stock")
            .WithBasicDetails(AssetTypes.Stock, quantity, symbol, AssetPriceSource.Manual, price, "USD");

    public static AssetBuilder AnEtfAsset(string symbol = "SPY", decimal price = 450m, decimal quantity = 5) =>
        new AssetBuilder()
            .WithName($"{symbol} ETF")
            .WithBasicDetails(AssetTypes.Etf, quantity, symbol, AssetPriceSource.Manual, price, "USD");

    public static AssetBuilder ACryptoAsset(string symbol = "ETH", decimal price = 2500m, decimal quantity = 2) =>
        new AssetBuilder()
            .WithName($"{symbol} Crypto")
            .WithBasicDetails(AssetTypes.Crypto, quantity, symbol, AssetPriceSource.Manual, price, "USD");

    public static AssetBuilder ARealEstateAsset(decimal value = 500000m, string? address = "123 Main St") =>
        new AssetBuilder()
            .WithName("Property")
            .WithRealEstateDetails(value, "USD", address, null);

    public static AssetBuilder ALeveragedPosition(
        decimal collateral = 1000m,
        decimal entryPrice = 50000m,
        decimal leverage = 10m,
        decimal currentPrice = 55000m,
        bool isLong = true) =>
        new AssetBuilder()
            .WithName("BTC Long 10x")
            .WithLeveragedDetails(
                collateral, entryPrice, leverage,
                liquidationPrice: isLong ? 45000m : 55000m,
                currentPrice, "USD", "BTC", AssetPriceSource.Manual, isLong);

    public static AssetBuilder ABitcoinLeveragedPosition(
        decimal collateral = 1000m,
        decimal entryPrice = 50000m,
        decimal leverage = 10m,
        decimal currentPrice = 55000m,
        bool isLong = true) =>
        new AssetBuilder()
            .WithName("BTC Long 10x")
            .WithLeveragedDetails(
                collateral, entryPrice, leverage,
                liquidationPrice: isLong ? 45000m : 55000m,
                currentPrice, "USD", "BTC", AssetPriceSource.LivePrice, isLong);

    public AssetBuilder WithBtcLoanDetails(
        string platformName = "HodlHodl",
        long collateralSats = 100_000_000,
        decimal loanAmount = 25_000m,
        string currencyCode = "USD",
        decimal apr = 0.12m,
        decimal initialLtv = 50m,
        decimal liquidationLtv = 80m,
        decimal marginCallLtv = 70m,
        decimal fees = 100m,
        LoanStatus status = LoanStatus.Active,
        decimal currentBtcPrice = 50_000m)
    {
        _details = new BtcLoanDetails(
            platformName, collateralSats, loanAmount, currencyCode, apr,
            initialLtv, liquidationLtv, marginCallLtv, fees,
            new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1),
            status, currentBtcPrice);
        return this;
    }

    public AssetBuilder WithBtcLendingDetails(
        decimal amountLent = 10_000m,
        string currencyCode = "USD",
        decimal apr = 0.05m,
        string borrowerOrPlatformName = "Ledn",
        LoanStatus status = LoanStatus.Active)
    {
        _details = new BtcLendingDetails(
            amountLent, currencyCode, apr,
            new DateOnly(2026, 1, 1),
            borrowerOrPlatformName,
            new DateOnly(2025, 1, 1),
            status);
        return this;
    }

    public AssetBuilder WithSnapshot(
        DateOnly effectiveDate,
        decimal totalBorrowed,
        decimal interestAccruedUntilDate = 0m,
        long? collateralSats = null,
        decimal? loanAmount = null,
        decimal? apr = null,
        decimal? liquidationLtv = null,
        decimal? marginCallLtv = null,
        decimal? fees = null,
        DateOnly? repaymentDate = null,
        LoanStatus? status = null,
        decimal? currentBtcPrice = null,
        string? note = null)
    {
        if (_details is not BtcLoanDetails loan)
            throw new InvalidOperationException("WithSnapshot requires BTC loan details");

        var snapshot = new LoanStateSnapshot(
            effectiveDate: effectiveDate,
            totalBorrowed: totalBorrowed,
            interestAccruedUntilDate: interestAccruedUntilDate,
            platformName: loan.PlatformName,
            collateralSats: collateralSats ?? loan.CollateralSats,
            loanAmount: loanAmount ?? loan.LoanAmount,
            currencyCode: loan.CurrencyCode,
            apr: apr ?? loan.Apr,
            initialLtv: loan.InitialLtv,
            liquidationLtv: liquidationLtv ?? loan.LiquidationLtv,
            marginCallLtv: marginCallLtv ?? loan.MarginCallLtv,
            fees: fees ?? loan.Fees,
            loanStartDate: loan.LoanStartDate,
            repaymentDate: repaymentDate ?? loan.RepaymentDate,
            status: status ?? loan.Status,
            currentBtcPriceInLoanCurrency: currentBtcPrice ?? loan.CurrentBtcPriceInLoanCurrency,
            fixedTotalDebt: loan.FixedTotalDebt,
            note: note);

        _details = loan.WithAddedSnapshot(snapshot);
        return this;
    }

    public AssetBuilder WithSeededSnapshot()
    {
        if (_details is not BtcLoanDetails loan)
            throw new InvalidOperationException("WithSeededSnapshot requires BTC loan details");

        var currentTotalDebt = loan.CalculateTotalDebt();
        var interest = Math.Max(0m, currentTotalDebt - loan.LoanAmount - loan.Fees);

        var snapshot = new LoanStateSnapshot(
            effectiveDate: loan.LoanStartDate,
            totalBorrowed: loan.LoanAmount,
            interestAccruedUntilDate: interest,
            platformName: loan.PlatformName,
            collateralSats: loan.CollateralSats,
            loanAmount: loan.LoanAmount,
            currencyCode: loan.CurrencyCode,
            apr: loan.Apr,
            initialLtv: loan.InitialLtv,
            liquidationLtv: loan.LiquidationLtv,
            marginCallLtv: loan.MarginCallLtv,
            fees: loan.Fees,
            loanStartDate: loan.LoanStartDate,
            repaymentDate: loan.RepaymentDate,
            status: loan.Status,
            currentBtcPriceInLoanCurrency: loan.CurrentBtcPriceInLoanCurrency,
            fixedTotalDebt: loan.FixedTotalDebt,
            note: null);

        _details = loan.WithAddedSnapshot(snapshot);
        return this;
    }

    public static AssetBuilder ABtcLoan(
        string platformName = "HodlHodl",
        long collateralSats = 100_000_000,
        decimal loanAmount = 25_000m,
        decimal currentBtcPrice = 50_000m) =>
        new AssetBuilder()
            .WithName("BTC Loan")
            .WithBtcLoanDetails(
                platformName: platformName,
                collateralSats: collateralSats,
                loanAmount: loanAmount,
                currentBtcPrice: currentBtcPrice);

    public static AssetBuilder ABtcLending(
        decimal amountLent = 10_000m,
        string borrowerOrPlatformName = "Ledn") =>
        new AssetBuilder()
            .WithName("BTC Lending")
            .WithBtcLendingDetails(
                amountLent: amountLent,
                borrowerOrPlatformName: borrowerOrPlatformName);
}
