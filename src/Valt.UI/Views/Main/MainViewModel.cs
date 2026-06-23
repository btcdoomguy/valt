using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Services.CsvExport;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.LocalStorage;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State;
using Valt.UI.State.Events;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Controls;
using Valt.UI.Views.Main.Modals.InitialSelection;
using Valt.UI.Views.Main.Modals.InputPassword;
using Valt.Infra.Mcp.Notifications;
using Valt.Infra.Mcp.Server;

namespace Valt.UI.Views.Main;

public partial class MainViewModel : ValtViewModel, IDisposable
{
    private readonly IPageFactory _pageFactory;
    private readonly IModalLauncher _modalLauncher;
    private readonly IDatabaseLifecycleService _dbLifecycle;
    private readonly CurrencySettings _currencySettings;
    private readonly BackgroundJobManager? _backgroundJobManager;
    private readonly LiveRatesViewModel _liveRatesViewModel = null!;
    private readonly UpdateIndicatorViewModel _updateIndicatorViewModel;
    private readonly ICsvExportService _csvExportService = null!;
    private readonly IClock _clock = null!;
    private readonly ILogger<MainViewModel> _logger = null!;
    private readonly SecureModeState _secureModeState = null!;
    private readonly McpServerService _mcpServerService = null!;
    private readonly McpServerState _mcpServerState = null!;
    private readonly DisplaySettings _displaySettings = null!;
    private readonly TabRefreshState _tabRefreshState = null!;
    private readonly ILocalStorageService _localStorageService = null!;
    private readonly AccountsTotalState _accountsTotalState = null!;
    private readonly FilterState _filterState = null!;

    public MainView? Window { get; set; }

    [ObservableProperty] private bool _hasDatabaseOpen;
    
    public bool ShowUsdFiatLabels => _currencySettings.MainFiatCurrency != FiatCurrency.Usd.Code;

