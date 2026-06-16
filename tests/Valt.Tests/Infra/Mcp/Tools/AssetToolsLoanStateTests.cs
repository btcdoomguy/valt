using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Valt.App;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;
using Valt.Infra.Mcp.Tools;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Mcp.Tools;

[TestFixture]
public class AssetToolsLoanStateTests : IntegrationTest
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
    public new void SetUp()
    {
        _notificationPublisher = Substitute.For<INotificationPublisher>();
        ReplaceService(_notificationPublisher);

        _commandDispatcher = _serviceProvider.GetRequiredService<ICommandDispatcher>();
        _queryDispatcher = _serviceProvider.GetRequiredService<IQueryDispatcher>();
        _assetRepository = _serviceProvider.GetRequiredService<IAssetRepository>();
    }

    [Test]
    public async Task LoanStateTools_AddQueryDeleteAndNotify()
    {
        var asset = AssetBuilder.ABtcLoan().WithSeededSnapshot().Build();
        var loanDetails = (BtcLoanDetails)asset.Details;
        var seededSnapshotDebt = loanDetails.Snapshots[0].CurrentTotalDebt;

        await _assetRepository.SaveAsync(asset);

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var addResult = await AssetTools.AddLoanStateUpdate(
            _commandDispatcher,
            _notificationPublisher,
            asset.Id.Value,
            today,
            20_000m,
            100_000_000,
            0.10m,
            100m,
            "Updated state");

        Assert.That(addResult, Does.Not.StartWith("Error:"));

        var latest = await AssetTools.GetLatestLoanState(_queryDispatcher, asset.Id.Value);
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.CurrentTotalDebt, Is.EqualTo(20_000m));

        var timeline = await AssetTools.GetLoanStateTimeline(_queryDispatcher, asset.Id.Value);
        Assert.That(timeline.Count, Is.GreaterThanOrEqualTo(2));

        var deleteResult = await AssetTools.DeleteLoanStateUpdate(
            _commandDispatcher,
            _notificationPublisher,
            asset.Id.Value,
            today);

        Assert.That(deleteResult, Does.Not.StartWith("Error:"));

        latest = await AssetTools.GetLatestLoanState(_queryDispatcher, asset.Id.Value);
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.CurrentTotalDebt, Is.EqualTo(seededSnapshotDebt));

        await _notificationPublisher.Received().PublishAsync(Arg.Any<McpDataChangedNotification>());
    }

    [Test]
    public async Task LoanStateTools_AddWithInvalidEffectiveDate_ReturnsCleanError()
    {
        var asset = AssetBuilder.ABtcLoan().WithSeededSnapshot().Build();
        await _assetRepository.SaveAsync(asset);

        var result = await AssetTools.AddLoanStateUpdate(
            _commandDispatcher,
            _notificationPublisher,
            asset.Id.Value,
            "not-a-date",
            20_000m,
            100_000_000,
            0.10m,
            100m);

        Assert.That(result, Does.StartWith("Error:"));
        Assert.That(result, Contains.Substring("yyyy-MM-dd"));
        await _notificationPublisher.DidNotReceive().PublishAsync(Arg.Any<McpDataChangedNotification>());
    }

    [Test]
    public async Task LoanStateTools_DeleteWithInvalidEffectiveDate_ReturnsCleanError()
    {
        var asset = AssetBuilder.ABtcLoan().WithSeededSnapshot().Build();
        await _assetRepository.SaveAsync(asset);

        var result = await AssetTools.DeleteLoanStateUpdate(
            _commandDispatcher,
            _notificationPublisher,
            asset.Id.Value,
            "not-a-date");

        Assert.That(result, Does.StartWith("Error:"));
        Assert.That(result, Contains.Substring("yyyy-MM-dd"));
        await _notificationPublisher.DidNotReceive().PublishAsync(Arg.Any<McpDataChangedNotification>());
    }
}
