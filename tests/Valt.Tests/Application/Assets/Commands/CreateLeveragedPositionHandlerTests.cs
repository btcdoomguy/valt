using Valt.App.Modules.Assets.Commands.CreateLeveragedPosition;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class CreateLeveragedPositionHandlerTests : DatabaseTest
{
    private CreateLeveragedPositionHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateLeveragedPositionHandler(
            _assetRepository,
            new CreateLeveragedPositionValidator());
    }

    [Test]
    public async Task HandleAsync_WithCollateralMode_CreatesAsset()
    {
        var command = new CreateLeveragedPositionCommand
        {
            Name = "BTC Long 10x",
            CurrencyCode = "USD",
            Symbol = "BTC",
            Collateral = 1000m,
            EntryPrice = 50000m,
            CurrentPrice = 55000m,
            Leverage = 10m,
            LiquidationPrice = 45000m,
            IsLong = true,
            InputMode = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.AssetId, Is.Not.Null.And.Not.Empty);
        });

        // Verify stored collateral matches input
        var asset = await _assetRepository.GetByIdAsync(new AssetId(result.Value!.AssetId));
        var details = (LeveragedPositionDetails)asset!.Details;
        Assert.That(details.Collateral, Is.EqualTo(1000m));
        Assert.That(details.InputMode, Is.EqualTo(LeveragedPositionInputMode.Collateral));
    }

    [Test]
    public async Task HandleAsync_WithExactPositionMode_CalculatesCollateral()
    {
        // Position size of 0.2 BTC at entry price 50000 with 10x leverage
        // Expected collateral = 0.2 * 50000 / 10 = 1000
        var command = new CreateLeveragedPositionCommand
        {
            Name = "BTC Long 10x",
            CurrencyCode = "USD",
            Symbol = "BTC",
            Collateral = 0, // Not used in ExactPosition mode
            EntryPrice = 50000m,
            CurrentPrice = 55000m,
            Leverage = 10m,
            LiquidationPrice = 45000m,
            IsLong = true,
            InputMode = 1,
            PositionSize = 0.2m
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var asset = await _assetRepository.GetByIdAsync(new AssetId(result.Value!.AssetId));
        var details = (LeveragedPositionDetails)asset!.Details;
        Assert.Multiple(() =>
        {
            Assert.That(details.Collateral, Is.EqualTo(1000m));
            Assert.That(details.PositionSize, Is.EqualTo(0.2m));
            Assert.That(details.InputMode, Is.EqualTo(LeveragedPositionInputMode.ExactPosition));
        });
    }

    [Test]
    public async Task HandleAsync_WithExactPositionMode_InvalidPositionSize_ReturnsFailure()
    {
        var command = new CreateLeveragedPositionCommand
        {
            Name = "BTC Long 10x",
            CurrencyCode = "USD",
            Symbol = "BTC",
            Collateral = 0,
            EntryPrice = 50000m,
            CurrentPrice = 55000m,
            Leverage = 10m,
            LiquidationPrice = 45000m,
            IsLong = true,
            InputMode = 1,
            PositionSize = 0 // Invalid
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithCollateralMode_InvalidCollateral_ReturnsFailure()
    {
        var command = new CreateLeveragedPositionCommand
        {
            Name = "BTC Long 10x",
            CurrencyCode = "USD",
            Symbol = "BTC",
            Collateral = 0, // Invalid for collateral mode
            EntryPrice = 50000m,
            CurrentPrice = 55000m,
            Leverage = 10m,
            LiquidationPrice = 45000m,
            IsLong = true,
            InputMode = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithExactPositionMode_ShortPosition_CalculatesCollateral()
    {
        // Short 5x with 2 BTC position at entry 40000
        // Expected collateral = 2 * 40000 / 5 = 16000
        var command = new CreateLeveragedPositionCommand
        {
            Name = "BTC Short 5x",
            CurrencyCode = "USD",
            Symbol = "BTC",
            Collateral = 0,
            EntryPrice = 40000m,
            CurrentPrice = 38000m,
            Leverage = 5m,
            LiquidationPrice = 48000m,
            IsLong = false,
            InputMode = 1,
            PositionSize = 2m
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var asset = await _assetRepository.GetByIdAsync(new AssetId(result.Value!.AssetId));
        var details = (LeveragedPositionDetails)asset!.Details;
        Assert.Multiple(() =>
        {
            Assert.That(details.Collateral, Is.EqualTo(16000m));
            Assert.That(details.PositionSize, Is.EqualTo(2m));
        });
    }
}