    public GridLength LayoutCustomTitleBarRowHeight => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new GridLength(30)
        : new GridLength(0);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTransactionButtonSelected))]
    [NotifyPropertyChangedFor(nameof(IsReportButtonSelected))]
    [NotifyPropertyChangedFor(nameof(IsAvgPriceButtonSelected))]
    [NotifyPropertyChangedFor(nameof(IsAssetsButtonSelected))]
    private ValtTabViewModel? _selectedTabComponent;

    public bool IsTransactionButtonSelected =>
        SelectedTabComponent?.TabName == MainViewTabNames.TransactionsPageContent;

    public bool IsReportButtonSelected =>
        SelectedTabComponent?.TabName == MainViewTabNames.ReportsPageContent;

    public bool IsAvgPriceButtonSelected =>
        SelectedTabComponent?.TabName == MainViewTabNames.AvgPricePageContent;

    public bool IsAssetsButtonSelected =>
        SelectedTabComponent?.TabName == MainViewTabNames.AssetsPageContent;

    [ObservableProperty] private string _statusDisplay = string.Empty;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _loadingMessage = string.Empty;

    [ObservableProperty] private bool _showPepe;
    [ObservableProperty] private bool _crashing;
    [ObservableProperty] private bool _pumping;
    [ObservableProperty] private bool _up;
    [ObservableProperty] private bool _down;
    [ObservableProperty]
    private Bitmap? _pepeMoodImage;

    [ObservableProperty] private bool _isOffline;

    public LiveRatesViewModel LiveRatesViewModel => _liveRatesViewModel;
    public UpdateIndicatorViewModel UpdateIndicator => _updateIndicatorViewModel;
    public AvaloniaList<JobInfo> Jobs { get; set; } = new();

    public string SecureModeIcon => _secureModeState?.IsEnabled == true ? "\xE897" : "\xE898";

    // MCP Server status properties
    public bool IsMcpFeatureEnabled => _localStorageService?.LoadMcpServerEnabled() == true;
    public bool IsMcpServerRunning => _mcpServerState?.IsRunning == true;
    public bool IsMcpServerError => _mcpServerState?.ErrorMessage != null;
    public bool IsMcpProcessing => _mcpServerState?.IsProcessing == true;

    public string McpServerTooltip
    {
        get
        {
            if (_mcpServerState?.ErrorMessage != null)
                return string.Format(language.McpServer_ErrorTooltip, _mcpServerState.ErrorMessage);
            if (_mcpServerState?.IsRunning == true)
                return string.Format(language.McpServer_RunningTooltip, _mcpServerState.ServerUrl);
            return language.McpServer_StoppedTooltip;
        }
    }

    public TabRefreshState TabRefreshState => _tabRefreshState;

    #region Event subscribers

    private void LocalDatabaseOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HasDatabaseOpen = _dbLifecycle is not null; // Simplified: service manages DB state
    }

    private void McpServerStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsMcpServerRunning));
        OnPropertyChanged(nameof(IsMcpServerError));
        OnPropertyChanged(nameof(IsMcpProcessing));
        OnPropertyChanged(nameof(McpServerTooltip));
    }

    #endregion

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public MainViewModel()
    {
        _pageFactory = new DesignTimePageFactory();
        _modalLauncher = new DesignTimeModalLauncher();
        _dbLifecycle = null!;
        _currencySettings = new CurrencySettings(null!, null!);
        _updateIndicatorViewModel = new UpdateIndicatorViewModel();

        HasDatabaseOpen = true;
        _currencySettings.MainFiatCurrency = FiatCurrency.Brl.Code;
        SelectedTabComponent = _pageFactory.Create(MainViewTabNames.TransactionsPageContent);
        IsOffline = true;
    }

    public MainViewModel(IPageFactory pageFactory,
        IModalLauncher modalLauncher,
        IDatabaseLifecycleService dbLifecycle,
        CurrencySettings currencySettings,
        BackgroundJobManager backgroundJobManager,
        LiveRatesViewModel liveRatesViewModel,
        UpdateIndicatorViewModel updateIndicatorViewModel,
        LiveRateState liveRateState,
        ICsvExportService csvExportService,
        IClock clock,
        ILogger<MainViewModel> logger,
        SecureModeState secureModeState,
        McpServerService mcpServerService,
        McpServerState mcpServerState,
        DisplaySettings displaySettings,
        TabRefreshState tabRefreshState,
        ILocalStorageService localStorageService,
        AccountsTotalState accountsTotalState,
        FilterState filterState)
    {
        _pageFactory = pageFactory;
        _modalLauncher = modalLauncher;
        _dbLifecycle = dbLifecycle;
        _currencySettings = currencySettings;
        _backgroundJobManager = backgroundJobManager;
        _liveRatesViewModel = liveRatesViewModel;
        _updateIndicatorViewModel = updateIndicatorViewModel;
        _csvExportService = csvExportService;
        _clock = clock;
        _logger = logger;
        _secureModeState = secureModeState;
        _mcpServerService = mcpServerService;
        _mcpServerState = mcpServerState;
        _displaySettings = displaySettings;
        _tabRefreshState = tabRefreshState;
        _localStorageService = localStorageService;
        _accountsTotalState = accountsTotalState;
        _filterState = filterState;

        _mcpServerState.PropertyChanged += McpServerStateOnPropertyChanged;
        
        Jobs = new AvaloniaList<JobInfo>(_backgroundJobManager.GetJobInfos());
        foreach (var job in Jobs)
            job.PropertyChanged += JobOnPropertyChanged;
        
        WeakReferenceMessenger.Default.Register<LivePriceUpdated>(this, (recipient, message) =>
        {
            var percentage = liveRateState.PreviousBitcoinPrice.HasValue
                ? liveRateState.BitcoinPrice / liveRateState.PreviousBitcoinPrice.Value : 0m;

            Crashing = percentage <= 0.95m;
            Down = percentage <= 0.97m && !Crashing;
            Pumping = percentage >= 1.05m;
            Up = percentage >= 1.03m && !Pumping;

            UpdatePepeImage();

            IsOffline = liveRateState.IsOffline;
        });

        WeakReferenceMessenger.Default.Register<McpDataChangedNotification>(this, (recipient, message) =>
        {
            _tabRefreshState.SetAllNeedRefresh();
        });

        WeakReferenceMessenger.Default.Register<McpFeatureEnabledChanged>(this, async (recipient, message) =>
        {
            OnPropertyChanged(nameof(IsMcpFeatureEnabled));

            // Stop MCP server if feature is disabled and server is running
            if (!message.Enabled && _mcpServerState.IsRunning)
            {
                await _mcpServerService.StopAsync();
            }
        });
    }

    [RelayCommand]
    private async Task SetTab(MainViewTabNames tab)
    {
        var needsRefresh = _tabRefreshState.NeedsRefresh(tab);
        SelectedTabComponent = _pageFactory.Create(tab);

        if (needsRefresh && SelectedTabComponent is not null)
        {
            await SelectedTabComponent.RefreshAsync();
            _tabRefreshState.ClearRefresh(tab);
        }
    }

    [RelayCommand]
    private async Task OpenPriceHistory()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.PriceHistory, Window!);
    }

    [RelayCommand]
    private async Task ToggleSecureMode()
    {
        // If leaving secure mode, require password verification
        if (_secureModeState.IsEnabled)
        {
            var result = await _modalLauncher.ShowAsync<InputPasswordViewModel, InputPasswordViewModel.Response?>(
                ApplicationModalNames.InputPassword,
                Window!,
                vm => vm.HideSecureModeCheckbox = true);

            if (result?.Password is null)
                return;

            // Verify password before allowing to leave secure mode
            if (!_secureModeState.VerifyPassword(result.Password))
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_InvalidPassword, Window!);
                return;
            }
        }

        _secureModeState.IsEnabled = !_secureModeState.IsEnabled;
        OnPropertyChanged(nameof(SecureModeIcon));
    }

    [RelayCommand]
    private async Task ToggleMcpServer()
    {
        await _mcpServerService.ToggleAsync(_displaySettings.McpServerPort);
    }

    [RelayCommand]
    private async Task ManageCategories()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.ManageCategories, Window!);
    }

    [RelayCommand]
    private async Task ManageSettings()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.Settings, Window!);
    }

    [RelayCommand]
    private async Task OpenImportWizard()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.ImportWizard, Window!);
    }

    [RelayCommand]
    private async Task ExportTransactions()
    {
        if (Window is null) return;

        var options = new FilePickerSaveOptions
        {
            Title = "Export Transactions",
            SuggestedFileName = "valt-transactions.csv",
            FileTypeChoices =
            [
                new FilePickerFileType("CSV Files") { Patterns = ["*.csv"] }
            ]
        };

        var result = await Window.StorageProvider.SaveFilePickerAsync(options);
        if (result is null) return;

        var csv = await _csvExportService.ExportTransactionsAsync();
        await File.WriteAllTextAsync(result.Path.LocalPath, csv);
    }

    [RelayCommand]
    private async Task CloseDatabase()
    {
        // 1. Clear the selected tab first to stop ViewModels from fetching data
        SelectedTabComponent = null;

        // 2. Give UI thread time to process the tab removal
        await Task.Delay(100);

        // 3. Stop MCP server (depends on database)
        if (_mcpServerService is not null && _mcpServerState.IsRunning)
        {
            await _mcpServerService.StopAsync();
        }

        // 4. Stop all background jobs and close databases
        await _dbLifecycle.CloseDatabasesAsync();

        // 5. Reset all UI state
        _accountsTotalState.Reset();
        _filterState.Reset();
        _secureModeState.Reset();
        _tabRefreshState.ClearAll();

        // 6. Update UI
        OnPropertyChanged(nameof(SecureModeIcon));

        // 7. Navigate back to initial selection modal
        await OpenInitialSelectionModal();
    }

    [RelayCommand]
    private async Task About()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.About, Window!);
    }

    [RelayCommand]
    private async Task OpenConversionCalculator()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.ConversionCalculator, Window!);
    }

    [RelayCommand]
    private async Task OpenLeverageSimulator()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.LeverageSimulator, Window!);
    }

    [RelayCommand]
    private async Task OpenSpendingEvolution()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.SpendingEvolution, Window!);
    }

    [RelayCommand]
    private void HowToUse()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://btcdoomguy.github.io/valt-docs/",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening User Guide URL");
        }
    }

    [RelayCommand]
    private Task SendF2()
    {
        if (SelectedTabComponent?.TabName == MainViewTabNames.TransactionsPageContent)
            WeakReferenceMessenger.Default.Send(new AddTransactionRequested());
        else if (SelectedTabComponent?.TabName == MainViewTabNames.AvgPricePageContent)
            WeakReferenceMessenger.Default.Send(new AddAvgPriceLineRequested());
        else if (SelectedTabComponent?.TabName == MainViewTabNames.AssetsPageContent)
            WeakReferenceMessenger.Default.Send(new AddAssetRequested());

        return Task.CompletedTask;
    }

    public async Task OpenTipsModalIfNeededAsync()
    {
        if (!_localStorageService.LoadShowTipsOnStartup())
            return;

        await ShowTipsAsync();
    }

    [RelayCommand]
    private async Task ShowTips()
    {
        await ShowTipsAsync();
    }

    private async Task ShowTipsAsync()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.Tips, Window!);
    }

    public async Task OpenInitialSelectionModal()
    {
        var appLifetime =
            Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        var openedFile = false;
        while (!openedFile)
        {
            var result = await _modalLauncher.ShowAsync<InitialSelectionViewModel, InitialSelectionViewModel.Response?>(
                ApplicationModalNames.InitialSelection,
                Window!);
            Window?.Activate();

            if (result is null || string.IsNullOrEmpty(result.File))
            {
                appLifetime!.Shutdown();
                return;
            }

            var openResult = await _dbLifecycle.OpenLocalDatabaseAsync(result.File, result.Password);
            if (!openResult.Success)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error,
                    string.Format(language.ValtFile_Error, openResult.ErrorMessage),
                    Window!);
                continue;
            }

            // Check database version compatibility before proceeding
            if (!result.IsNew)
            {
                var compatibility = _dbLifecycle.CheckCompatibility();
                if (!compatibility.IsCompatible)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error,
                        string.Format(language.Error_IncompatibleVersion,
                            compatibility.RequiredVersion,
                            compatibility.CurrentVersion),
                        Window!);
                    _dbLifecycle.CloseLocalDatabase();
                    continue;
                }
            }

            if (result.IsNew)
            {
                await _dbLifecycle.InitializeNewDatabaseAsync(result.InitialDataLanguage, result.SelectedCurrencies.ToArray());
            }

            await _dbLifecycle.MigrateAsync();
            
            LoadingMessage = language.Loading_PriceDatabase;
            IsLoading = true;

            using var priceDbCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var priceDbResult = await Task.Run(() =>
                _dbLifecycle.InitializePriceDatabaseAsync(priceDbCts.Token));

            if (!priceDbResult.Success)
            {
                _logger.LogWarning("Price database initialization failed, continuing with stale/cached data");
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LoadingMessage = language.Loading_LoadingTransactions;
                SelectedTabComponent = _pageFactory.Create(MainViewTabNames.TransactionsPageContent);
            });

            if (SelectedTabComponent is not null)
            {
                await SelectedTabComponent.RefreshAsync();
            }

            openedFile = true;

            // Initialize filter state with current date for the new database
            _filterState.MainDate = DateTime.Now;

            await _dbLifecycle.StartValtDatabaseJobsAsync();

            IsLoading = false;
            LoadingMessage = string.Empty;

            // Store the password hash for secure mode verification
            _secureModeState.SetPassword(result.Password);

            // Initialize secure mode state based on user preference from login
            _secureModeState.IsEnabled = result.StartInSecureMode;
            OnPropertyChanged(nameof(SecureModeIcon));

            // Schedule an early asset price refresh to update stale prices from the previous session.
            // The regular interval is 5 minutes, so this ensures totals are up-to-date shortly after startup.
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                _dbLifecycle.TriggerJobManually(BackgroundJobSystemNames.AssetPriceUpdater);
            });

            // Check for updates (fire and forget - don't block startup)
            _ = CheckForUpdatesAsync();
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            _updateIndicatorViewModel.SetOwnerWindow(Window!);
            await _updateIndicatorViewModel.CheckForUpdateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for updates");
        }
    }

    [RelayCommand]
    private async Task OpenStatusDisplay()
    {
        await _modalLauncher.ShowAsync(ApplicationModalNames.StatusDisplay, Window!);
    }
    
    private void JobOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Single-pass to determine job states instead of multiple All()/Any() calls
        var hasOk = false;
        var hasRunning = false;
        var allOk = true;
        var allError = true;

        foreach (var job in Jobs)
        {
            switch (job.State)
            {
                case BackgroundJobState.Ok:
                    hasOk = true;
                    allError = false;
                    break;
                case BackgroundJobState.Error:
                    allOk = false;
                    break;
                case BackgroundJobState.Running:
                    hasRunning = true;
                    allOk = false;
                    allError = false;
                    break;
                default:
                    allOk = false;
                    allError = false;
                    break;
            }
        }

        if (allOk && Jobs.Count > 0)
        {
            StatusDisplay = "Status: OK";
            return;
        }

        if (allError && Jobs.Count > 0)
        {
            StatusDisplay = "Status: Error";
            return;
        }

        if (hasRunning)
        {
            StatusDisplay = "Status: Running";
            return;
        }

        if (hasOk)
        {
            StatusDisplay = "Status: Unstable";
        }
    }

    public async Task OnClosingAsync()
    {
        if (_mcpServerService is not null)
        {
            await _mcpServerService.StopAsync();
        }

        await _dbLifecycle.CloseDatabasesAsync();
    }
    
    private void UpdatePepeImage()
    {
        var assetName = (Pumping, Crashing, Up, Down) switch
        {
            (true,  _,     _,     _) => "veryhappy.webp",
            (_,     true,  _,     _) => "veryunhappy.webp",
            (_,     _,     true,  _) => "happy.webp",
            (_,     _,     _,     true) => "unhappy.webp",
            _ => null
        };

        if (assetName is null)
        {
            ShowPepe = false;
            PepeMoodImage = null;
            return;
        }

        ShowPepe = true;
        var uri = new Uri($"avares://Valt/Assets/Pepe/{assetName}");
        PepeMoodImage = new Bitmap(AssetLoader.Open(uri));
    }

    public void Dispose()
    {
        // Unregister from WeakReferenceMessenger
        WeakReferenceMessenger.Default.UnregisterAll(this);

        // Unsubscribe from MCP server state PropertyChanged
        if (_mcpServerState is not null)
            _mcpServerState.PropertyChanged -= McpServerStateOnPropertyChanged;

        // Unsubscribe from job PropertyChanged events
        foreach (var job in Jobs)
            job.PropertyChanged -= JobOnPropertyChanged;

        // Dispose the Pepe image
        PepeMoodImage?.Dispose();
        PepeMoodImage = null;
    }
}
