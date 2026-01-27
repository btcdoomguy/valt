using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Budget;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Reports.AllTimeHigh;
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
using Valt.UI.Views.Main.Modals.About;
using Valt.UI.Views.Main.Modals.InitialSelection;
using Valt.UI.Views.Main.Modals.ManageCategories;
using Valt.UI.Views.Main.Modals.Settings;
using Valt.UI.Views.Main.Modals.StatusDisplay;
using Valt.UI.Views.Main.Modals.ImportWizard;
using Valt.UI.Views.Main.Modals.InputPassword;
using Valt.UI.Views.Main.Modals.ConversionCalculator;
using Valt.Infra.Mcp.Notifications;
using Valt.Infra.Mcp.Server;

namespace Valt.UI.Views.Main;

public partial class MainViewModel : ValtViewModel, IDisposable
{
    private readonly IPageFactory _pageFactory;
    private readonly IModalFactory _modalFactory;

    private readonly ILocalDatabase? _localDatabase;
    private readonly IPriceDatabase _priceDatabase = null!;
    private readonly CurrencySettings _currencySettings;
    private readonly BackgroundJobManager? _backgroundJobManager;
    private readonly IDatabaseInitializer? _databaseInitializer;
    private readonly IDatabaseVersionChecker? _databaseVersionChecker;
    private readonly LiveRatesViewModel _liveRatesViewModel = null!;
    private readonly UpdateIndicatorViewModel _updateIndicatorViewModel;
    private readonly IAllTimeHighReport _allTimeHighReport = null!;
    private readonly ICsvExportService _csvExportService = null!;
    private readonly IClock _clock = null!;
    private readonly ILogger<MainViewModel> _logger = null!;
    private readonly SecureModeState _secureModeState = null!;
    private readonly McpServerService _mcpServerService = null!;
    private readonly McpServerState _mcpServerState = null!;
    private readonly DisplaySettings _displaySettings = null!;
    private readonly TabRefreshState _tabRefreshState = null!;
    private readonly ILocalStorageService _localStorageService = null!;

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
    private ValtTabViewModel? _selectedTabComponent;

    public bool IsTransactionButtonSelected =>
        SelectedTabComponent?.TabName == MainViewTabNames.TransactionsPageContent;

    public bool IsReportButtonSelected =>
        SelectedTabComponent?.TabName == MainViewTabNames.ReportsPageContent;
    
    public bool IsAvgPriceButtonSelected =>
        SelectedTabComponent?.TabName == MainViewTabNames.AvgPricePageContent;

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
        HasDatabaseOpen = _localDatabase!.HasDatabaseOpen;
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
        _modalFactory = new DesignTimeModalFactory();
        _currencySettings = new CurrencySettings(_localDatabase!, null!);
        _updateIndicatorViewModel = new UpdateIndicatorViewModel();

