using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;
using Valt.Infra.Modules.Assets.Queries;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ManageAsset;

public partial class ManageAssetViewModel : ValtModalValidatorViewModel
{
    private readonly IAssetRepository? _assetRepository;
    private readonly IAssetQueries? _assetQueries;
    private readonly CurrencySettings? _currencySettings;
    private readonly IConfigurationManager? _configurationManager;
    private readonly ILogger<ManageAssetViewModel>? _logger;

    private string? _assetId;

    #region Form Data

    [ObservableProperty]
    [Required(ErrorMessage = "Name is required")]
    [NotifyDataErrorInfo]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBasicFields))]
    [NotifyPropertyChangedFor(nameof(ShowRealEstateFields))]
    [NotifyPropertyChangedFor(nameof(ShowLeveragedFields))]
    private string _selectedAssetType = AssetTypes.Stock.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrencySymbol))]
    [NotifyPropertyChangedFor(nameof(SymbolOnRight))]
    private string _selectedCurrency = "USD";

    // Basic asset fields
    [ObservableProperty]
    private string _symbol = string.Empty;

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private FiatValue _currentPriceFiat = FiatValue.Empty;

    [ObservableProperty]
    private string _selectedPriceSource = AssetPriceSource.Manual.ToString();

    // Real estate fields
    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private FiatValue _currentValueFiat = FiatValue.Empty;

    [ObservableProperty]
    private FiatValue _monthlyRentalIncomeFiat = FiatValue.Empty;

    // Leveraged position fields
    [ObservableProperty]
    private FiatValue _collateralFiat = FiatValue.Empty;

    [ObservableProperty]
    private FiatValue _entryPriceFiat = FiatValue.Empty;

    [ObservableProperty]
    private decimal _leverage = 1;

    [ObservableProperty]
    private FiatValue _liquidationPriceFiat = FiatValue.Empty;

    // Currency symbol helpers
    public string CurrencySymbol => FiatCurrency.GetFromCode(SelectedCurrency).Symbol;
    public bool SymbolOnRight => FiatCurrency.GetFromCode(SelectedCurrency).SymbolOnRight;

    [ObservableProperty]
    private bool _isLong = true;

    // Common fields
    [ObservableProperty]
    private bool _includeInNetWorth = true;

    [ObservableProperty]
    private bool _visible = true;

    [ObservableProperty]
    private bool _isEditMode;

    // Visibility helpers
    public bool ShowBasicFields => SelectedAssetType is "Stock" or "Etf" or "Crypto" or "Commodity" or "Custom";
    public bool ShowRealEstateFields => SelectedAssetType == "RealEstate";
    public bool ShowLeveragedFields => SelectedAssetType == "LeveragedPosition";

    public static List<ComboBoxValue> AvailableAssetTypes =>
    [
        new(language.Assets_Type_Stock, AssetTypes.Stock.ToString()),
        new(language.Assets_Type_Etf, AssetTypes.Etf.ToString()),
        new(language.Assets_Type_Crypto, AssetTypes.Crypto.ToString()),
        new(language.Assets_Type_Commodity, AssetTypes.Commodity.ToString()),
        new(language.Assets_Type_RealEstate, AssetTypes.RealEstate.ToString()),
        new(language.Assets_Type_LeveragedPosition, AssetTypes.LeveragedPosition.ToString()),
        new(language.Assets_Type_Custom, AssetTypes.Custom.ToString())
    ];

    public static List<ComboBoxValue> AvailablePriceSources =>
    [
        new(language.Assets_PriceSource_Manual, AssetPriceSource.Manual.ToString()),
        new(language.Assets_PriceSource_YahooFinance, AssetPriceSource.YahooFinance.ToString()),
        new(language.Assets_PriceSource_CoinGecko, AssetPriceSource.CoinGecko.ToString())
    ];

    public List<string> AvailableCurrencies => _configurationManager?.GetAvailableFiatCurrencies()
        .OrderBy(x => x)
        .ToList() ?? new List<string> { FiatCurrency.Usd.Code };

    #endregion

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ManageAssetViewModel()
    {
        SelectedAssetType = AssetTypes.Stock.ToString();
    }

    public ManageAssetViewModel(
        IAssetRepository assetRepository,
        IAssetQueries assetQueries,
        CurrencySettings currencySettings,
        IConfigurationManager configurationManager,
        ILogger<ManageAssetViewModel> logger)
    {
        _assetRepository = assetRepository;
        _assetQueries = assetQueries;
        _currencySettings = currencySettings;
        _configurationManager = configurationManager;
        _logger = logger;

        SelectedAssetType = AssetTypes.Stock.ToString();
        SelectedCurrency = currencySettings.MainFiatCurrency;
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is string assetId && !string.IsNullOrWhiteSpace(assetId))
        {
            var assetDto = await _assetQueries!.GetByIdAsync(assetId);
            if (assetDto is null)
            {
                await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, language.Error_AssetNotFound, GetWindow!());
                return;
            }

            _assetId = assetDto.Id;
            IsEditMode = true;

            // Load common fields
            Name = assetDto.Name;
            SelectedAssetType = ((AssetTypes)assetDto.AssetTypeId).ToString();
            SelectedCurrency = assetDto.CurrencyCode;
            IncludeInNetWorth = assetDto.IncludeInNetWorth;
            Visible = assetDto.Visible;

            // Load type-specific fields
            switch ((AssetTypes)assetDto.AssetTypeId)
            {
                case AssetTypes.Stock:
                case AssetTypes.Etf:
                case AssetTypes.Crypto:
                case AssetTypes.Commodity:
                case AssetTypes.Custom:
                    Symbol = assetDto.Symbol ?? string.Empty;
                    Quantity = assetDto.Quantity ?? 0;
                    CurrentPriceFiat = FiatValue.New(assetDto.CurrentPrice);
                    SelectedPriceSource = ((AssetPriceSource)(assetDto.PriceSourceId ?? 0)).ToString();
                    break;

                case AssetTypes.RealEstate:
                    Address = assetDto.Address ?? string.Empty;
                    CurrentValueFiat = FiatValue.New(assetDto.CurrentValue);
                    MonthlyRentalIncomeFiat = FiatValue.New(assetDto.MonthlyRentalIncome ?? 0);
                    break;

                case AssetTypes.LeveragedPosition:
                    Symbol = assetDto.Symbol ?? string.Empty;
                    CollateralFiat = FiatValue.New(assetDto.Collateral ?? 0);
                    EntryPriceFiat = FiatValue.New(assetDto.EntryPrice ?? 0);
                    CurrentPriceFiat = FiatValue.New(assetDto.CurrentPrice);
                    Leverage = assetDto.Leverage ?? 1;
                    LiquidationPriceFiat = FiatValue.New(assetDto.LiquidationPrice ?? 0);
                    IsLong = assetDto.IsLong ?? true;
                    SelectedPriceSource = ((AssetPriceSource)(assetDto.PriceSourceId ?? 0)).ToString();
                    break;
            }
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (HasErrors)
            return;

        try
        {
            var assetType = Enum.Parse<AssetTypes>(SelectedAssetType);
            var assetName = new AssetName(Name);
            IAssetDetails details;

            switch (assetType)
            {
                case AssetTypes.Stock:
                case AssetTypes.Etf:
                case AssetTypes.Crypto:
                case AssetTypes.Commodity:
                case AssetTypes.Custom:
                    var priceSource = Enum.Parse<AssetPriceSource>(SelectedPriceSource);
                    details = new BasicAssetDetails(
                        assetType: assetType,
                        quantity: Quantity,
                        symbol: Symbol,
                        priceSource: priceSource,
                        currentPrice: CurrentPriceFiat.Value,
                        currencyCode: SelectedCurrency);
                    break;

                case AssetTypes.RealEstate:
                    details = new RealEstateAssetDetails(
                        address: string.IsNullOrWhiteSpace(Address) ? null : Address,
                        currentValue: CurrentValueFiat.Value,
                        currencyCode: SelectedCurrency,
                        monthlyRentalIncome: MonthlyRentalIncomeFiat.Value > 0 ? MonthlyRentalIncomeFiat.Value : null);
                    break;

                case AssetTypes.LeveragedPosition:
                    var leveragedPriceSource = Enum.Parse<AssetPriceSource>(SelectedPriceSource);
                    details = new LeveragedPositionDetails(
                        collateral: CollateralFiat.Value,
                        entryPrice: EntryPriceFiat.Value,
                        leverage: Leverage,
                        liquidationPrice: LiquidationPriceFiat.Value,
                        currentPrice: CurrentPriceFiat.Value,
                        currencyCode: SelectedCurrency,
                        symbol: Symbol,
                        priceSource: leveragedPriceSource,
                        isLong: IsLong);
                    break;

                default:
                    await MessageBoxHelper.ShowErrorAsync(language.Error, "Unsupported asset type", GetWindow!());
                    return;
            }

            if (_assetId is null)
            {
                // Create new asset
                var asset = Asset.New(assetName, details, Icon.Empty, IncludeInNetWorth, Visible);
                await _assetRepository!.SaveAsync(asset);
                CloseDialog?.Invoke(new Response(true, asset.Id.ToString()));
            }
            else
            {
                // Edit existing asset
                var asset = await _assetRepository!.GetByIdAsync(new AssetId(_assetId));
                if (asset is null)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_AssetNotFound, GetWindow!());
                    return;
                }

                asset.Edit(assetName, details, asset.Icon, IncludeInNetWorth, Visible);
                await _assetRepository.SaveAsync(asset);
                CloseDialog?.Invoke(new Response(true, _assetId));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving asset");
            await MessageBoxHelper.ShowErrorAsync(language.Error, ex.Message, GetWindow!());
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record Response(bool Ok, string? AssetId = null);
}
