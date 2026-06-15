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
        var snapshotDate = new DateOnly(2025, 6, 1);
        var asset = AssetBuilder.ABtcLoan(
                loanAmount: 25_000m,
                collateralSats: 100_000_000L,
                currentBtcPrice: 50_000m)
            .WithSnapshot(
                effectiveDate: snapshotDate,
                currentTotalDebt: 30_000m,
                collateralSats: 80_000_000L,
                apr: 0.15m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var result = await _dashboardHandler.HandleAsync(Query());

        // 30k debt / (0.8 BTC * 50k) = 30k / 40k = 75% LTV
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalDebtInMainCurrency, Is.EqualTo(30_000m));
            Assert.That(result.DebtWeightedAvgLtv, Is.EqualTo(75m));
            Assert.That(result.DebtWeightedAvgApr, Is.EqualTo(15m));
            Assert.That(result.HighestLtv, Is.EqualTo(75m));
            Assert.That(result.ClosestDistanceToLiquidationLtv, Is.EqualTo(5m));
        });
    }

    [Test]
    public async Task HandleAsync_DeleteLatestSnapshot_FallsBackToPreviousSnapshot()
    {
        var firstDate = new DateOnly(2025, 6, 1);
        var firstDebt = 30_000m;
        var secondDate = new DateOnly(2025, 7, 1);

        var asset = AssetBuilder.ABtcLoan(
                loanAmount: 25_000m,
                collateralSats: 100_000_000L,
                currentBtcPrice: 50_000m)
            .WithSnapshot(
                effectiveDate: firstDate,
                currentTotalDebt: firstDebt,
                collateralSats: 80_000_000L,
                apr: 0.15m)
            .WithSnapshot(
                effectiveDate: secondDate,
                currentTotalDebt: 35_000m,
                collateralSats: 70_000_000L,
                apr: 0.18m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var deleteResult = await _deleteHandler.HandleAsync(new DeleteLoanStateUpdateCommand
        {
            AssetId = asset.Id.Value,
            EffectiveDate = secondDate
        });

        Assert.That(deleteResult.IsSuccess, Is.True);

        var result = await _dashboardHandler.HandleAsync(Query());

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalDebtInMainCurrency, Is.EqualTo(firstDebt));
            Assert.That(result.DebtWeightedAvgLtv, Is.EqualTo(75m));
            Assert.That(result.DebtWeightedAvgApr, Is.EqualTo(15m));
            Assert.That(result.HighestLtv, Is.EqualTo(75m));
        });
    }
}
