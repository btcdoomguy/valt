using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Budget;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State;
using Valt.UI.State.Events;
using Valt.UI.Views.Main.Controls;
using Valt.UI.Views.Main.Modals.About;
using Valt.UI.Views.Main.Modals.InitialSelection;
using Valt.UI.Views.Main.Modals.ManageCategories;
using Valt.UI.Views.Main.Modals.Settings;
using Valt.UI.Views.Main.Modals.StatusDisplay;

namespace Valt.UI.Views.Main;

public partial class MainViewModel : ValtViewModel
{
    private readonly IPageFactory _pageFactory;
    private readonly IModalFactory _modalFactory;

    private readonly ILocalDatabase? _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly CurrencySettings _currencySettings;
    private readonly BackgroundJobManager? _backgroundJobManager;
    private readonly IDatabaseInitializer? _databaseInitializer;
    private readonly LiveRatesViewModel _liveRatesViewModel;
    private readonly ILogger<MainViewModel> _logger;

    public MainView? Window { get; set; }

    [ObservableProperty] private bool _hasDatabaseOpen;
    
    public bool ShowUsdFiatLabels => _currencySettings.MainFiatCurrency != FiatCurrency.Usd.Code;

    public GridLength LayoutCustomTitleBarRowHeight => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new GridLength(30)
        : new GridLength(0);

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsTransactionButtonSelected))]
    private ValtViewModel? _selectedTabComponent;

    public bool IsTransactionButtonSelected =>
        SelectedTabComponent?.PageName == MainViewTabNames.TransactionsPageContent;

    public bool IsReportButtonSelected =>
        SelectedTabComponent?.PageName == MainViewTabNames.ReportsPageContent;

    [ObservableProperty] private string _statusDisplay;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _loadingMessage;

    [ObservableProperty] private bool _showPepe;
    [ObservableProperty] private bool _crashing;
    [ObservableProperty] private bool _pumping;
    [ObservableProperty] private bool _up;
    [ObservableProperty] private bool _down;
    [ObservableProperty]
    private Bitmap? _pepeMoodImage;

    [ObservableProperty] private bool _isOffline;

    public LiveRatesViewModel LiveRatesViewModel => _liveRatesViewModel;
    public AvaloniaList<JobInfo> Jobs { get; set; }

    #region Event subscribers

    private void LocalDatabaseOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HasDatabaseOpen = _localDatabase!.HasDatabaseOpen;
    }

    #endregion

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public MainViewModel()
    {
        _pageFactory = new DesignTimePageFactory();
        _modalFactory = new DesignTimeModalFactory();
        _currencySettings = new CurrencySettings(_localDatabase);

        HasDatabaseOpen = true;
        _currencySettings.MainFiatCurrency = FiatCurrency.Brl.Code;
    }

    public MainViewModel(IPageFactory pageFactory,
        IModalFactory modalFactory,
        ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        CurrencySettings currencySettings,
        BackgroundJobManager backgroundJobManager,
        IDatabaseInitializer databaseInitializer,
        LiveRatesViewModel liveRatesViewModel,
        LiveRateState liveRateState,
        ILogger<MainViewModel> logger)
    {
        _pageFactory = pageFactory;
        _modalFactory = modalFactory;
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _currencySettings = currencySettings;
        _backgroundJobManager = backgroundJobManager;
        _databaseInitializer = databaseInitializer;
        _liveRatesViewModel = liveRatesViewModel;
        _logger = logger;

        _localDatabase.PropertyChanged += LocalDatabaseOnPropertyChanged;

        Jobs = new AvaloniaList<JobInfo>(_backgroundJobManager.GetJobInfos());
        foreach (var job in Jobs)
            job.PropertyChanged += JobOnPropertyChanged;
        
        WeakReferenceMessenger.Default.Register<LivePriceUpdated>(this, (recipient, message) =>
        {
            var percentage = liveRateState.BitcoinPrice / liveRateState.PreviousBitcoinPrice.GetValueOrDefault();
            
            Crashing = percentage <= 0.95m;
            Down = percentage <= 0.97m && !Crashing;
            Pumping = percentage >= 1.05m;
            Up = percentage >= 1.03m && !Pumping;
            
            UpdatePepeImage();

            IsOffline = liveRateState.IsOffline;
        });
    }

    [RelayCommand]
    private void SetTransactionsTab()
    {
        if (SelectedTabComponent != null)
        {
            SelectedTabComponent = null;
        }

        SelectedTabComponent = _pageFactory.Create(MainViewTabNames.TransactionsPageContent);
    }

    [RelayCommand]
    private void SetReportsTab()
    {
        if (SelectedTabComponent != null)
        {
            SelectedTabComponent = null;
        }

        SelectedTabComponent = _pageFactory.Create(MainViewTabNames.ReportsPageContent);
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
    private async Task About()
    {
        var modal =
            (AboutView)await _modalFactory.CreateAsync(ApplicationModalNames.About, Window)!;

        await modal.ShowDialog(Window!);
    }

    [RelayCommand]
    private Task AddTransaction()
    {
        WeakReferenceMessenger.Default.Send(new AddTransactionRequested());
        return Task.CompletedTask;
    }

    public async Task OpenInitialSelectionModal()
    {
        var appLifetime =
            Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        if (!await InitializePriceDatabaseAsync())
        {
            appLifetime!.Shutdown();
        }

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

            if (result.IsNew)
            {
                await _databaseInitializer!.InitializeAsync();
            }

            await _databaseInitializer!.MigrateAsync();

            openedFile = true;
            SetTransactionsTab();

            //this avoids some race conditions with the jobs and current UI state
            Dispatcher.UIThread.Invoke(() =>
            {
                _backgroundJobManager!.StartAllJobs(jobType: BackgroundJobTypes.ValtDatabase);
            });
        }
    }

    private async Task<bool> InitializePriceDatabaseAsync()
    {
        try
        {
            if (!_priceDatabase.DatabaseFileExists())
            {
                var installResult = await InstallProcessAsync();

                if (!installResult)
                    return false;
            }

            if (!_priceDatabase.HasDatabaseOpen)
                _priceDatabase!.OpenDatabase();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsLoading = true;
                LoadingMessage = language.LoadingMessage;
            });

            _backgroundJobManager!.StartAllJobs(jobType: BackgroundJobTypes.PriceDatabase);

            while (_backgroundJobManager.IsRunningTasksOf(BackgroundJobTypes.PriceDatabase))
                await Task.Delay(100);

            IsLoading = false;

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
        
        return false;
    }

    private async Task<bool> InstallProcessAsync()
    {
        if (!await MessageBoxHelper.ShowOkCancelAsync(language.InstallPriceDatabase_Title,
                language.InstallPriceDatabase_Info,
                Window!))
        {
            return false;
        }
        
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
            await _backgroundJobManager!.TriggerJobManuallyOnCurrentThreadAsync(BackgroundJobSystemNames
                .BitcoinHistoryUpdater);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LoadingMessage = language.InstallingFiatPriceMessage;
            });
            await _backgroundJobManager.TriggerJobManuallyOnCurrentThreadAsync(BackgroundJobSystemNames
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

        await _backgroundJobManager.StopAll();
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
}