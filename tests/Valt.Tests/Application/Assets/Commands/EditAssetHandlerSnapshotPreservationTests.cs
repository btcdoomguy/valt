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
    public async Task HandleAsync_BtcLoanWithSnapshot_EditPreservesSnapshot()
    {
        // Arrange
        var effectiveDate = new DateOnly(2025, 6, 1);
        var currentTotalDebt = 30_000m;

        var asset = AssetBuilder.ABtcLoan()
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
                PlatformName = "HodlHodl",
                CollateralSats = 100_000_000L,
                LoanAmount = 25_000m,
                CurrencyCode = "USD",
                Apr = 0.12m,
                InitialLtv = 50m,
                LiquidationLtv = 80m,
                MarginCallLtv = 70m,
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
        Assert.That(reloaded, Is.Not.Null);
        Assert.That(reloaded.Details, Is.TypeOf<BtcLoanDetails>());

        var loanDetails = (BtcLoanDetails)reloaded.Details;
        Assert.That(loanDetails.Snapshots, Has.Count.EqualTo(1));

        var snapshot = loanDetails.Snapshots.Single();
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.EffectiveDate, Is.EqualTo(effectiveDate));
            Assert.That(snapshot.CurrentTotalDebt, Is.EqualTo(currentTotalDebt));
        });
    }

    [Test]
    public async Task HandleAsync_BtcLoanWithMultipleSnapshots_EditPreservesAllSnapshots()
    {
        // Arrange
        var firstDate = new DateOnly(2025, 6, 1);
        var firstDebt = 30_000m;
        var secondDate = new DateOnly(2025, 7, 1);
        var secondDebt = 32_000m;

        var asset = AssetBuilder.ABtcLoan()
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
                PlatformName = "HodlHodl",
                CollateralSats = 100_000_000L,
                LoanAmount = 25_000m,
                CurrencyCode = "USD",
                Apr = 0.12m,
                InitialLtv = 50m,
                LiquidationLtv = 80m,
                MarginCallLtv = 70m,
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

        Assert.That(loanDetails.Snapshots, Has.Count.EqualTo(2));
        Assert.That(loanDetails.Snapshots.Select(s => s.EffectiveDate), Is.EquivalentTo(new[] { firstDate, secondDate }));
        Assert.That(loanDetails.Snapshots.Select(s => s.CurrentTotalDebt), Is.EquivalentTo(new[] { firstDebt, secondDebt }));
    }
}
