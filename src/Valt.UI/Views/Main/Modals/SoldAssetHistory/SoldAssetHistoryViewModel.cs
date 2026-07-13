using System;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.UndoAssetSale;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetSoldAssets;
using Valt.Infra.Kernel;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State.Events;
using Valt.UI.Views.Main.Tabs.Assets.Models;

namespace Valt.UI.Views.Main.Modals.SoldAssetHistory;

public partial class SoldAssetHistoryViewModel : ValtModalViewModel
{
    private readonly IQueryDispatcher _queryDispatcher = null!;
    private readonly ICommandDispatcher _commandDispatcher = null!;
    private readonly CurrencySettings _currencySettings = null!;

    [ObservableProperty] private string _windowTitle = "Sold Asset History";
    [ObservableProperty] private bool _isLoadingError;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private SoldAssetItemViewModel? _selectedSoldAsset;

    public AvaloniaList<SoldAssetItemViewModel> SoldAssets { get; set; } = new();

    public bool HasSoldAssets => SoldAssets.Count > 0;
    public bool HasNoSoldAssets => !IsLoadingError && SoldAssets.Count == 0;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public SoldAssetHistoryViewModel()
    {
        if (!Design.IsDesignMode) return;

        SoldAssets.Add(CreateDesignTimeItem("1", "Apple Inc.", 0, "Stock", "\xE8F5", "AAPL", 100m, new DateOnly(2024, 6, 1)));
        SoldAssets.Add(CreateDesignTimeItem("2", "Bitcoin ETF", 1, "ETF", "\xE8F5", "IBIT", 1000m, new DateOnly(2024, 5, 15)));
        SoldAssets.Add(CreateDesignTimeItem("3", "Beach House", 3, "Real Estate", "\xE88A", null, null, new DateOnly(2024, 4, 20), 450000m, "123 Ocean Drive, Miami FL"));
    }

    private static SoldAssetItemViewModel CreateDesignTimeItem(
        string id, string name, int assetTypeId, string assetTypeName, string icon, string? symbol, decimal? quantity, DateOnly dateSold, decimal? currentValue = null, string? address = null)
    {
        var value = currentValue ?? (quantity.HasValue && symbol is not null ? 1000m : 0m);
        var dto = new AssetDTO
        {
            Id = id,
            Name = name,
            AssetTypeId = assetTypeId,
            AssetTypeName = assetTypeName,
            Icon = icon,
            IncludeInNetWorth = true,
            Visible = true,
            LastPriceUpdateAt = DateTime.Now,
            CreatedAt = DateTime.Now,
            DisplayOrder = 1,
            CurrentPrice = 10m,
            CurrentValue = value,
            CurrencyCode = "USD",
            Quantity = quantity,
            Symbol = symbol,
            Address = address,
            IsSold = true,
            DateSold = dateSold
        };
        return new SoldAssetItemViewModel(dto, "USD");
    }

    public SoldAssetHistoryViewModel(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        CurrencySettings currencySettings)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _currencySettings = currencySettings;
    }

    public override async Task OnBindParameterAsync()
    {
        await LoadSoldAssetsAsync();
    }

    private async Task LoadSoldAssetsAsync()
    {
        IsLoadingError = false;
        ErrorMessage = string.Empty;

        try
        {
            var assets = await _queryDispatcher.DispatchAsync(new GetSoldAssetsQuery());
            SoldAssets.Clear();
            foreach (var dto in assets)
            {
                SoldAssets.Add(new SoldAssetItemViewModel(dto, _currencySettings.MainFiatCurrency));
            }
        }
        catch (Exception)
        {
            IsLoadingError = true;
            ErrorMessage = "Check your data and try again. If the problem persists, contact support.";
        }

        OnPropertyChanged(nameof(HasSoldAssets));
        OnPropertyChanged(nameof(HasNoSoldAssets));
        RestoreAssetCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSoldAssetChanged(SoldAssetItemViewModel? value)
    {
        RestoreAssetCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoSoldAssets));
    }

    [RelayCommand(CanExecute = nameof(CanRestoreAsset))]
    private async Task RestoreAsset()
    {
        if (SelectedSoldAsset is null || GetWindow is null)
            return;

        var ownerWindow = GetWindow();
        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            "Restore Asset?",
            "Restore this asset to the active Assets view?",
            ownerWindow);

        if (!confirmed)
            return;

        var result = await _commandDispatcher.DispatchAsync(new UndoAssetSaleCommand
        {
            AssetId = SelectedSoldAsset.Id
        });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync("Error", result.Error!.Message, ownerWindow);
            return;
        }

        WeakReferenceMessenger.Default.Send(new AssetSummaryUpdatedMessage());
        CloseWindow?.Invoke();
    }

    private bool CanRestoreAsset() => SelectedSoldAsset is not null;

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record Response
    {
    }

    public record SoldAssetItemViewModel
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string AssetTypeName { get; init; } = string.Empty;
        public string Icon { get; init; } = string.Empty;
        public DateOnly DateSold { get; init; } = DateOnly.MinValue;
        public string DateSoldFormatted { get; init; } = string.Empty;
        public AssetViewModel AssetDetails { get; }

        public SoldAssetItemViewModel(AssetDTO dto, string mainCurrencyCode)
        {
            Id = dto.Id;
            Name = dto.Name;
            AssetTypeName = dto.AssetTypeName;
            Icon = dto.Icon;
            DateSold = dto.DateSold ?? DateOnly.MinValue;
            DateSoldFormatted = DateSold.ToShortDateString();
            AssetDetails = new AssetViewModel(dto, mainCurrencyCode);
        }
    }
}
