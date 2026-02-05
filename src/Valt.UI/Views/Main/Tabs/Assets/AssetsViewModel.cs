using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.DeleteAsset;
using Valt.App.Modules.Assets.Commands.SetAssetIncludeInNetWorth;
using Valt.App.Modules.Assets.Commands.SetAssetVisibility;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetAssets;
using Valt.App.Modules.Assets.Queries.GetAssetSummary;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.State;
using Valt.UI.State.Events;
using Valt.UI.Views;
using Valt.UI.Services.MessageBoxes;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.Views.Main.Modals.ManageAsset;
using Valt.UI.Views.Main.Modals.TransactionEditor;
using Valt.UI.Views.Main.Tabs.Assets.Models;
using static Valt.UI.Base.TaskExtensions;

namespace Valt.UI.Views.Main.Tabs.Assets;

public partial class AssetsViewModel : ValtTabViewModel, IDisposable
{
    private readonly IQueryDispatcher _queryDispatcher = null!;
    private readonly ICommandDispatcher _commandDispatcher = null!;
    private readonly CurrencySettings _currencySettings = null!;
    private readonly RatesState _ratesState = null!;
    private readonly SecureModeState _secureModeState = null!;
    private readonly IModalFactory _modalFactory = null!;
    private readonly BackgroundJobManager _backgroundJobManager = null!;
    private readonly ILogger<AssetsViewModel> _logger = null!;
    private JobInfo? _assetPriceUpdaterJobInfo;

