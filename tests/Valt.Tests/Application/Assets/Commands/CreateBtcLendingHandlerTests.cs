using Valt.App.Modules.Assets.Commands.CreateBtcLending;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class CreateBtcLendingHandlerTests : DatabaseTest
{
    private CreateBtcLendingHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateBtcLendingHandler(
            _assetRepository,
            new CreateBtcLendingValidator());
    }

    private static CreateBtcLendingCommand ValidCommand() => new()
    {
        Name = "P2P Lending",
        CurrencyCode = "USD",
        AmountLent = 10_000m,
        Apr = 0.05m,
        BorrowerOrPlatformName = "Ledn",
        LendingStartDate = new DateOnly(2025, 1, 1),
        ExpectedRepaymentDate = new DateOnly(2026, 1, 1),
        IncludeInNetWorth = true,
        Visible = true
    };

    [Test]
    public async Task HandleAsync_WithValidCommand_CreatesAsset()
    {
        var result = await _handler.HandleAsync(ValidCommand());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.AssetId, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_WithNoRepaymentDate_CreatesAsset()
    {
        var command = ValidCommand() with { ExpectedRepaymentDate = null };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = ValidCommand() with { Name = "" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithInvalidCurrencyCode_ReturnsError()
    {
        var command = ValidCommand() with { CurrencyCode = "INVALID" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_CURRENCY"));
        });
    }

    [Test]
    public async Task HandleAsync_WithZeroAmount_ReturnsValidationError()
    {
        var command = ValidCommand() with { AmountLent = 0 };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNegativeApr_ReturnsValidationError()
    {
        var command = ValidCommand() with { Apr = -0.01m };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyBorrowerName_ReturnsValidationError()
    {
        var command = ValidCommand() with { BorrowerOrPlatformName = "" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithIcon_CreatesAsset()
    {
        var command = ValidCommand() with { Icon = "trending_up_E8E5_#00FF00" };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }
}
