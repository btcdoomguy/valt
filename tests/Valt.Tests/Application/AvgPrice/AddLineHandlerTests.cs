using Valt.App.Modules.AvgPrice.Commands.AddLine;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.AvgPrice;

[TestFixture]
public class AddLineHandlerTests : DatabaseTest
{
    private AddLineHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new AddLineHandler(
            _avgPriceRepository,
            new AddLineValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidBuyLine_AddsLine()
    {
        // Create a profile first
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithName("Test Profile")
            .Build();
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        var command = new AddLineCommand
        {
            ProfileId = profile.Id.Value,
            Date = new DateOnly(2024, 1, 15),
            LineTypeId = 0, // Buy
            Quantity = 1.5m,
            Amount = 50000m,
            Comment = "First purchase"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.LineId, Is.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_WithSellLine_AddsLine()
    {
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithName("Test Profile")
            .Build();
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        // First add a buy line so there's something to sell
        var buyCommand = new AddLineCommand
        {
            ProfileId = profile.Id.Value,
            Date = new DateOnly(2024, 1, 10),
            LineTypeId = 0, // Buy
            Quantity = 1.0m,
            Amount = 50000m
        };
        await _handler.HandleAsync(buyCommand);

        var command = new AddLineCommand
        {
            ProfileId = profile.Id.Value,
            Date = new DateOnly(2024, 1, 15),
            LineTypeId = 1, // Sell
            Quantity = 0.5m,
            Amount = 20000m
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithNonExistentProfile_ReturnsNotFound()
    {
        var command = new AddLineCommand
        {
            ProfileId = "000000000000000000000001",
            Date = new DateOnly(2024, 1, 15),
            LineTypeId = 0,
            Quantity = 1m,
            Amount = 1000m
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("PROFILE_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithZeroQuantity_ReturnsValidationError()
    {
        var command = new AddLineCommand
        {
            ProfileId = "000000000000000000000001",
            Date = new DateOnly(2024, 1, 15),
            LineTypeId = 0,
            Quantity = 0m,
            Amount = 1000m
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
