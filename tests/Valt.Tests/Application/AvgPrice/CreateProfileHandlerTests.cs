using Valt.App.Modules.AvgPrice.Commands.CreateProfile;

namespace Valt.Tests.Application.AvgPrice;

[TestFixture]
public class CreateProfileHandlerTests : DatabaseTest
{
    private CreateProfileHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateProfileHandler(
            _avgPriceRepository,
            new CreateProfileValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidCommand_CreatesProfile()
    {
        var command = new CreateProfileCommand
        {
            Name = "Bitcoin Holdings",
            AssetName = "BTC",
            Precision = 8,
            Visible = true,
            CurrencyCode = "USD",
            CalculationMethodId = 0 // BrazilianRule
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.ProfileId, Is.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_WithFifoMethod_CreatesProfile()
    {
        var command = new CreateProfileCommand
        {
            Name = "Stock Portfolio",
            AssetName = "AAPL",
            Precision = 4,
            Visible = true,
            CurrencyCode = "USD",
            CalculationMethodId = 1 // Fifo
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new CreateProfileCommand
        {
            Name = "",
            AssetName = "BTC",
            Precision = 8,
            Visible = true,
            CurrencyCode = "USD",
            CalculationMethodId = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithInvalidCurrency_ReturnsError()
    {
        var command = new CreateProfileCommand
        {
            Name = "Test",
            AssetName = "BTC",
            Precision = 8,
            Visible = true,
            CurrencyCode = "INVALID",
            CalculationMethodId = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_CURRENCY"));
        });
    }

    [Test]
    public async Task HandleAsync_WithInvalidCalculationMethod_ReturnsValidationError()
    {
        var command = new CreateProfileCommand
        {
            Name = "Test",
            AssetName = "BTC",
            Precision = 8,
            Visible = true,
            CurrencyCode = "USD",
            CalculationMethodId = 99
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
