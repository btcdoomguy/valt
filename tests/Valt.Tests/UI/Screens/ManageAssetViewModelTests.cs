using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetAsset;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Assets;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.Assets.PriceProviders;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.UI.Services;
using Valt.UI.Views.Main.Modals.ManageAsset;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class ManageAssetViewModelTests
{
    private IQueryDispatcher _queryDispatcher;
    private ICommandDispatcher _commandDispatcher;
    private IAssetPriceProviderSelector _priceProviderSelector;
    private IConfigurationManager _configurationManager;
    private ILogger<ManageAssetViewModel> _logger;
    private ILocalDatabase _localDatabase;
    private INotificationPublisher _notificationPublisher;
    private CurrencySettings _currencySettings;
    private IModalFactory _modalFactory;

    [OneTimeSetUp]
    public void OneTimeSetUp() => IdGenerator.Configure(new LiteDbIdProvider());

    [SetUp]
    public void SetUp()
    {
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _priceProviderSelector = Substitute.For<IAssetPriceProviderSelector>();
        _configurationManager = Substitute.For<IConfigurationManager>();
        _logger = Substitute.For<ILogger<ManageAssetViewModel>>();
        _localDatabase = Substitute.For<ILocalDatabase>();
        _notificationPublisher = Substitute.For<INotificationPublisher>();
        _modalFactory = Substitute.For<IModalFactory>();
        _currencySettings = new CurrencySettings(_localDatabase, _notificationPublisher)
        {
            MainFiatCurrency = "USD"
        };
    }

    [TearDown]
    public void TearDown()
    {
        _localDatabase?.Dispose();
    }

    private ManageAssetViewModel CreateViewModel()
        => new(_queryDispatcher, _commandDispatcher, _priceProviderSelector, _currencySettings, _configurationManager, _logger, _modalFactory);

    private static AssetDTO CreateStockAssetDto(DateOnly? acquisitionDate = null) => new()
    {
        Id = "asset-1",
        Name = "AAPL",
        AssetTypeId = (int)AssetTypes.Stock,
        AssetTypeName = "Stock",
        Icon = "\xE87C",
        IncludeInNetWorth = true,
        Visible = true,
        LastPriceUpdateAt = DateTime.Now,
        CreatedAt = DateTime.Now,
        DisplayOrder = 1,
        CurrentPrice = 200m,
        CurrentValue = 2000m,
        CurrencyCode = "USD",
        Symbol = "AAPL",
        Quantity = 10,
        PriceSourceId = 0,
        AcquisitionDate = acquisitionDate,
        AcquisitionPrice = 150m
    };

    private static AssetDTO CreateRealEstateAssetDto(DateOnly? acquisitionDate = null) => new()
    {
        Id = "asset-2",
        Name = "Beach House",
        AssetTypeId = (int)AssetTypes.RealEstate,
        AssetTypeName = "Real Estate",
        Icon = "\xE88A",
        IncludeInNetWorth = true,
        Visible = true,
        LastPriceUpdateAt = DateTime.Now,
        CreatedAt = DateTime.Now,
        DisplayOrder = 2,
        CurrentPrice = 500_000m,
        CurrentValue = 500_000m,
        CurrencyCode = "USD",
        Address = "123 Ocean Dr",
        MonthlyRentalIncome = 2000m,
        AcquisitionDate = acquisitionDate,
        AcquisitionPrice = 400_000m
    };

    [Test]
    public async Task Should_Prefill_AcquisitionDate_For_Stock()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var asset = CreateStockAssetDto(new DateOnly(2024, 1, 15));

        _queryDispatcher.DispatchAsync(Arg.Any<GetAssetQuery>(), Arg.Any<CancellationToken>())
            .Returns(asset);

        viewModel.Parameter = "asset-1";

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.AcquisitionDate, Is.Not.Null);
        Assert.That(viewModel.AcquisitionDate!.Value.Date, Is.EqualTo(new DateTime(2024, 1, 15).Date));
    }

    [Test]
    public async Task Should_Prefill_AcquisitionDate_For_RealEstate()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var asset = CreateRealEstateAssetDto(new DateOnly(2020, 6, 1));

        _queryDispatcher.DispatchAsync(Arg.Any<GetAssetQuery>(), Arg.Any<CancellationToken>())
            .Returns(asset);

        viewModel.Parameter = "asset-2";

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.AcquisitionDate, Is.Not.Null);
        Assert.That(viewModel.AcquisitionDate!.Value.Date, Is.EqualTo(new DateTime(2020, 6, 1).Date));
    }

    [Test]
    public async Task Should_Leave_AcquisitionDate_Null_For_Stock_When_Not_Set()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var asset = CreateStockAssetDto(null);

        _queryDispatcher.DispatchAsync(Arg.Any<GetAssetQuery>(), Arg.Any<CancellationToken>())
            .Returns(asset);

        viewModel.Parameter = "asset-1";

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.AcquisitionDate, Is.Null);
    }
}
