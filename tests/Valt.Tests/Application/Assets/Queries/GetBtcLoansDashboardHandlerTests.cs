using Valt.App.Modules.Assets.Queries.GetBtcLoansDashboard;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Queries;

[TestFixture]
public class GetBtcLoansDashboardHandlerTests : DatabaseTest
{
    private GetBtcLoansDashboardHandler _handler = null!;

    private static readonly IReadOnlyDictionary<string, decimal> UsdRates = new Dictionary<string, decimal>
    {
        { "USD", 1.0m },
        { "BRL", 5.0m }
    };

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new GetBtcLoansDashboardHandler(_assetQueries);
    }

    [TearDown]
    public async Task ClearAssets()
    {
        var existing = await _assetRepository.GetAllAsync();
        foreach (var asset in existing)
            await _assetRepository.DeleteAsync(asset);
    }

    private static GetBtcLoansDashboardQuery Query(long stackSats = 100_000_000) => new()
    {
        MainCurrencyCode = "USD",
        BtcPriceUsd = 50_000m,
        FiatRates = UsdRates,
        TotalBtcStackSats = stackSats
    };

    private static Asset BuildLoan(
        string name = "Loan",
        long collateralSats = 100_000_000,
        decimal loanAmount = 25_000m,
        string currencyCode = "USD",
        decimal apr = 0.12m,
        decimal initialLtv = 50m,
        decimal liquidationLtv = 80m,
        decimal marginCallLtv = 70m,
        decimal fees = 0m,
        DateOnly? loanStartDate = null,
        DateOnly? repaymentDate = null,
        LoanStatus status = LoanStatus.Active,
        decimal currentBtcPrice = 50_000m,
        bool visible = true,
        bool includeInNetWorth = true)
    {
        var details = new BtcLoanDetails(
            platformName: "TestPlatform",
            collateralSats: collateralSats,
            loanAmount: loanAmount,
            currencyCode: currencyCode,
            apr: apr,
            initialLtv: initialLtv,
            liquidationLtv: liquidationLtv,
            marginCallLtv: marginCallLtv,
            fees: fees,
            loanStartDate: loanStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            repaymentDate: repaymentDate,
            status: status,
            currentBtcPriceInLoanCurrency: currentBtcPrice);

        return AssetBuilder.AnAsset()
            .WithName(name)
            .WithDetails(details)
            .WithVisible(visible)
            .WithIncludeInNetWorth(includeInNetWorth)
            .Build();
    }

    [Test]
    public async Task HandleAsync_NoLoans_ReturnsHasActiveLoansFalse()
    {
        await _assetRepository.SaveAsync(AssetBuilder.AStockAsset().Build());
        await _assetRepository.SaveAsync(AssetBuilder.AnEtfAsset().Build());

        var result = await _handler.HandleAsync(Query());

        Assert.Multiple(() =>
        {
            Assert.That(result.HasActiveLoans, Is.False);
            Assert.That(result.ActiveLoansCount, Is.EqualTo(0));
            Assert.That(result.TotalDebtInMainCurrency, Is.EqualTo(0m));
        });
    }

    [Test]
    public async Task HandleAsync_OnlyRepaidLoans_ReturnsHasActiveLoansFalse()
    {
        await _assetRepository.SaveAsync(BuildLoan(status: LoanStatus.Repaid));

        var result = await _handler.HandleAsync(Query());

        Assert.That(result.HasActiveLoans, Is.False);
    }

    [Test]
    public async Task HandleAsync_SingleActiveLoan_ComputesCoreMetrics()
    {
        // 25k loan, 1 BTC collateral, btcPrice 50k → LTV = 50%, APR = 12% → 12 displayed
        await _assetRepository.SaveAsync(BuildLoan(
            loanAmount: 25_000m, collateralSats: 100_000_000, apr: 0.12m,
            currentBtcPrice: 50_000m, fees: 0m));

        var result = await _handler.HandleAsync(Query());

        Assert.Multiple(() =>
        {
            Assert.That(result.HasActiveLoans, Is.True);
            Assert.That(result.ActiveLoansCount, Is.EqualTo(1));
            Assert.That(result.TotalDebtInMainCurrency, Is.EqualTo(25_000m));
            Assert.That(result.DebtWeightedAvgLtv, Is.EqualTo(50m));
            Assert.That(result.DebtWeightedAvgApr, Is.EqualTo(12m));
        });
    }

    [Test]
    public async Task HandleAsync_TwoLoans_DebtWeightedAveragesNotMedian()
    {
        // Loan A: $150k loan, 20 BTC collateral @ $50k → collateralValue=$1M → LTV=15%, APR=8%
        await _assetRepository.SaveAsync(BuildLoan(name: "Big",
            loanAmount: 150_000m, collateralSats: 2_000_000_000L, apr: 0.08m,
            initialLtv: 15m, liquidationLtv: 80m, marginCallLtv: 70m,
            currentBtcPrice: 50_000m, fees: 0m));

        // Loan B: $1k loan, 0.02857143 BTC @ $50k → collateralValue≈$1428.57 → LTV=70%, APR=20%
        await _assetRepository.SaveAsync(BuildLoan(name: "Small",
            loanAmount: 1_000m, collateralSats: 2_857_143L, apr: 0.20m,
            initialLtv: 70m, liquidationLtv: 90m, marginCallLtv: 75m,
            currentBtcPrice: 50_000m, fees: 0m));

        var result = await _handler.HandleAsync(Query(stackSats: 2_100_000_000L));

        // Median LTV = 42.5; weighted ≈ (15*150_000 + 70*1_000) / 151_000 = 15.36
        // Median APR = 14; weighted ≈ (8*150_000 + 20*1_000) / 151_000 = 8.08
        Assert.Multiple(() =>
        {
            Assert.That(result.DebtWeightedAvgLtv, Is.EqualTo(15.36m).Within(0.1m));
            Assert.That(result.DebtWeightedAvgApr, Is.EqualTo(8.08m).Within(0.1m));
            Assert.That(result.DebtWeightedAvgLtv, Is.Not.EqualTo(42.5m));
            Assert.That(result.DebtWeightedAvgApr, Is.Not.EqualTo(14m));
        });
    }

    [Test]
    public async Task HandleAsync_CollateralPercentOfStack_HalfStack()
    {
        await _assetRepository.SaveAsync(BuildLoan(collateralSats: 100_000_000));

        var result = await _handler.HandleAsync(Query(stackSats: 200_000_000L));

        Assert.Multiple(() =>
        {
            Assert.That(result.CollateralPercentOfStack, Is.EqualTo(50m));
            Assert.That(result.FreeBtcSats, Is.EqualTo(100_000_000L));
            Assert.That(result.TotalCollateralSats, Is.EqualTo(100_000_000L));
        });
    }

    [Test]
    public async Task HandleAsync_CollateralExceedsStack_NegativeFreeBtcAndOver100Percent()
    {
        await _assetRepository.SaveAsync(BuildLoan(collateralSats: 100_000_000));

        var result = await _handler.HandleAsync(Query(stackSats: 50_000_000L));

        Assert.Multiple(() =>
        {
            Assert.That(result.CollateralPercentOfStack, Is.EqualTo(200m));
            Assert.That(result.FreeBtcSats, Is.EqualTo(-50_000_000L));
        });
    }

    [Test]
    public async Task HandleAsync_HealthBreakdown_CountsByBucket()
    {
        // marginCall=70, liquidation=80; default loanAmount=25k, collateral=1 BTC
        // Healthy: btc=50_000 → LTV=50
        await _assetRepository.SaveAsync(BuildLoan(name: "Healthy", currentBtcPrice: 50_000m));
        // Warning: btc=33_000 → LTV=25000/33000*100 = 75.76 (in [70,80))
        await _assetRepository.SaveAsync(BuildLoan(name: "Warning", currentBtcPrice: 33_000m));
        // Danger: btc=30_000 → LTV=83.33 (≥80)
        await _assetRepository.SaveAsync(BuildLoan(name: "Danger", currentBtcPrice: 30_000m));

        var result = await _handler.HandleAsync(Query());

        Assert.Multiple(() =>
        {
            Assert.That(result.HealthyCount, Is.EqualTo(1));
            Assert.That(result.WarningCount, Is.EqualTo(1));
            Assert.That(result.DangerCount, Is.EqualTo(1));
            Assert.That(result.ActiveLoansCount, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task HandleAsync_HighestLtvAndClosestDistance_ReportedFromCorrectLoan()
    {
        // Loan A: LTV 50, liquidation 80 → distance 30
        await _assetRepository.SaveAsync(BuildLoan(name: "Spacious",
            loanAmount: 25_000m, collateralSats: 100_000_000, currentBtcPrice: 50_000m,
            liquidationLtv: 80m, marginCallLtv: 70m));

        // Loan B: LTV 60, liquidation 70 → distance 10 (closest)
        await _assetRepository.SaveAsync(BuildLoan(name: "Tight",
            loanAmount: 30_000m, collateralSats: 100_000_000, currentBtcPrice: 50_000m,
            liquidationLtv: 70m, marginCallLtv: 65m));

        var result = await _handler.HandleAsync(Query());

        Assert.Multiple(() =>
        {
            Assert.That(result.HighestLtv, Is.EqualTo(60m));
            Assert.That(result.ClosestDistanceToLiquidationLtv, Is.EqualTo(10m));
            Assert.That(result.ClosestLoanName, Is.EqualTo("Tight"));
        });
    }

    [Test]
    public async Task HandleAsync_WorstCaseLiquidationBtcPrice_SingleUsdLoan()
    {
        // loan=25k, liqLtv=80, collateral=1 BTC → liqPrice = 25000 / (0.8 * 1) = 31_250
        await _assetRepository.SaveAsync(BuildLoan(
            loanAmount: 25_000m, collateralSats: 100_000_000,
            liquidationLtv: 80m, marginCallLtv: 70m, currencyCode: "USD"));

        var result = await _handler.HandleAsync(Query());

        Assert.That(result.WorstCaseLiquidationBtcPriceUsd, Is.EqualTo(31_250m).Within(0.5m));
    }

    [Test]
    public async Task HandleAsync_MultiCurrency_ConvertsDebtToMainCurrency()
    {
        // USD loan: $10_000 (no fees, accrued≈0)
        await _assetRepository.SaveAsync(BuildLoan(name: "USD",
            loanAmount: 10_000m, currencyCode: "USD", fees: 0m,
            collateralSats: 100_000_000, currentBtcPrice: 50_000m));

        // BRL loan: R$50_000; fiatRates BRL=5 → $10_000 in main (USD)
        await _assetRepository.SaveAsync(BuildLoan(name: "BRL",
            loanAmount: 50_000m, currencyCode: "BRL", fees: 0m,
            collateralSats: 100_000_000, currentBtcPrice: 250_000m));

        var result = await _handler.HandleAsync(Query());

        Assert.That(result.TotalDebtInMainCurrency, Is.EqualTo(20_000m).Within(1m));
    }

    [Test]
    public async Task HandleAsync_NextRepayment_PicksEarliestNonNull()
    {
        var early = new DateOnly(2026, 8, 1);
        await _assetRepository.SaveAsync(BuildLoan(name: "Late", repaymentDate: new DateOnly(2027, 6, 15)));
        await _assetRepository.SaveAsync(BuildLoan(name: "Earliest", repaymentDate: early));
        await _assetRepository.SaveAsync(BuildLoan(name: "Middle", repaymentDate: new DateOnly(2026, 12, 31)));
        await _assetRepository.SaveAsync(BuildLoan(name: "OpenEnded", repaymentDate: null));

        var result = await _handler.HandleAsync(Query());

        Assert.Multiple(() =>
        {
            Assert.That(result.NextRepaymentDate, Is.EqualTo(early));
            Assert.That(result.NextRepaymentLoanName, Is.EqualTo("Earliest"));
            Assert.That(result.DaysUntilNextRepayment, Is.Not.Null);
        });
    }

    [Test]
    public async Task HandleAsync_AllLoansOpenEnded_NextRepaymentNull()
    {
        await _assetRepository.SaveAsync(BuildLoan(name: "A", repaymentDate: null));
        await _assetRepository.SaveAsync(BuildLoan(name: "B", repaymentDate: null));

        var result = await _handler.HandleAsync(Query());

        Assert.Multiple(() =>
        {
            Assert.That(result.NextRepaymentDate, Is.Null);
            Assert.That(result.DaysUntilNextRepayment, Is.Null);
            Assert.That(result.NextRepaymentLoanName, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_IgnoresVisibleAndIncludeInNetWorthFlags()
    {
        await _assetRepository.SaveAsync(BuildLoan(
            name: "Hidden", visible: false, includeInNetWorth: false));

        var result = await _handler.HandleAsync(Query());

        Assert.Multiple(() =>
        {
            Assert.That(result.HasActiveLoans, Is.True);
            Assert.That(result.ActiveLoansCount, Is.EqualTo(1));
            Assert.That(result.TotalCollateralSats, Is.EqualTo(100_000_000L));
        });
    }

    [Test]
    public async Task HandleAsync_ExcludesNonLoanAssets()
    {
        await _assetRepository.SaveAsync(AssetBuilder.AStockAsset("AAPL", 150m, 10).Build());
        await _assetRepository.SaveAsync(AssetBuilder.AnEtfAsset("SPY", 450m, 5).Build());
        await _assetRepository.SaveAsync(AssetBuilder.ALeveragedPosition().Build());
        await _assetRepository.SaveAsync(BuildLoan(loanAmount: 10_000m, fees: 0m));

        var result = await _handler.HandleAsync(Query());

        Assert.Multiple(() =>
        {
            Assert.That(result.ActiveLoansCount, Is.EqualTo(1));
            Assert.That(result.TotalDebtInMainCurrency, Is.EqualTo(10_000m));
        });
    }
}
