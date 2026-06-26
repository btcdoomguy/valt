using Valt.App.Modules.Assets.Commands.AddLoanStateUpdate;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class AddLoanStateUpdateHandlerTests : DatabaseTest
{
    private AddLoanStateUpdateHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new AddLoanStateUpdateHandler(
            _assetRepository,
            new AddLoanStateUpdateValidator());
    }

    private static AddLoanStateUpdateCommand ValidCommand(string assetId, DateOnly? effectiveDate = null) => new()
    {
        AssetId = assetId,
        EffectiveDate = effectiveDate ?? new DateOnly(2025, 6, 1),
        TotalBorrowed = 25_000m,
        InterestAccruedUntilDate = 900m,
        CollateralSats = 110_000_000,
        Apr = 0.13m,
        Fees = 150m,
        Note = "Updated state"
    };

    [Test]
    public async Task HandleAsync_WithValidCommand_AppendsSnapshot()
    {
        var asset = AssetBuilder.ABtcLoan().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value);
        var result = await _handler.HandleAsync(command);

        var saved = await _assetRepository.GetByIdAsync(asset.Id);
        var details = (BtcLoanDetails)saved!.Details;
        var latest = details.Snapshots.MaxBy(s => s.EffectiveDate)!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(details.Snapshots, Has.Count.EqualTo(2));
            Assert.That(latest.EffectiveDate, Is.EqualTo(command.EffectiveDate));
            Assert.That(latest.TotalBorrowed, Is.EqualTo(command.TotalBorrowed));
            Assert.That(latest.InterestAccruedUntilDate, Is.EqualTo(command.InterestAccruedUntilDate));
            Assert.That(latest.CurrentTotalDebt, Is.EqualTo(command.TotalBorrowed + command.InterestAccruedUntilDate + command.Fees));
            Assert.That(latest.CollateralSats, Is.EqualTo(command.CollateralSats));
            Assert.That(latest.Apr, Is.EqualTo(command.Apr));
            Assert.That(latest.Fees, Is.EqualTo(command.Fees));
            Assert.That(latest.Note, Is.EqualTo(command.Note));
        });
    }

    [Test]
    public async Task HandleAsync_WithNoSnapshots_EffectiveDateMustBeAfterLoanStartDate()
    {
        var asset = AssetBuilder.ABtcLoan(loanAmount: 25_000m).Build();
        await _assetRepository.SaveAsync(asset);

        var details = (BtcLoanDetails)asset.Details;
        var command = ValidCommand(asset.Id.Value, details.LoanStartDate);
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithExistingSnapshot_EqualDate_ReturnsValidationFailed()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(new DateOnly(2025, 6, 1), 26_000m)
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value, new DateOnly(2025, 6, 1));
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithExistingSnapshot_EarlierDate_ReturnsValidationFailed()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(new DateOnly(2025, 6, 1), 26_000m)
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value, new DateOnly(2025, 5, 1));
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithExistingSnapshot_LaterDate_AppendsSnapshot()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(new DateOnly(2025, 6, 1), 26_000m)
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value, new DateOnly(2025, 7, 1));
        var result = await _handler.HandleAsync(command);

        var saved = await _assetRepository.GetByIdAsync(asset.Id);
        var details = (BtcLoanDetails)saved!.Details;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(details.Snapshots, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task HandleAsync_WithNegativeTotalBorrowed_ReturnsValidationFailed()
    {
        var asset = AssetBuilder.ABtcLoan().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value) with { TotalBorrowed = -100m };
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNegativeInterestAccrued_ReturnsValidationFailed()
    {
        var asset = AssetBuilder.ABtcLoan().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value) with { InterestAccruedUntilDate = -100m };
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithZeroCollateralSats_ReturnsValidationFailed()
    {
        var asset = AssetBuilder.ABtcLoan().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value) with { CollateralSats = 0 };
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNegativeApr_ReturnsValidationFailed()
    {
        var asset = AssetBuilder.ABtcLoan().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value) with { Apr = -0.01m };
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNegativeFees_ReturnsValidationFailed()
    {
        var asset = AssetBuilder.ABtcLoan().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value) with { Fees = -50m };
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithMissingAsset_ReturnsNotFound()
    {
        var command = ValidCommand("000000000000000000000000");
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("ASSET_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithStockAsset_ReturnsInvalidAssetType()
    {
        var asset = AssetBuilder.AStockAsset().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value);
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_ASSET_TYPE"));
        });
    }

    [Test]
    public async Task HandleAsync_WithBtcLendingAsset_ReturnsInvalidAssetType()
    {
        var asset = AssetBuilder.ABtcLending().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value);
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_ASSET_TYPE"));
        });
    }

    [Test]
    public async Task HandleAsync_WithDefaultEffectiveDate_ReturnsValidationFailed()
    {
        var asset = AssetBuilder.ABtcLoan().Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value) with { EffectiveDate = default };
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyAssetId_ReturnsValidationFailed()
    {
        var command = ValidCommand("");
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_InheritsFieldsFromLatestSnapshot()
    {
        var asset = AssetBuilder.ABtcLoan()
            .WithSnapshot(
                new DateOnly(2025, 6, 1),
                26_000m,
                liquidationLtv: 85m,
                marginCallLtv: 75m,
                currentBtcPrice: 55_000m)
            .Build();
        await _assetRepository.SaveAsync(asset);

        var command = ValidCommand(asset.Id.Value, new DateOnly(2025, 7, 1));
        var result = await _handler.HandleAsync(command);

        var saved = await _assetRepository.GetByIdAsync(asset.Id);
        var details = (BtcLoanDetails)saved!.Details;
        var latest = details.Snapshots.MaxBy(s => s.EffectiveDate)!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(latest.LiquidationLtv, Is.EqualTo(85m));
            Assert.That(latest.MarginCallLtv, Is.EqualTo(75m));
            Assert.That(latest.CurrentBtcPriceInLoanCurrency, Is.EqualTo(55_000m));
        });
    }
}
