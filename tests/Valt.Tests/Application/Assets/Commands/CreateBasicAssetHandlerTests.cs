using Valt.App.Modules.Assets.Commands.CreateBasicAsset;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class CreateBasicAssetHandlerTests : DatabaseTest
{
    private CreateBasicAssetHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateBasicAssetHandler(
            _assetRepository,
            new CreateBasicAssetValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidStockAsset_CreatesAsset()
    {
        var command = new CreateBasicAssetCommand
        {
            Name = "Apple Stock",
            AssetType = 0, // Stock
            CurrencyCode = "USD",
            Symbol = "AAPL",
            Quantity = 10,
            CurrentPrice = 150m,
            PriceSource = 0, // Manual
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
    public async Task HandleAsync_WithValidEtfAsset_CreatesAsset()
    {
        var command = new CreateBasicAssetCommand
        {
            Name = "S&P 500 ETF",
            AssetType = 1, // ETF
            CurrencyCode = "USD",
            Symbol = "SPY",
            Quantity = 5,
            CurrentPrice = 450m,
            PriceSource = 1, // YahooFinance
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithValidCryptoAsset_CreatesAsset()
    {
        var command = new CreateBasicAssetCommand
        {
            Name = "Ethereum",
            AssetType = 2, // Crypto
            CurrencyCode = "USD",
            Symbol = "ETH",
            Quantity = 2,
            CurrentPrice = 2500m,
            PriceSource = 0, // Manual
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new CreateBasicAssetCommand
        {
            Name = "",
            AssetType = 0,
            CurrencyCode = "USD",
            Symbol = "AAPL",
            Quantity = 10,
            CurrentPrice = 150m,
            PriceSource = 0,
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
        var command = new CreateBasicAssetCommand
        {
            Name = "Test Asset",
            AssetType = 0,
            CurrencyCode = "INVALID",
            Symbol = "TEST",
            Quantity = 10,
            CurrentPrice = 100m,
            PriceSource = 0,
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
    public async Task HandleAsync_WithZeroQuantity_CreatesAsset()
    {
        // Zero quantity is allowed (user may want to track an asset they don't own yet)
        var command = new CreateBasicAssetCommand
        {
            Name = "Test Asset",
            AssetType = 0,
            CurrencyCode = "USD",
            Symbol = "TEST",
            Quantity = 0,
            CurrentPrice = 100m,
            PriceSource = 0,
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithNegativeQuantity_ReturnsValidationError()
    {
        var command = new CreateBasicAssetCommand
        {
            Name = "Test Asset",
            AssetType = 0,
            CurrencyCode = "USD",
            Symbol = "TEST",
            Quantity = -10,
            CurrentPrice = 100m,
            PriceSource = 0,
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
    public async Task HandleAsync_WithNegativePrice_ReturnsValidationError()
    {
        var command = new CreateBasicAssetCommand
        {
            Name = "Test Asset",
            AssetType = 0,
            CurrencyCode = "USD",
            Symbol = "TEST",
            Quantity = 10,
            CurrentPrice = -100m,
            PriceSource = 0,
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
    public async Task HandleAsync_WithEmptySymbol_CreatesAsset()
    {
        // Empty symbol is allowed for custom assets
        var command = new CreateBasicAssetCommand
        {
            Name = "Test Asset",
            AssetType = 6, // Custom
            CurrencyCode = "USD",
            Symbol = "",
            Quantity = 10,
            CurrentPrice = 100m,
            PriceSource = 0,
            IncludeInNetWorth = true,
            Visible = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithIcon_CreatesAssetWithIcon()
    {
        var command = new CreateBasicAssetCommand
        {
            Name = "Apple Stock",
            AssetType = 0,
            CurrencyCode = "USD",
            Symbol = "AAPL",
            Quantity = 10,
            CurrentPrice = 150m,
            PriceSource = 0,
            IncludeInNetWorth = true,
            Visible = true,
            Icon = "trending_up_E8E5_#00FF00"
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }
}