    [ObservableProperty] private AvaloniaList<AssetViewModel> _assets = new();
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isRefreshingPrices;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalValueColor))]
    [NotifyPropertyChangedFor(nameof(TotalSatsColor))]
    [NotifyPropertyChangedFor(nameof(TotalValueFormatted))]
    private AssetSummaryDTO? _summary;
    [ObservableProperty] private AssetViewModel? _selectedAsset;

    public bool IsSecureModeEnabled => _secureModeState?.IsEnabled ?? false;

    public string MainCurrencyCode => _currencySettings?.MainFiatCurrency ?? "USD";

    public string TotalValueColor => Summary?.TotalValueInMainCurrency >= 0 ? "#FFFFFF" : "#F44336";
    public string TotalSatsColor => Summary?.TotalValueInSats >= 0 ? "#FFFFFF" : "#F44336";

    public string TotalValueFormatted => Summary != null
        ? CurrencyDisplay.FormatFiat(Summary.TotalValueInMainCurrency, MainCurrencyCode)
        : "-";

    /// <summary>
    /// Design-time sample data for XAML previewer.
    /// </summary>
    public static AssetsViewModel DesignTimeSample
    {
        get
        {
            var vm = new AssetsViewModel
            {
                IsLoading = false,
                Summary = new AssetSummaryDTO
                {
                    TotalAssets = 5,
                    VisibleAssets = 5,
                    AssetsIncludedInNetWorth = 4,
                    ValuesByCurrency = new List<AssetValueByCurrencyDTO>
                    {
                        new() { CurrencyCode = "USD", TotalValue = 125432.50m, AssetCount = 3 },
                        new() { CurrencyCode = "BRL", TotalValue = 50000m, AssetCount = 2 }
                    },
                    TotalValueInMainCurrency = 135432.50m,
                    TotalValueInSats = 1_350_000_000
                },
                Assets = new AvaloniaList<AssetViewModel>
                {
                    new(new AssetDTO
                    {
                        Id = "1", Name = "Apple Inc.", AssetTypeId = 0, AssetTypeName = "Stock",
                        Icon = "\xE8F5", IncludeInNetWorth = true, Visible = true,
                        LastPriceUpdateAt = DateTime.Now, CreatedAt = DateTime.Now, DisplayOrder = 1,
                        CurrentPrice = 185.50m, CurrentValue = 18550m, CurrencyCode = "USD",
                        Quantity = 100, Symbol = "AAPL"
                    }, "USD"),
                    new(new AssetDTO
                    {
                        Id = "2", Name = "Bitcoin ETF", AssetTypeId = 1, AssetTypeName = "ETF",
                        Icon = "\xE8F5", IncludeInNetWorth = true, Visible = true,
                        LastPriceUpdateAt = DateTime.Now, CreatedAt = DateTime.Now, DisplayOrder = 2,
                        CurrentPrice = 52.30m, CurrentValue = 52300m, CurrencyCode = "USD",
                        Quantity = 1000, Symbol = "IBIT"
                    }, "USD"),
                    new(new AssetDTO
                    {
                        Id = "3", Name = "Beach House", AssetTypeId = 3, AssetTypeName = "Real Estate",
                        Icon = "\xE88A", IncludeInNetWorth = true, Visible = true,
                        LastPriceUpdateAt = DateTime.Now, CreatedAt = DateTime.Now, DisplayOrder = 3,
                        CurrentPrice = 450000m, CurrentValue = 450000m, CurrencyCode = "USD",
                        Address = "123 Ocean Drive, Miami FL", MonthlyRentalIncome = 3500m
                    }, "USD"),
                    new(new AssetDTO
                    {
                        Id = "4", Name = "BTC Long 5x", AssetTypeId = 5, AssetTypeName = "Leveraged Position",
                        Icon = "\xE8F5", IncludeInNetWorth = true, Visible = true,
                        LastPriceUpdateAt = DateTime.Now, CreatedAt = DateTime.Now, DisplayOrder = 4,
                        CurrentPrice = 98500m, CurrentValue = 12500m, CurrencyCode = "USD",
                        Collateral = 10000m, EntryPrice = 95000m, Leverage = 5m,
                        LiquidationPrice = 78000m, IsLong = true, PnL = 2500m,
                        PnLPercentage = 25m, DistanceToLiquidation = 20.8m, IsAtRisk = false
                    }, "USD"),
                    new(new AssetDTO
                    {
                        Id = "5", Name = "Gold Bars", AssetTypeId = 4, AssetTypeName = "Commodity",
                        Icon = "\xE8F5", IncludeInNetWorth = false, Visible = true,
                        LastPriceUpdateAt = DateTime.Now, CreatedAt = DateTime.Now, DisplayOrder = 5,
                        CurrentPrice = 2050m, CurrentValue = 10250m, CurrencyCode = "USD",
                        Quantity = 5, Symbol = "XAU"
                    }, "USD")
                }
            };
            return vm;
        }
    }

    /// <summary>
    /// Private constructor for design-time use only.
    /// </summary>
    private AssetsViewModel() { }

    public AssetsViewModel(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        CurrencySettings currencySettings,
        RatesState ratesState,
        SecureModeState secureModeState,
        IModalFactory modalFactory,
        BackgroundJobManager backgroundJobManager,
        ILogger<AssetsViewModel> logger)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _currencySettings = currencySettings;
        _ratesState = ratesState;
        _secureModeState = secureModeState;
        _modalFactory = modalFactory;
        _backgroundJobManager = backgroundJobManager;
        _logger = logger;

        _secureModeState.PropertyChanged += OnSecureModeStatePropertyChanged;
        _ratesState.PropertyChanged += OnRatesStatePropertyChanged;

        // Subscribe to asset price updater job state changes
        _assetPriceUpdaterJobInfo = _backgroundJobManager.GetJobInfos()
            .FirstOrDefault(j => j.Job.SystemName == BackgroundJobSystemNames.AssetPriceUpdater);
        if (_assetPriceUpdaterJobInfo != null)
        {
            _assetPriceUpdaterJobInfo.PropertyChanged += OnAssetPriceUpdaterJobPropertyChanged;
            IsRefreshingPrices = _assetPriceUpdaterJobInfo.State == BackgroundJobState.Running;
        }

        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            if (message.PropertyName == nameof(CurrencySettings.MainFiatCurrency))
            {
                OnPropertyChanged(nameof(MainCurrencyCode));
                LoadAssetsAsync().SafeFireAndForget(logger: _logger, callerName: nameof(LoadAssetsAsync));
            }
        });
    }

    public void Initialize()
    {
        LoadAssetsAsync().SafeFireAndForget(logger: _logger, callerName: nameof(LoadAssetsAsync));
    }

    public override MainViewTabNames TabName => MainViewTabNames.AssetsPageContent;

    public override async Task RefreshAsync()
    {
        await LoadAssetsAsync();
    }

    private async Task LoadAssetsAsync()
    {
        try
        {
            IsLoading = true;

            var assets = await _queryDispatcher.DispatchAsync(new GetAssetsQuery());
            var viewModels = assets
                .Select(a => new AssetViewModel(a, _currencySettings.MainFiatCurrency))
                .OrderBy(a => a.AssetType)
                .ThenBy(a => a.Name)
                .ToList();

            // Save current selection before clearing
            var currentSelectedAssetId = SelectedAsset?.Id;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Assets.Clear();
                Assets.AddRange(viewModels);

                // Restore selection after repopulating
                if (!string.IsNullOrEmpty(currentSelectedAssetId))
                {
                    SelectedAsset = Assets.FirstOrDefault(a => a.Id == currentSelectedAssetId);
                }
            });

            // Load summary with current rates
            var btcPriceUsd = _ratesState.BitcoinPrice;
            var fiatRates = _ratesState.FiatRates;

            Summary = await _queryDispatcher.DispatchAsync(new GetAssetSummaryQuery
            {
                MainCurrencyCode = _currencySettings.MainFiatCurrency,
                BtcPriceUsd = btcPriceUsd,
                FiatRates = fiatRates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assets");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddAsset()
    {
        var ownerWindow = GetUserControlOwnerWindow?.Invoke();
        if (ownerWindow is null)
            return;

        var modal = (ManageAssetView)await _modalFactory.CreateAsync(
            ApplicationModalNames.ManageAsset,
            ownerWindow);

        var result = await modal.ShowDialog<ManageAssetViewModel.Response?>(ownerWindow);

        if (result?.Ok == true)
        {
            await LoadAssetsAsync();
            NotifyAssetSummaryUpdated();
        }
    }

    [RelayCommand]
    private async Task EditAsset(AssetViewModel? asset)
    {
        if (asset is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow?.Invoke();
        if (ownerWindow is null)
            return;

        var modal = (ManageAssetView)await _modalFactory.CreateAsync(
            ApplicationModalNames.ManageAsset,
            ownerWindow,
            asset.Id);

        var result = await modal.ShowDialog<ManageAssetViewModel.Response?>(ownerWindow);

        if (result?.Ok == true)
        {
            await LoadAssetsAsync();
            NotifyAssetSummaryUpdated();
        }
    }

    [RelayCommand]
    private async Task DeleteAsset(AssetViewModel? asset)
    {
        if (asset is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow?.Invoke();
        if (ownerWindow is null)
            return;

        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.Assets_Delete_Alert,
            language.Assets_Delete_Message,
            ownerWindow);

        if (!confirmed)
            return;

        var result = await _commandDispatcher.DispatchAsync(new DeleteAssetCommand
        {
            AssetId = asset.Id
        });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, ownerWindow);
            return;
        }

        await LoadAssetsAsync();
        NotifyAssetSummaryUpdated();
    }

    [RelayCommand]
    private async Task SellAsset(AssetViewModel? asset)
    {
        if (asset is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow?.Invoke();
        if (ownerWindow is null)
            return;

        // Confirmation dialog
        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.Assets_Sell_Alert,
            language.Assets_Sell_Message,
            ownerWindow);

        if (!confirmed)
            return;

        // Generate pre-populated values
        var transactionName = GenerateSellTransactionName(asset);
        var transactionNotes = GenerateSellTransactionNotes(asset);
        var transactionMode = asset.CurrentValue >= 0
            ? TransactionTypes.Credit
            : TransactionTypes.Debt;
        var absoluteValue = FiatValue.New(Math.Abs(asset.CurrentValue));

        var request = new TransactionEditorViewModel.Request
        {
            Date = DateTime.Now,
            Name = transactionName,
            DefaultFromFiatValue = absoluteValue,
            Notes = transactionNotes,
            DefaultMode = transactionMode
        };

        var modal = (TransactionEditorView)await _modalFactory.CreateAsync(
            ApplicationModalNames.TransactionEditor,
            ownerWindow,
            request);

        var result = await modal.ShowDialog<TransactionEditorViewModel.Response?>(ownerWindow);

        // Only delete if transaction was saved
        if (result?.Ok == true)
        {
            var deleteResult = await _commandDispatcher.DispatchAsync(new DeleteAssetCommand
            {
                AssetId = asset.Id
            });

            if (deleteResult.IsFailure)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, deleteResult.Error!.Message, ownerWindow);
                return;
            }

            await LoadAssetsAsync();
            NotifyAssetSummaryUpdated();
        }
    }

    private static string GenerateSellTransactionName(AssetViewModel asset)
    {
        var pnl = asset.PnL ?? asset.CurrentValue;
        return pnl >= 0
            ? string.Format(language.Assets_Sell_TransactionName_Profit, asset.Name)
            : string.Format(language.Assets_Sell_TransactionName_Loss, asset.Name);
    }

    private static string GenerateSellTransactionNotes(AssetViewModel asset)
    {
        return asset.AssetType switch
        {
            AssetTypes.LeveragedPosition => GenerateLeveragedPositionNotes(asset),
            AssetTypes.RealEstate => GenerateRealEstateNotes(asset),
            _ => GenerateBasicAssetNotes(asset)
        };
    }

    private static string GenerateLeveragedPositionNotes(AssetViewModel asset)
    {
        var resultType = (asset.PnL ?? 0) >= 0
            ? language.Assets_Sell_Profit
            : language.Assets_Sell_Loss;
        return string.Format(language.Assets_Sell_Notes_Leveraged,
            resultType, asset.EntryPriceFormatted, asset.LeverageFormatted);
    }

    private static string GenerateRealEstateNotes(AssetViewModel asset)
    {
        var address = !string.IsNullOrEmpty(asset.Address) ? asset.Address : asset.Name;
        return string.Format(language.Assets_Sell_Notes_RealEstate,
            address, asset.AcquisitionPriceFormatted);
    }

    private static string GenerateBasicAssetNotes(AssetViewModel asset)
    {
        var identifier = !string.IsNullOrEmpty(asset.Symbol) ? asset.Symbol : asset.Name;
        return string.Format(language.Assets_Sell_Notes_Basic,
            identifier, asset.AcquisitionPriceFormatted);
    }

    [RelayCommand]
    private async Task ToggleVisibility(AssetViewModel? asset)
    {
        if (asset is null)
            return;

        var result = await _commandDispatcher.DispatchAsync(new SetAssetVisibilityCommand
        {
            AssetId = asset.Id,
            Visible = !asset.Visible
        });

        if (result.IsFailure)
        {
            var ownerWindow = GetUserControlOwnerWindow?.Invoke();
            if (ownerWindow is not null)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, ownerWindow);
            }
            return;
        }

        await LoadAssetsAsync();
        NotifyAssetSummaryUpdated();
    }

    [RelayCommand]
    private async Task ToggleIncludeInNetWorth(AssetViewModel? asset)
    {
        if (asset is null)
            return;

        var result = await _commandDispatcher.DispatchAsync(new SetAssetIncludeInNetWorthCommand
        {
            AssetId = asset.Id,
            IncludeInNetWorth = !asset.IncludeInNetWorth
        });

        if (result.IsFailure)
        {
            var ownerWindow = GetUserControlOwnerWindow?.Invoke();
            if (ownerWindow is not null)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, ownerWindow);
            }
            return;
        }

        await LoadAssetsAsync();
        NotifyAssetSummaryUpdated();
    }

    [RelayCommand(CanExecute = nameof(CanRefreshPrices))]
    private async Task RefreshPrices()
    {
        try
        {
            IsRefreshingPrices = true;
            await _backgroundJobManager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.AssetPriceUpdater);
            await LoadAssetsAsync();
            NotifyAssetSummaryUpdated();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing asset prices");
        }
        finally
        {
            IsRefreshingPrices = false;
        }
    }

    private bool CanRefreshPrices() => !IsRefreshingPrices;

    partial void OnIsRefreshingPricesChanged(bool value)
    {
        RefreshPricesCommand.NotifyCanExecuteChanged();
    }

    private void NotifyAssetSummaryUpdated()
    {
        WeakReferenceMessenger.Default.Send(new AssetSummaryUpdatedMessage());
    }

    private void OnSecureModeStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsSecureModeEnabled));
    }

    private void OnRatesStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Refresh summary when rates change
        LoadAssetsAsync().SafeFireAndForget(logger: _logger, callerName: nameof(LoadAssetsAsync));
    }

    private void OnAssetPriceUpdaterJobPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(JobInfo.State) && _assetPriceUpdaterJobInfo != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsRefreshingPrices = _assetPriceUpdaterJobInfo.State == BackgroundJobState.Running;
            });
        }
    }

    public void Dispose()
    {
        _secureModeState.PropertyChanged -= OnSecureModeStatePropertyChanged;
        _ratesState.PropertyChanged -= OnRatesStatePropertyChanged;
        if (_assetPriceUpdaterJobInfo != null)
            _assetPriceUpdaterJobInfo.PropertyChanged -= OnAssetPriceUpdaterJobPropertyChanged;
        WeakReferenceMessenger.Default.Unregister<SettingsChangedMessage>(this);
    }
}