        HasDatabaseOpen = true;
        _currencySettings.MainFiatCurrency = FiatCurrency.Brl.Code;
        SelectedTabComponent = _pageFactory.Create(MainViewTabNames.TransactionsPageContent);
        IsOffline = true;
    }

    public MainViewModel(IPageFactory pageFactory,
        IModalFactory modalFactory,
        ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        CurrencySettings currencySettings,
        BackgroundJobManager backgroundJobManager,
        IDatabaseInitializer databaseInitializer,
        IDatabaseVersionChecker databaseVersionChecker,
        LiveRatesViewModel liveRatesViewModel,
        UpdateIndicatorViewModel updateIndicatorViewModel,
        LiveRateState liveRateState,
        IAllTimeHighReport allTimeHighReport,
        ICsvExportService csvExportService,
        IClock clock,
        ILogger<MainViewModel> logger,
        SecureModeState secureModeState,
        McpServerService mcpServerService,
        McpServerState mcpServerState,
        DisplaySettings displaySettings,
        TabRefreshState tabRefreshState,
        ILocalStorageService localStorageService)
    {
        _pageFactory = pageFactory;
        _modalFactory = modalFactory;
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _currencySettings = currencySettings;
        _backgroundJobManager = backgroundJobManager;
        _databaseInitializer = databaseInitializer;
        _databaseVersionChecker = databaseVersionChecker;
        _liveRatesViewModel = liveRatesViewModel;
        _updateIndicatorViewModel = updateIndicatorViewModel;
        _allTimeHighReport = allTimeHighReport;
        _csvExportService = csvExportService;
        _clock = clock;
        _logger = logger;
        _secureModeState = secureModeState;
        _mcpServerService = mcpServerService;
        _mcpServerState = mcpServerState;
        _displaySettings = displaySettings;
        _tabRefreshState = tabRefreshState;
        _localStorageService = localStorageService;

        _localDatabase.PropertyChanged += LocalDatabaseOnPropertyChanged;
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
    private async Task SetTransactionsTab()
    {
        var needsRefresh = _tabRefreshState.NeedsRefresh(MainViewTabNames.TransactionsPageContent);
        SelectedTabComponent = _pageFactory.Create(MainViewTabNames.TransactionsPageContent);

        if (needsRefresh && SelectedTabComponent is not null)
        {
            await SelectedTabComponent.RefreshAsync();
            _tabRefreshState.ClearRefresh(MainViewTabNames.TransactionsPageContent);
        }
    }

    [RelayCommand]
    private async Task SetReportsTab()
    {
        var needsRefresh = _tabRefreshState.NeedsRefresh(MainViewTabNames.ReportsPageContent);
        SelectedTabComponent = _pageFactory.Create(MainViewTabNames.ReportsPageContent);

        if (needsRefresh && SelectedTabComponent is not null)
        {
            await SelectedTabComponent.RefreshAsync();
            _tabRefreshState.ClearRefresh(MainViewTabNames.ReportsPageContent);
        }
    }

    [RelayCommand]
    private async Task SetAvgPriceTab()
    {
        var needsRefresh = _tabRefreshState.NeedsRefresh(MainViewTabNames.AvgPricePageContent);
        SelectedTabComponent = _pageFactory.Create(MainViewTabNames.AvgPricePageContent);

        if (needsRefresh && SelectedTabComponent is not null)
        {
            await SelectedTabComponent.RefreshAsync();
            _tabRefreshState.ClearRefresh(MainViewTabNames.AvgPricePageContent);
        }
    }

    [RelayCommand]
    private async Task ToggleSecureMode()
    {
        // If leaving secure mode, require password verification
        if (_secureModeState.IsEnabled)
        {
            var inputPasswordModal =
                (InputPasswordView)await _modalFactory.CreateAsync(ApplicationModalNames.InputPassword, Window)!;

            // Hide the "Start in Secure Mode" checkbox when verifying password to leave
            var viewModel = (InputPasswordViewModel)inputPasswordModal.DataContext!;
            viewModel.HideSecureModeCheckbox = true;

            var result = await inputPasswordModal.ShowDialog<InputPasswordViewModel.Response?>(Window!);

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
        var modal =
            (ManageCategoriesView)await _modalFactory.CreateAsync(ApplicationModalNames.ManageCategories, Window)!;

        await modal.ShowDialog(Window!);
    }

    [RelayCommand]
    private async Task ManageSettings()
    {
        var modal =
            (SettingsView)await _modalFactory.CreateAsync(ApplicationModalNames.Settings, Window)!;

        await modal.ShowDialog(Window!);
    }

    [RelayCommand]
    private async Task OpenImportWizard()
    {
        var modal =
            (ImportWizardView)await _modalFactory.CreateAsync(ApplicationModalNames.ImportWizard, Window)!;

        await modal.ShowDialog(Window!);
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
    private async Task About()
    {
        var modal =
            (AboutView)await _modalFactory.CreateAsync(ApplicationModalNames.About, Window)!;

        await modal.ShowDialog(Window!);
    }

    [RelayCommand]
    private async Task OpenConversionCalculator()
    {
        var modal =
            (ConversionCalculatorView)await _modalFactory.CreateAsync(ApplicationModalNames.ConversionCalculator, Window)!;

        await modal.ShowDialog(Window!);
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
        
        return Task.CompletedTask;
    }

    public async Task OpenInitialSelectionModal()
    {
        var appLifetime =
            Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        var openedFile = false;
        while (!openedFile)
        {
            var window =
                (InitialSelectionView)await _modalFactory.CreateAsync(ApplicationModalNames.InitialSelection, Window)!;

            var result = await window.ShowDialog<InitialSelectionViewModel.Response?>(Window!);

            if (result is null || string.IsNullOrEmpty(result.File))
            {
                appLifetime!.Shutdown();
            }

            try
            {
                _localDatabase!.OpenDatabase(result!.File, result.Password);
            }
            catch (LiteException ex)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error,
                    string.Format(language.ValtFile_Error, ex.Message),
                    Window!);
                continue;
            }
            catch (Exception ex)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, $"{ex.Message}", Window!);
                continue;
            }

            // Check database version compatibility before proceeding
            if (!result.IsNew)
            {
                var compatibilityResult = _databaseVersionChecker!.CheckCompatibility();
                if (!compatibilityResult.IsCompatible)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error,
                        string.Format(language.Error_IncompatibleVersion,
                            compatibilityResult.RequiredVersion,
                            compatibilityResult.CurrentVersion),
                        Window!);
                    _localDatabase.CloseDatabase();
                    continue;
                }
            }

            if (result.IsNew)
            {
                await _databaseInitializer!.InitializeAsync(result.InitialDataLanguage, result.SelectedCurrencies);
            }

            await _databaseInitializer!.MigrateAsync();
            
            // Initialize price database AFTER local database is opened
            // This ensures we have access to AvailableFiatCurrencies configuration
            if (!await InitializePriceDatabaseAsync())
            {
                appLifetime!.Shutdown();
            }

            openedFile = true;
            SetTransactionsTab();

            // Store the password hash for secure mode verification
            _secureModeState.SetPassword(result.Password);

            // Initialize secure mode state based on user preference from login
            _secureModeState.IsEnabled = result.StartInSecureMode;
            OnPropertyChanged(nameof(SecureModeIcon));

            //this avoids some race conditions with the jobs and current UI state
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await _backgroundJobManager!.StartAllJobsAsync(jobType: BackgroundJobTypes.ValtDatabase);
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

    private async Task<bool> InitializePriceDatabaseAsync()
    {
        try
        {
            if (!_priceDatabase.DatabaseFileExists() || IsPriceDatabaseEmpty())
            {
                var installResult = await InstallProcessAsync();

                if (!installResult)
                    return false;
            }

            if (!_priceDatabase.HasDatabaseOpen)
                _priceDatabase!.OpenDatabase();

            await _backgroundJobManager!.StartAllJobsAsync(jobType: BackgroundJobTypes.PriceDatabase, triggerInitialRun: false);

            // Run LivePricesUpdater synchronously to ensure rates are available before UI is shown
            await _backgroundJobManager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.LivePricesUpdater);

            // Trigger history updater jobs to run in the background
            _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.BitcoinHistoryUpdater);
            _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.FiatHistoryUpdater);

            return true;
        }
        catch (LiteException ex)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error,
                string.Format(language.ValtPriceFile_Error, ex.Message),
                Window!);
        }
        catch (Exception ex)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, $"{ex.Message}", Window!);
        }
        finally
        {
            IsLoading = false;
        }
        
        return false;
    }

    private bool IsPriceDatabaseEmpty()
    {
        try
        {
            _priceDatabase!.OpenDatabase();
            return !_priceDatabase.HasPriceData();
        }
        finally
        {
            _priceDatabase!.CloseDatabase();
        }
    }

    private async Task<bool> InstallProcessAsync()
    {
        await MessageBoxHelper.ShowAlertAsync(language.InstallPriceDatabase_Title,
            language.InstallPriceDatabase_Info,
            Window!);
        
        _priceDatabase!.OpenDatabase();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsLoading = true;
        });

        //execute the main jobs manually
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LoadingMessage = language.InstallingBitcoinPriceMessage;
            });
            await _backgroundJobManager!.TriggerJobAndWaitAsync(BackgroundJobSystemNames
                .BitcoinHistoryUpdater);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LoadingMessage = language.InstallingFiatPriceMessage;
            });
            await _backgroundJobManager.TriggerJobAndWaitAsync(BackgroundJobSystemNames
                .FiatHistoryUpdater);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[InstallProcessAsync] Error during execution");

            await MessageBoxHelper.ShowErrorAsync(language.InstallPriceDatabase_Error_Title,
                language.InstallPriceDatabase_Error_Info,
                Window!);
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task OpenStatusDisplay()
    {
        var modal =
            (StatusDisplayView)await _modalFactory.CreateAsync(ApplicationModalNames.StatusDisplay, Window)!;

        await modal.ShowDialog(Window!);
    }
    
    private void JobOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Jobs.All(x => x.State == BackgroundJobState.Ok))
        {
            StatusDisplay = "Status: OK";
            return;
        }

        if (Jobs.All(x => x.State == BackgroundJobState.Error))
        {
            StatusDisplay = "Status: Error";
            return;
        }

        if (Jobs.Any(x => x.State == BackgroundJobState.Running))
        {
            StatusDisplay = "Status: Running";
            return;
        }

        if (Jobs.Any(x => x.State == BackgroundJobState.Ok))
        {
            StatusDisplay = "Status: Unstable";
        }
    }

    public async Task OnClosingAsync()
    {
        if (_backgroundJobManager is null)
            return;

        // Stop MCP server first
        if (_mcpServerService is not null)
        {
            await _mcpServerService.StopAsync();
        }

        await _backgroundJobManager.StopAll();

        // Close databases to trigger checkpoint (merges -log.db into main .db file)
        _localDatabase?.CloseDatabase();
        _priceDatabase?.CloseDatabase();
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

        // Unsubscribe from local database PropertyChanged
        if (_localDatabase is not null)
            _localDatabase.PropertyChanged -= LocalDatabaseOnPropertyChanged;

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