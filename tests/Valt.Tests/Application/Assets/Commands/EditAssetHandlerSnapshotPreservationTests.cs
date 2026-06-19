using Valt.App.Kernel.Validation;
using Valt.App.Modules.Assets.Commands.EditAsset;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class EditAssetHandlerSnapshotPreservationTests : DatabaseTest
{
    private EditAssetHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new EditAssetHandler(_assetRepository, new EditAssetValidator());
    }

    [TearDown]
    public async Task ClearAssets()
    {
        var existing = await _assetRepository.GetAllAsync();
        foreach (var asset in existing)
            await _assetRepository.DeleteAsync(asset);
    }

    [Test]
    public async Task HandleAsync_BtcLoanWithSnapshot_EditUpdatesSetupValuesWhilePreservingPointInTimeData()
    {
        // Arrange
        var effectiveDate = new DateOnly(2025, 6, 1);
        var currentTotalDebt = 30_000m;

        var asset = AssetBuilder.ABtcLoan(
                platformName: "HodlHodl",
                collateralSats: 80_000_000L,
                loanAmount: 20_000m,
                currentBtcPrice: 50_000m)
            .WithSnapshot(
                effectiveDate: effectiveDate,
                currentTotalDebt: currentTotalDebt,
                collateralSats: 80_000_000L,
                apr: 0.15m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var command = new EditAssetCommand
        {
            AssetId = asset.Id.Value,
            Name = "Renamed BTC Loan",
            Details = new BtcLoanDetailsInputDTO
            {
                PlatformName = "Ledn",
                CollateralSats = 100_000_000L,
                LoanAmount = 25_000m,
                CurrencyCode = "EUR",
                Apr = 0.12m,
                InitialLtv = 55m,
                LiquidationLtv = 85m,
                MarginCallLtv = 75m,
                Fees = 100m,
                LoanStartDate = new DateOnly(2025, 2, 1),
                RepaymentDate = new DateOnly(2026, 2, 1),
                CurrentBtcPrice = 55_000m,
                Status = (int)LoanStatus.Active
            },
            IncludeInNetWorth = true,
            Visible = true
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        var reloaded = await _assetRepository.GetByIdAsync(asset.Id);
        Assert.That(reloaded, Is.Not.Null);
        Assert.That(reloaded.Details, Is.TypeOf<BtcLoanDetails>());

        var loanDetails = (BtcLoanDetails)reloaded.Details;
        Assert.That(loanDetails.Snapshots, Has.Count.EqualTo(1));

        var snapshot = loanDetails.Snapshots.Single();
        Assert.Multiple(() =>
        {
            // Point-in-time data preserved
            Assert.That(snapshot.EffectiveDate, Is.EqualTo(effectiveDate));
            Assert.That(snapshot.CurrentTotalDebt, Is.EqualTo(currentTotalDebt));

            // Setup values propagated to the snapshot
            Assert.That(snapshot.PlatformName, Is.EqualTo("Ledn"));
            Assert.That(snapshot.CollateralSats, Is.EqualTo(100_000_000L));
            Assert.That(snapshot.LoanAmount, Is.EqualTo(25_000m));
            Assert.That(snapshot.CurrencyCode, Is.EqualTo("EUR"));
            Assert.That(snapshot.Apr, Is.EqualTo(0.12m));
            Assert.That(snapshot.InitialLtv, Is.EqualTo(55m));
            Assert.That(snapshot.LiquidationLtv, Is.EqualTo(85m));
            Assert.That(snapshot.MarginCallLtv, Is.EqualTo(75m));
            Assert.That(snapshot.Fees, Is.EqualTo(100m));
            Assert.That(snapshot.LoanStartDate, Is.EqualTo(new DateOnly(2025, 2, 1)));
            Assert.That(snapshot.RepaymentDate, Is.EqualTo(new DateOnly(2026, 2, 1)));
        });
    }

    [Test]
    public async Task HandleAsync_BtcLoanWithMultipleSnapshots_EditPropagatesSetupValuesToAllSnapshots()
    {
        // Arrange
        var firstDate = new DateOnly(2025, 6, 1);
        var firstDebt = 30_000m;
        var secondDate = new DateOnly(2025, 7, 1);
        var secondDebt = 32_000m;

        var asset = AssetBuilder.ABtcLoan(
                platformName: "HodlHodl",
                collateralSats: 80_000_000L,
                loanAmount: 20_000m,
                currentBtcPrice: 50_000m)
            .WithSnapshot(
                effectiveDate: firstDate,
                currentTotalDebt: firstDebt,
                collateralSats: 80_000_000L)
            .WithSnapshot(
                effectiveDate: secondDate,
                currentTotalDebt: secondDebt,
                collateralSats: 75_000_000L)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var command = new EditAssetCommand
        {
            AssetId = asset.Id.Value,
            Name = "Renamed BTC Loan",
            Details = new BtcLoanDetailsInputDTO
            {
                PlatformName = "Ledn",
                CollateralSats = 100_000_000L,
                LoanAmount = 25_000m,
                CurrencyCode = "EUR",
                Apr = 0.12m,
                InitialLtv = 55m,
                LiquidationLtv = 85m,
                MarginCallLtv = 75m,
                Fees = 100m,
                LoanStartDate = new DateOnly(2025, 2, 1),
                RepaymentDate = new DateOnly(2026, 2, 1),
                CurrentBtcPrice = 55_000m,
                Status = (int)LoanStatus.Active
            },
            IncludeInNetWorth = true,
            Visible = true
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        var reloaded = await _assetRepository.GetByIdAsync(asset.Id);
        var loanDetails = (BtcLoanDetails)reloaded!.Details;

        Assert.That(loanDetails.Snapshots, Has.Count.EqualTo(2));
        Assert.That(loanDetails.Snapshots.Select(s => s.EffectiveDate), Is.EquivalentTo(new[] { firstDate, secondDate }));
        Assert.That(loanDetails.Snapshots.Select(s => s.CurrentTotalDebt), Is.EquivalentTo(new[] { firstDebt, secondDebt }));

        foreach (var snapshot in loanDetails.Snapshots)
        {
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.PlatformName, Is.EqualTo("Ledn"));
                Assert.That(snapshot.CollateralSats, Is.EqualTo(100_000_000L));
                Assert.That(snapshot.LoanAmount, Is.EqualTo(25_000m));
                Assert.That(snapshot.CurrencyCode, Is.EqualTo("EUR"));
                Assert.That(snapshot.Apr, Is.EqualTo(0.12m));
                Assert.That(snapshot.InitialLtv, Is.EqualTo(55m));
                Assert.That(snapshot.LiquidationLtv, Is.EqualTo(85m));
                Assert.That(snapshot.MarginCallLtv, Is.EqualTo(75m));
                Assert.That(snapshot.Fees, Is.EqualTo(100m));
                Assert.That(snapshot.LoanStartDate, Is.EqualTo(new DateOnly(2025, 2, 1)));
                Assert.That(snapshot.RepaymentDate, Is.EqualTo(new DateOnly(2026, 2, 1)));
            });
        }
    }

    [Test]
    public async Task HandleAsync_BtcLoanWithSnapshot_EditChangesCurrentCalculations()
    {
        // Arrange
        var effectiveDate = new DateOnly(2025, 6, 1);

        var asset = AssetBuilder.ABtcLoan(
                collateralSats: 100_000_000L,
                loanAmount: 25_000m,
                currentBtcPrice: 50_000m)
            .WithSnapshot(
                effectiveDate: effectiveDate,
                currentTotalDebt: 30_000m,
                collateralSats: 100_000_000L,
                loanAmount: 25_000m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var command = new EditAssetCommand
        {
            AssetId = asset.Id.Value,
            Name = "Renamed BTC Loan",
            Details = new BtcLoanDetailsInputDTO
            {
                PlatformName = "HodlHodl",
                CollateralSats = 100_000_000L,
                LoanAmount = 40_000m,
                CurrencyCode = "USD",
                Apr = 0.12m,
                InitialLtv = 80m,
                LiquidationLtv = 90m,
                MarginCallLtv = 85m,
                Fees = 0m,
                LoanStartDate = new DateOnly(2025, 1, 1),
                RepaymentDate = new DateOnly(2026, 1, 1),
                CurrentBtcPrice = 50_000m,
                Status = (int)LoanStatus.Active
            },
            IncludeInNetWorth = true,
            Visible = true
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        var reloaded = await _assetRepository.GetByIdAsync(asset.Id);
        var loanDetails = (BtcLoanDetails)reloaded!.Details;

        // LTV = LoanAmount / CollateralValue * 100
        // Collateral = 1 BTC * 50,000 = 50,000; Loan = 40,000 => LTV = 80%
        Assert.That(loanDetails.CalculateCurrentLtv(50_000m), Is.EqualTo(80m));
    }
}
