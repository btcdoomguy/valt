using Valt.App.Modules.Assets.Commands.RepayLoan;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class RepayLoanHandlerTests : DatabaseTest
{
    private RepayLoanHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new RepayLoanHandler(
            _assetRepository,
            new RepayLoanValidator());
    }

    [Test]
    public async Task HandleAsync_WithBtcLoanAsset_SetsStatusToRepaid()
    {
        // Arrange - create a BTC loan asset
        var details = new BtcLoanDetails(
            platformName: "HodlHodl",
            collateralSats: 100_000_000,
            loanAmount: 25_000m,
            currencyCode: "USD",
            apr: 0.12m,
            initialLtv: 50m,
            liquidationLtv: 80m,
            marginCallLtv: 70m,
            fees: 100m,
            loanStartDate: new DateOnly(2025, 1, 1),
            repaymentDate: new DateOnly(2026, 1, 1),
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: 50_000m);

        var asset = Asset.New(new AssetName("Test Loan"), details, Icon.Empty);
        await _assetRepository.SaveAsync(asset);

        // Act
        var result = await _handler.HandleAsync(new RepayLoanCommand { AssetId = asset.Id.Value });

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        var updated = await _assetRepository.GetByIdAsync(asset.Id);
        Assert.That(updated, Is.Not.Null);
        var updatedDetails = (BtcLoanDetails)updated!.Details;
        Assert.That(updatedDetails.Status, Is.EqualTo(LoanStatus.Repaid));
    }

    [Test]
    public async Task HandleAsync_WithBtcLendingAsset_SetsStatusToRepaid()
    {
        // Arrange - create a BTC lending asset
        var details = new BtcLendingDetails(
            amountLent: 10_000m,
            currencyCode: "USD",
            apr: 0.05m,
            expectedRepaymentDate: new DateOnly(2026, 1, 1),
            borrowerOrPlatformName: "Ledn",
            lendingStartDate: new DateOnly(2025, 1, 1),
            status: LoanStatus.Active);

        var asset = Asset.New(new AssetName("Test Lending"), details, Icon.Empty);
        await _assetRepository.SaveAsync(asset);

        // Act
        var result = await _handler.HandleAsync(new RepayLoanCommand { AssetId = asset.Id.Value });

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        var updated = await _assetRepository.GetByIdAsync(asset.Id);
        Assert.That(updated, Is.Not.Null);
        var updatedDetails = (BtcLendingDetails)updated!.Details;
        Assert.That(updatedDetails.Status, Is.EqualTo(LoanStatus.Repaid));
    }

    [Test]
    public async Task HandleAsync_WithNonExistentAsset_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(new RepayLoanCommand { AssetId = "000000000000000000000000" });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSET_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyAssetId_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(new RepayLoanCommand { AssetId = "" });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonLoanAsset_ThrowsInvalidOperation()
    {
        // Arrange - create a basic (non-loan) asset
        var details = new BasicAssetDetails(
            AssetTypes.Stock, 10, "AAPL", AssetPriceSource.Manual, 150m, "USD");
        var asset = Asset.New(new AssetName("Apple Stock"), details, Icon.Empty);
        await _assetRepository.SaveAsync(asset);

        // Act & Assert - handler should throw for non-loan assets
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(new RepayLoanCommand { AssetId = asset.Id.Value }));
    }
}
