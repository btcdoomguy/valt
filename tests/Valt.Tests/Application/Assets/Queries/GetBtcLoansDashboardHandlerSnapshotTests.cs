using Valt.App.Kernel.Validation;
using Valt.App.Modules.Assets.Commands.DeleteLoanStateUpdate;
using Valt.App.Modules.Assets.Queries.GetBtcLoansDashboard;
using Valt.Core.Modules.Assets;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Queries;

[TestFixture]
public class GetBtcLoansDashboardHandlerSnapshotTests : DatabaseTest
{
    private GetBtcLoansDashboardHandler _dashboardHandler = null!;
    private DeleteLoanStateUpdateHandler _deleteHandler = null!;

    private static readonly IReadOnlyDictionary<string, decimal> UsdRates = new Dictionary<string, decimal>
    {
        { "USD", 1.0m }
    };

    [SetUp]
    public void SetUpHandlers()
    {
        _dashboardHandler = new GetBtcLoansDashboardHandler(_assetQueries);
        _deleteHandler = new DeleteLoanStateUpdateHandler(_assetRepository, new DeleteLoanStateUpdateValidator());
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

    [Test]
    public async Task HandleAsync_LatestSnapshot_DrivesDashboardTotals()
    {
        // Setup: 25k loan, 1 BTC collateral, 12% APR
        // Snapshot: 30k debt, 0.8 BTC collateral, 15% APR
        var snapshotDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var asset = AssetBuilder.ABtcLoan(
                loanAmount: 25_000m,
                collateralSats: 100_000_000L,
                currentBtcPrice: 50_000m)
            .WithSnapshot(
                effectiveDate: snapshotDate,
                totalBorrowed: 25_000m,
                interestAccruedUntilDate: 4_900m,
                collateralSats: 80_000_000L,
                apr: 0.15m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var result = await _dashboardHandler.HandleAsync(Query());

        // 30k debt / (0.8 BTC * 50k) = 30k / 40k = 75% LTV
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalDebtInMainCurrency, Is.EqualTo(30_000m));
            Assert.That(result.TotalBorrowedInMainCurrency, Is.EqualTo(25_000m));
            Assert.That(result.DebtWeightedAvgLtv, Is.EqualTo(75m));
            Assert.That(result.DebtWeightedAvgApr, Is.EqualTo(15m));
            Assert.That(result.HighestLtv, Is.EqualTo(75m));
            Assert.That(result.ClosestDistanceToLiquidationLtv, Is.EqualTo(5m));
        });
    }

    [Test]
    public async Task HandleAsync_DeleteLatestSnapshot_FallsBackToPreviousSnapshot()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var firstDebt = 30_000m;
        var firstBorrowed = 25_000m;
        var firstInterest = 4_900m;

        var asset = AssetBuilder.ABtcLoan(
                loanAmount: 25_000m,
                collateralSats: 100_000_000L,
                currentBtcPrice: 50_000m)
            .WithSnapshot(
                effectiveDate: yesterday,
                totalBorrowed: firstBorrowed,
                interestAccruedUntilDate: firstInterest,
                collateralSats: 80_000_000L,
                apr: 0.15m)
            .WithSnapshot(
                effectiveDate: today,
                totalBorrowed: 25_000m,
                interestAccruedUntilDate: 5_900m,
                collateralSats: 70_000_000L,
                apr: 0.18m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var deleteResult = await _deleteHandler.HandleAsync(new DeleteLoanStateUpdateCommand
        {
            AssetId = asset.Id.Value,
            EffectiveDate = today
        });

        Assert.That(deleteResult.IsSuccess, Is.True);

        var result = await _dashboardHandler.HandleAsync(Query());

        // Previous snapshot recalculates one day of interest:
        // borrowed = 25_000; interest until date = 4_900; additional interest = 25_000 * 0.15 / 365 = 10.27
        // Total = 25_000 + 4_900 + 100 + 10.27 = 30_010.27
        var expectedDebt = 30_010.27m;
        var expectedLtv = Math.Round(expectedDebt / 40_000m * 100, 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalDebtInMainCurrency, Is.EqualTo(expectedDebt));
            Assert.That(result.TotalBorrowedInMainCurrency, Is.EqualTo(25_000m));
            Assert.That(result.DebtWeightedAvgLtv, Is.EqualTo(expectedLtv));
            Assert.That(result.DebtWeightedAvgApr, Is.EqualTo(15m));
            Assert.That(result.HighestLtv, Is.EqualTo(expectedLtv));
        });
    }
}
