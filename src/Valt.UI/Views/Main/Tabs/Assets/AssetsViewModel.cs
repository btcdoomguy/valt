using System;
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
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.State;
using Valt.UI.State.Events;
using Valt.UI.Views;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.ManageAsset;
using Valt.UI.Views.Main.Tabs.Assets.Models;
using static Valt.UI.Base.TaskExtensions;

namespace Valt.UI.Views.Main.Tabs.Assets;

public partial class AssetsViewModel : ValtTabViewModel, IDisposable
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly CurrencySettings _currencySettings;
    private readonly RatesState _ratesState;
    private readonly SecureModeState _secureModeState;
    private readonly IModalFactory _modalFactory;
    private readonly ILogger<AssetsViewModel> _logger;

    [ObservableProperty] private AvaloniaList<AssetViewModel> _assets = new();
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalValueColor))]
    [NotifyPropertyChangedFor(nameof(TotalSatsColor))]
    [NotifyPropertyChangedFor(nameof(TotalValueFormatted))]
    private AssetSummaryDTO? _summary;
    [ObservableProperty] private AssetViewModel? _selectedAsset;

    public bool IsSecureModeEnabled => _secureModeState.IsEnabled;

    public string MainCurrencyCode => _currencySettings.MainFiatCurrency;

    public string TotalValueColor => Summary?.TotalValueInMainCurrency >= 0 ? "#FFFFFF" : "#F44336";
    public string TotalSatsColor => Summary?.TotalValueInSats >= 0 ? "#FFFFFF" : "#F44336";

    public string TotalValueFormatted => Summary != null
        ? CurrencyDisplay.FormatFiat(Summary.TotalValueInMainCurrency, MainCurrencyCode)
        : "-";

    public AssetsViewModel(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        CurrencySettings currencySettings,
        RatesState ratesState,
        SecureModeState secureModeState,
        IModalFactory modalFactory,
        ILogger<AssetsViewModel> logger)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _currencySettings = currencySettings;
        _ratesState = ratesState;
        _secureModeState = secureModeState;
        _modalFactory = modalFactory;
        _logger = logger;

        _secureModeState.PropertyChanged += OnSecureModeStatePropertyChanged;
        _ratesState.PropertyChanged += OnRatesStatePropertyChanged;

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
            var viewModels = assets.Select(a => new AssetViewModel(a, _currencySettings.MainFiatCurrency)).ToList();

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

    private void NotifyAssetSummaryUpdated()
    {
        WeakReferenceMessenger.Default.Send(new AssetSummaryUpdatedMessage());
    }

    private void OnSecureModeStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsSecureModeEnabled));
    }

    private void OnRatesStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Refresh summary when rates change
        LoadAssetsAsync().SafeFireAndForget(logger: _logger, callerName: nameof(LoadAssetsAsync));
    }

    public void Dispose()
    {
        _secureModeState.PropertyChanged -= OnSecureModeStatePropertyChanged;
        _ratesState.PropertyChanged -= OnRatesStatePropertyChanged;
        WeakReferenceMessenger.Default.Unregister<SettingsChangedMessage>(this);
    }
}
