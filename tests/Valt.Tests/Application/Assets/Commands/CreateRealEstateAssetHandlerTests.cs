using Valt.App.Modules.Assets.Commands.CreateRealEstateAsset;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class CreateRealEstateAssetHandlerTests : DatabaseTest
{
    private CreateRealEstateAssetHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateRealEstateAssetHandler(
            _assetRepository,
            new CreateRealEstateAssetValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidRealEstateAsset_CreatesAsset()
    {
        var command = new CreateRealEstateAssetCommand
        {
            Name = "Main Residence",
            CurrencyCode = "USD",
            CurrentValue = 500000m,
            Address = "123 Main Street",
            MonthlyRentalIncome = null,
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.AssetId, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_WithRentalProperty_CreatesAsset()
    {
        var command = new CreateRealEstateAssetCommand
        {
            Name = "Rental Property",
            CurrencyCode = "BRL",
            CurrentValue = 800000m,
            Address = "456 Investment Ave",
            MonthlyRentalIncome = 3500m,
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithoutAddress_CreatesAsset()
    {
        var command = new CreateRealEstateAssetCommand
        {
            Name = "Investment Property",
            CurrencyCode = "EUR",
            CurrentValue = 300000m,
            Address = null,
            MonthlyRentalIncome = null,
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new CreateRealEstateAssetCommand
        {
            Name = "",
            CurrencyCode = "USD",
            CurrentValue = 500000m,
            Address = "123 Main Street",
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithZeroValue_CreatesAsset()
    {
        // Zero value is allowed (property may be worthless or value unknown)
        var command = new CreateRealEstateAssetCommand
        {
            Name = "Property",
            CurrencyCode = "USD",
            CurrentValue = 0m,
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithNegativeValue_ReturnsValidationError()
    {
        var command = new CreateRealEstateAssetCommand
        {
            Name = "Property",
            CurrencyCode = "USD",
            CurrentValue = -500000m,
            IncludeInNetWorth = true,
            Visible = true
        };

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
        var command = new CreateRealEstateAssetCommand
        {
            Name = "Property",
            CurrencyCode = "INVALID",
            CurrentValue = 500000m,
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_CURRENCY"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNegativeRentalIncome_ReturnsValidationError()
    {
        var command = new CreateRealEstateAssetCommand
        {
            Name = "Rental Property",
            CurrencyCode = "USD",
            CurrentValue = 500000m,
            MonthlyRentalIncome = -1000m,
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
