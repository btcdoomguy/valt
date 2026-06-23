using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Kernel.Queries;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Mcp.Server;
using Valt.Infra.Services.CsvExport;
using Valt.Infra.Settings;
using Valt.UI.Services;
using Valt.UI.Services.LocalStorage;
using Valt.UI.State;
using Valt.UI.UserControls;
using Valt.UI.Views;
using Valt.UI.Views.Main;
using Valt.UI.Views.Main.Controls;
using Valt.UI.Views.Main.Modals.InputPassword;

namespace Valt.Tests.UI.ViewModels;

[TestFixture]
public class MainViewModelModalCommandTests
{
    private IModalLauncher _modalLauncher = null!;
    private Window _owner = null!;
    private MainViewModel _viewModel = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        AppBuilder.Configure<global::Avalonia.Application>()
            .UsePlatformDetect()
            .SetupWithoutStarting();
    }

    [SetUp]
    public void SetUp()
    {
        _modalLauncher = Substitute.For<IModalLauncher>();
        _owner = new Window();
        _viewModel = CreateMainViewModel(_modalLauncher);
        _viewModel.Window = _owner;
    }

    [TearDown]
    public void TearDown()
    {
        _viewModel?.Dispose();
    }

    [Test]
    public async Task OpenPriceHistoryCommand_Should_Use_Launcher_With_Correct_Modal()
    {
        await _viewModel.OpenPriceHistoryCommand.ExecuteAsync(null);

        await _modalLauncher.Received(1).ShowAsync(
            Arg.Is(ApplicationModalNames.PriceHistory),
            Arg.Is(_owner));
    }

    [Test]
    public async Task ManageCategoriesCommand_Should_Use_Launcher_With_Correct_Modal()
    {
        await _viewModel.ManageCategoriesCommand.ExecuteAsync(null);

        await _modalLauncher.Received(1).ShowAsync(
            Arg.Is(ApplicationModalNames.ManageCategories),
            Arg.Is(_owner));
    }

    [Test]
    public async Task AboutCommand_Should_Use_Launcher_With_Correct_Modal()
    {
        await _viewModel.AboutCommand.ExecuteAsync(null);

        await _modalLauncher.Received(1).ShowAsync(
            Arg.Is(ApplicationModalNames.About),
            Arg.Is(_owner));
    }

    [Test]
    public async Task ToggleSecureModeCommand_WhenEnabled_Should_Use_Generic_Launcher_And_HideCheckbox()
    {
        var secureModeState = new SecureModeState();
        secureModeState.SetPassword("password");
        secureModeState.IsEnabled = true;

        var viewModel = CreateMainViewModel(_modalLauncher, secureModeState);
        viewModel.Window = _owner;

        var configuredVm = new InputPasswordViewModel();

        _modalLauncher
            .ShowAsync<InputPasswordViewModel, InputPasswordViewModel.Response?>(
                Arg.Is(ApplicationModalNames.InputPassword),
                Arg.Is(_owner),
                Arg.Do<Action<InputPasswordViewModel>>(configure => configure(configuredVm)),
                Arg.Any<object?>())
            .Returns(Task.FromResult<InputPasswordViewModel.Response?>(new InputPasswordViewModel.Response("password", false)));

        await viewModel.ToggleSecureModeCommand.ExecuteAsync(null);

        await _modalLauncher.Received(1).ShowAsync<InputPasswordViewModel, InputPasswordViewModel.Response?>(
            Arg.Is(ApplicationModalNames.InputPassword),
            Arg.Is(_owner),
            Arg.Any<Action<InputPasswordViewModel>>(),
            Arg.Any<object?>());

        Assert.That(configuredVm.HideSecureModeCheckbox, Is.True);

        viewModel.Dispose();
    }

    private static MainViewModel CreateMainViewModel(IModalLauncher modalLauncher, SecureModeState? secureModeState = null)
    {
        var pageFactory = Substitute.For<IPageFactory>();
        var dbLifecycle = Substitute.For<IDatabaseLifecycleService>();
        var localDatabase = Substitute.For<ILocalDatabase>();
        var priceDatabase = Substitute.For<IPriceDatabase>();
        var localHistoricalPriceProvider = Substitute.For<ILocalHistoricalPriceProvider>();
        var notificationPublisher = Substitute.For<INotificationPublisher>();
        var queryDispatcher = Substitute.For<IQueryDispatcher>();
        var currencySettings = new CurrencySettings(localDatabase, notificationPublisher);
        var backgroundJobManager = new BackgroundJobManager([]);
        var liveRatesViewModel = new LiveRatesViewModel();
        var updateIndicatorViewModel = new UpdateIndicatorViewModel();
        var ratesState = new RatesState();
        var liveRateState = new LiveRateState(
            currencySettings,
            localDatabase,
            priceDatabase,
            localHistoricalPriceProvider,
            ratesState,
            Substitute.For<ILogger<LiveRateState>>());
        var csvExportService = Substitute.For<ICsvExportService>();
        var clock = Substitute.For<IClock>();
        var logger = Substitute.For<ILogger<MainViewModel>>();
        var secureMode = secureModeState ?? new SecureModeState();
        var mcpServerState = new McpServerState();
        var mcpServerService = Substitute.For<McpServerService>(
            mcpServerState,
            localDatabase,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<McpServerService>>());
        var displaySettings = new DisplaySettings(localDatabase, notificationPublisher);
        var tabRefreshState = new TabRefreshState();
        var localStorageService = Substitute.For<ILocalStorageService>();
        var accountsTotalState = new AccountsTotalState(
            currencySettings,
            ratesState,
            new CustomBtcPriceState(),
            queryDispatcher,
            Substitute.For<ILogger<AccountsTotalState>>());
        var filterState = new FilterState();

        return new MainViewModel(
            pageFactory,
            modalLauncher,
            dbLifecycle,
            currencySettings,
            backgroundJobManager,
            liveRatesViewModel,
            updateIndicatorViewModel,
            liveRateState,
            csvExportService,
            clock,
            logger,
            secureMode,
            mcpServerService,
            mcpServerState,
            displaySettings,
            tabRefreshState,
            localStorageService,
            accountsTotalState,
            filterState);
    }
}
