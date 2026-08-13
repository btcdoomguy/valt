using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using System.Linq;
using Valt.App;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.Core.Modules.Assets.Contracts;
using Valt.App.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;
using Valt.Infra.Mcp.Tools;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Mcp.Tools;

[TestFixture]
public class AssetToolsSoldStateTests : IntegrationTest
{
    private ICommandDispatcher _commandDispatcher = null!;
    private IQueryDispatcher _queryDispatcher = null!;
    private IAssetRepository _assetRepository = null!;
    private INotificationPublisher _notificationPublisher = null!;

    [OneTimeSetUp]
    public void AddApplicationLayer()
    {
        _serviceCollection.AddValtApp();
        RebuildServiceProvider();
    }

    [SetUp]
    public new async Task SetUp()
    {
        _notificationPublisher = Substitute.For<INotificationPublisher>();
        ReplaceService(_notificationPublisher);

        _commandDispatcher = _serviceProvider.GetRequiredService<ICommandDispatcher>();
        _queryDispatcher = _serviceProvider.GetRequiredService<IQueryDispatcher>();
        _assetRepository = _serviceProvider.GetRequiredService<IAssetRepository>();

        var existingAssets = (await _assetRepository.GetAllAsync()).ToList();
        foreach (var asset in existingAssets)
        {
            await _assetRepository.DeleteAsync(asset);
        }
    }

    [Test]
    public async Task MarkUndoAndListSoldAssets_SucceedsAndNotifies()
    {
        var asset = AssetBuilder.AStockAsset().Build();
        await _assetRepository.SaveAsync(asset);

        var today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
        var markResult = await AssetTools.MarkAssetAsSold(
            _commandDispatcher, _notificationPublisher, asset.Id.Value, today);

        Assert.That(markResult, Does.Not.StartWith("Error:"));

        var soldAssets = await AssetTools.ListSoldAssets(_queryDispatcher);
        Assert.That(soldAssets, Has.Count.EqualTo(1));

        var undoResult = await AssetTools.UndoAssetSale(
            _commandDispatcher, _notificationPublisher, asset.Id.Value);

        Assert.That(undoResult, Does.Not.StartWith("Error:"));

        soldAssets = await AssetTools.ListSoldAssets(_queryDispatcher);
        Assert.That(soldAssets, Is.Empty);

        await _notificationPublisher.Received(2).PublishAsync(Arg.Any<McpDataChangedNotification>());
    }

    [Test]
    public async Task MarkAssetAsSold_WithInvalidDate_ReturnsCleanError()
    {
        var asset = AssetBuilder.AStockAsset().Build();
        await _assetRepository.SaveAsync(asset);

        var result = await AssetTools.MarkAssetAsSold(
            _commandDispatcher, _notificationPublisher, asset.Id.Value, "not-a-date");

        Assert.That(result, Does.StartWith("Error:"));
        Assert.That(result, Contains.Substring("yyyy-MM-dd"));
        await _notificationPublisher.DidNotReceive().PublishAsync(Arg.Any<McpDataChangedNotification>());
    }

    [Test]
    public async Task MarkAssetAsSold_AlreadySold_ReturnsError()
    {
        var asset = AssetBuilder.AStockAsset().Build();
        await _assetRepository.SaveAsync(asset);

        var today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
        var firstResult = await AssetTools.MarkAssetAsSold(
            _commandDispatcher, _notificationPublisher, asset.Id.Value, today);

        Assert.That(firstResult, Does.Not.StartWith("Error:"));

        var secondResult = await AssetTools.MarkAssetAsSold(
            _commandDispatcher, _notificationPublisher, asset.Id.Value, today);

        Assert.That(secondResult, Does.StartWith("Error:"));
        await _notificationPublisher.Received(1).PublishAsync(Arg.Any<McpDataChangedNotification>());
    }
}
