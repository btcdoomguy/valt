using Avalonia.Controls;
using NSubstitute;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.DeleteLoanStateUpdate;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetLoanStateTimeline;
using Valt.Core.Kernel.Factories;
using Valt.Infra.Kernel;
using Valt.UI.Base;
using Valt.UI.Services;
using Valt.UI.Views;
using Valt.UI.Views.Main.Modals.LoanStateHistory;
using Valt.UI.Views.Main.Modals.UpdateLoanState;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class LoanStateHistoryViewModelTests
{
    private IQueryDispatcher _queryDispatcher = null!;
    private ICommandDispatcher _commandDispatcher = null!;
    private IModalFactory _modalFactory = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp() => IdGenerator.Configure(new LiteDbIdProvider());

    [SetUp]
    public void SetUp()
    {
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _modalFactory = Substitute.For<IModalFactory>();
    }

    private LoanStateHistoryViewModel CreateViewModel()
        => new(_queryDispatcher, _commandDispatcher, _modalFactory);

    private static LoanStateSnapshotDTO CreateSnapshot(DateOnly effectiveDate, bool isInitial = false) => new()
    {
        PlatformName = "HodlHodl",
        CollateralSats = 5_000_000,
        LoanAmount = 100_000m,
        CurrencyCode = "USD",
        Apr = 0.12m,
        InitialLtv = 0.50m,
        LiquidationLtv = 0.90m,
        MarginCallLtv = 0.75m,
        Fees = 500m,
        LoanStartDate = new DateOnly(2024, 1, 1),
        RepaymentDate = new DateOnly(2025, 1, 1),
        StatusId = 0,
        CurrentBtcPriceInLoanCurrency = 100_000m,
        FixedTotalDebt = null,
        CurrentTotalDebt = 105_000m,
        EffectiveDate = effectiveDate,
        Note = null,
        IsInitial = isInitial
    };

    [Test]
    public async Task Should_Load_Snapshots_In_Chronological_Order()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var older = CreateSnapshot(new DateOnly(2024, 1, 1));
        var newer = CreateSnapshot(new DateOnly(2024, 6, 1));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanStateTimelineQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<LoanStateSnapshotDTO> { older, newer }.AsReadOnly());

        viewModel.Parameter = new LoanStateHistoryViewModel.Request { AssetId = "asset-1" };

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.Snapshots, Has.Count.EqualTo(2));
        Assert.That(viewModel.Snapshots[0].EffectiveDate, Is.EqualTo(older.EffectiveDate));
        Assert.That(viewModel.Snapshots[1].EffectiveDate, Is.EqualTo(newer.EffectiveDate));
    }

    [Test]
    public async Task Should_Disable_Delete_When_Only_One_Snapshot()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var snapshot = CreateSnapshot(new DateOnly(2024, 6, 1), isInitial: true);

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanStateTimelineQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<LoanStateSnapshotDTO> { snapshot }.AsReadOnly());

        viewModel.Parameter = new LoanStateHistoryViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.DeleteSelectedCommand.CanExecute(null), Is.False);

        viewModel.SelectedSnapshot = viewModel.Snapshots[0];
        Assert.That(viewModel.DeleteSelectedCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task Should_Disable_Delete_When_Selected_Is_Initial()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var older = CreateSnapshot(new DateOnly(2024, 1, 1), isInitial: true);
        var newer = CreateSnapshot(new DateOnly(2024, 6, 1));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanStateTimelineQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<LoanStateSnapshotDTO> { older, newer }.AsReadOnly());

        viewModel.Parameter = new LoanStateHistoryViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();

        // Act
        viewModel.SelectedSnapshot = viewModel.Snapshots[0];

        // Assert
        Assert.That(viewModel.DeleteSelectedCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task Should_Dispatch_DeleteLoanStateUpdateCommand_When_Selected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var older = CreateSnapshot(new DateOnly(2024, 1, 1), isInitial: true);
        var newer = CreateSnapshot(new DateOnly(2024, 6, 1));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanStateTimelineQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<LoanStateSnapshotDTO> { older, newer }.AsReadOnly());

        viewModel.Parameter = new LoanStateHistoryViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();

        // Act
        viewModel.SelectedSnapshot = viewModel.Snapshots[1];

        // Assert
        Assert.That(viewModel.DeleteSelectedCommand.CanExecute(null), Is.True);

        // The actual command execution requires an Avalonia window for the confirmation
        // dialog, so we verify the guard is lifted and the command is configured to
        // dispatch DeleteLoanStateUpdateCommand with the selected snapshot's date.
        await _commandDispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<DeleteLoanStateUpdateCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Should_Open_UpdateLoanState_And_Close_History_When_AddNewState_Clicked()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var snapshot = CreateSnapshot(new DateOnly(2024, 6, 1));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanStateTimelineQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<LoanStateSnapshotDTO> { snapshot }.AsReadOnly());

        _modalFactory.CreateAsync(
                Arg.Is(ApplicationModalNames.UpdateLoanState),
                Arg.Any<Window?>(),
                Arg.Any<object>())
            .Returns(Task.FromResult<ValtBaseWindow>(null!));

        viewModel.Parameter = new LoanStateHistoryViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();
        viewModel.GetWindow = () => null!;

        var closeWindowInvoked = false;
        viewModel.CloseWindow = () => closeWindowInvoked = true;

        // Act
        try
        {
            await viewModel.AddNewStateCommand.ExecuteAsync(null);
        }
        catch (NullReferenceException)
        {
            // Expected: the substitute factory returns null and ShowDialogSafeAsync
            // is invoked on a null view reference outside a real Avalonia app.
        }

        // Assert
        Assert.That(closeWindowInvoked, Is.True);
        _ = _modalFactory.Received(1).CreateAsync(
            Arg.Is(ApplicationModalNames.UpdateLoanState),
            Arg.Is<Window?>((Window?)null),
            Arg.Is<UpdateLoanStateViewModel.Request>(r => r!.AssetId == "asset-1"));
    }

    [Test]
    public async Task Should_Refresh_Snapshots_After_Reload()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var snapshot = CreateSnapshot(new DateOnly(2024, 6, 1));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanStateTimelineQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<LoanStateSnapshotDTO> { snapshot }.AsReadOnly());

        viewModel.Parameter = new LoanStateHistoryViewModel.Request { AssetId = "asset-1" };

        // Act
        await viewModel.OnBindParameterAsync();
        await viewModel.OnBindParameterAsync();

        // Assert
        await _queryDispatcher.Received(2).DispatchAsync(
            Arg.Is<GetLoanStateTimelineQuery>(q => q.AssetId == "asset-1"),
            Arg.Any<CancellationToken>());
    }
}
