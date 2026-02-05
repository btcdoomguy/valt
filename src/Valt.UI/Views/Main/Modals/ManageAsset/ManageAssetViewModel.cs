using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.CreateBasicAsset;
using Valt.App.Modules.Assets.Commands.CreateLeveragedPosition;
using Valt.App.Modules.Assets.Commands.CreateRealEstateAsset;
using Valt.App.Modules.Assets.Commands.EditAsset;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetAsset;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Infra.Modules.Assets.PriceProviders;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ManageAsset;

public partial class ManageAssetViewModel : ValtModalValidatorViewModel
{
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IAssetPriceProviderSelector? _priceProviderSelector;
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
    [NotifyPropertyChangedFor(nameof(IsBitcoinLeveraged))]
    [NotifyPropertyChangedFor(nameof(IsCustomLeveraged))]
    [NotifyPropertyChangedFor(nameof(ShowLeveragedSymbolRow))]
    private string _selectedAssetType = AssetTypes.Stock.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrencySymbol))]
    [NotifyPropertyChangedFor(nameof(SymbolOnRight))]
    private string _selectedCurrency = "USD";

    // Leveraged position underlying asset toggle (Bitcoin vs Custom)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBitcoinLeveraged))]
    [NotifyPropertyChangedFor(nameof(IsCustomLeveraged))]
    [NotifyPropertyChangedFor(nameof(ShowLeveragedSymbolRow))]
    private bool _isBitcoinUnderlyingAsset = false;

    // Basic asset fields
    private string _symbol = string.Empty;
    public string Symbol
    {
        get => _symbol;
        set
        {
            if (SetProperty(ref _symbol, value?.ToUpperInvariant() ?? string.Empty))
            {
                OnPropertyChanged(nameof(IsBitcoinLeveraged));
            }
        }
    }

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private FiatValue _currentPriceFiat = FiatValue.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCheckSymbolButton))]
    [NotifyPropertyChangedFor(nameof(ShowCurrentPriceField))]
    [NotifyPropertyChangedFor(nameof(ShowYahooFinanceHelp))]
    [NotifyPropertyChangedFor(nameof(IsBitcoinLeveraged))]
    [NotifyPropertyChangedFor(nameof(IsCustomLeveraged))]
    private string _selectedPriceSource = AssetPriceSource.Manual.ToString();

    // Real estate fields
    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private FiatValue _currentValueFiat = FiatValue.Empty;

    [ObservableProperty]
    private FiatValue _monthlyRentalIncomeFiat = FiatValue.Empty;

    // Acquisition fields (common for basic assets and real estate)
    [ObservableProperty]
    private DateTimeOffset? _acquisitionDate;

    [ObservableProperty]
    private FiatValue _acquisitionPriceFiat = FiatValue.Empty;

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

    // Symbol validation
    [ObservableProperty]
    private bool _isCheckingSymbol;

    [ObservableProperty]
    private string? _symbolValidationMessage;

    [ObservableProperty]
    private bool _isSymbolValid;

    public bool ShowCheckSymbolButton => SelectedPriceSource != AssetPriceSource.Manual.ToString();
    public bool ShowCurrentPriceField => SelectedPriceSource == AssetPriceSource.Manual.ToString();
    public bool ShowYahooFinanceHelp => SelectedPriceSource == AssetPriceSource.YahooFinance.ToString();

    // Visibility helpers
    public bool ShowBasicFields => SelectedAssetType is "Stock" or "Etf" or "Crypto" or "Commodity" or "Custom";
    public bool ShowRealEstateFields => SelectedAssetType == "RealEstate";
    public bool ShowLeveragedFields => SelectedAssetType == "LeveragedPosition";

    // Leveraged position Bitcoin/Custom toggle helpers
    public bool IsBitcoinLeveraged => ShowLeveragedFields && IsBitcoinUnderlyingAsset;
    public bool IsCustomLeveraged => ShowLeveragedFields && !IsBitcoinUnderlyingAsset;
    public bool ShowLeveragedSymbolRow => ShowLeveragedFields && !IsBitcoinUnderlyingAsset;

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
        new(language.Assets_PriceSource_YahooFinance, AssetPriceSource.YahooFinance.ToString())
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
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        IAssetPriceProviderSelector priceProviderSelector,
        CurrencySettings currencySettings,
        IConfigurationManager configurationManager,
        ILogger<ManageAssetViewModel> logger)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _priceProviderSelector = priceProviderSelector;
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
            var assetDto = await _queryDispatcher!.DispatchAsync(new GetAssetQuery { AssetId = assetId });
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
                    if (assetDto.AcquisitionDate.HasValue)
                        AcquisitionDate = assetDto.AcquisitionDate.Value.ToDateTime(TimeOnly.MinValue);
                    AcquisitionPriceFiat = FiatValue.New(assetDto.AcquisitionPrice ?? 0);
                    break;

                case AssetTypes.RealEstate:
                    Address = assetDto.Address ?? string.Empty;
                    CurrentValueFiat = FiatValue.New(assetDto.CurrentValue);
                    MonthlyRentalIncomeFiat = FiatValue.New(assetDto.MonthlyRentalIncome ?? 0);
                    if (assetDto.AcquisitionDate.HasValue)
                        AcquisitionDate = assetDto.AcquisitionDate.Value.ToDateTime(TimeOnly.MinValue);
                    AcquisitionPriceFiat = FiatValue.New(assetDto.AcquisitionPrice ?? 0);
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
                    // Detect if this is a Bitcoin leveraged position
                    IsBitcoinUnderlyingAsset = (AssetPriceSource)(assetDto.PriceSourceId ?? 0) == AssetPriceSource.LivePrice &&
                                                Symbol.StartsWith("BTC", StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }
    }

    [RelayCommand]
    private async Task CheckSymbol()
    {
        if (string.IsNullOrWhiteSpace(Symbol) || _priceProviderSelector is null)
        {
            SymbolValidationMessage = language.ManageAsset_SymbolInvalid;
            IsSymbolValid = false;
            return;
        }

        try
        {
            IsCheckingSymbol = true;
            SymbolValidationMessage = language.ManageAsset_CheckingSymbol;
            IsSymbolValid = false;

            var priceSource = Enum.Parse<AssetPriceSource>(SelectedPriceSource);
            var isValid = await _priceProviderSelector.ValidateSymbolAsync(priceSource, Symbol);

            if (isValid)
            {
                SymbolValidationMessage = language.ManageAsset_SymbolValid;
                IsSymbolValid = true;
            }
            else
            {
                SymbolValidationMessage = language.ManageAsset_SymbolInvalid;
                IsSymbolValid = false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error validating symbol {Symbol}", Symbol);
            SymbolValidationMessage = language.ManageAsset_SymbolInvalid;
            IsSymbolValid = false;
        }
        finally
        {
            IsCheckingSymbol = false;
        }
    }

    [RelayCommand]
    private void SetBitcoinLeveraged()
    {
        IsBitcoinUnderlyingAsset = true;
        Symbol = "BTC";
        SelectedPriceSource = AssetPriceSource.LivePrice.ToString();
        // Keep user's selected currency - don't force USD
        SymbolValidationMessage = null;
        IsSymbolValid = true;
    }

    [RelayCommand]
    private void SetCustomLeveraged()
    {
        IsBitcoinUnderlyingAsset = false;
        Symbol = string.Empty;
        SelectedPriceSource = AssetPriceSource.Manual.ToString();
        SymbolValidationMessage = null;
        IsSymbolValid = false;
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

            if (_assetId is null)
            {
                // Create new asset
                await CreateNewAssetAsync(assetType);
            }
            else
            {
                // Edit existing asset
                await EditExistingAssetAsync(assetType);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving asset");
            await MessageBoxHelper.ShowErrorAsync(language.Error, ex.Message, GetWindow!());
        }
    }

    private async Task CreateNewAssetAsync(AssetTypes assetType)
    {
        switch (assetType)
        {
            case AssetTypes.Stock:
            case AssetTypes.Etf:
            case AssetTypes.Crypto:
            case AssetTypes.Commodity:
            case AssetTypes.Custom:
                var priceSource = Enum.Parse<AssetPriceSource>(SelectedPriceSource);
                var currentPrice = CurrentPriceFiat.Value;

                // Fetch price from provider if not Manual
                if (priceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(Symbol))
                {
                    var priceResult = await _priceProviderSelector!.GetPriceAsync(priceSource, Symbol, SelectedCurrency);
                    if (priceResult is not null)
                    {
                        currentPrice = priceResult.Price;
                    }
                }

                var basicResult = await _commandDispatcher!.DispatchAsync(new CreateBasicAssetCommand
                {
                    Name = Name,
                    AssetType = (int)assetType,
                    CurrencyCode = SelectedCurrency,
                    Symbol = Symbol,
                    Quantity = Quantity,
                    CurrentPrice = currentPrice,
                    PriceSource = (int)priceSource,
                    AcquisitionDate = AcquisitionDate.HasValue ? DateOnly.FromDateTime(AcquisitionDate.Value.DateTime) : null,
                    AcquisitionPrice = AcquisitionPriceFiat.Value > 0 ? AcquisitionPriceFiat.Value : null,
                    IncludeInNetWorth = IncludeInNetWorth,
                    Visible = Visible
                });

                if (basicResult.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, basicResult.Error!.Message, GetWindow!());
                    return;
                }

                CloseDialog?.Invoke(new Response(true, basicResult.Value!.AssetId));
                break;

            case AssetTypes.RealEstate:
                var realEstateResult = await _commandDispatcher!.DispatchAsync(new CreateRealEstateAssetCommand
                {
                    Name = Name,
                    CurrencyCode = SelectedCurrency,
                    CurrentValue = CurrentValueFiat.Value,
                    Address = string.IsNullOrWhiteSpace(Address) ? null : Address,
                    MonthlyRentalIncome = MonthlyRentalIncomeFiat.Value > 0 ? MonthlyRentalIncomeFiat.Value : null,
                    AcquisitionDate = AcquisitionDate.HasValue ? DateOnly.FromDateTime(AcquisitionDate.Value.DateTime) : null,
                    AcquisitionPrice = AcquisitionPriceFiat.Value > 0 ? AcquisitionPriceFiat.Value : null,
                    IncludeInNetWorth = IncludeInNetWorth,
                    Visible = Visible
                });

                if (realEstateResult.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, realEstateResult.Error!.Message, GetWindow!());
                    return;
                }

                CloseDialog?.Invoke(new Response(true, realEstateResult.Value!.AssetId));
                break;

            case AssetTypes.LeveragedPosition:
                var leveragedPriceSource = Enum.Parse<AssetPriceSource>(SelectedPriceSource);
                var leveragedCurrentPrice = CurrentPriceFiat.Value;

                // Fetch price from provider if not Manual
                if (leveragedPriceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(Symbol))
                {
                    var priceResult = await _priceProviderSelector!.GetPriceAsync(leveragedPriceSource, Symbol, SelectedCurrency);
                    if (priceResult is not null)
                    {
                        leveragedCurrentPrice = priceResult.Price;
                    }
                }

                var leveragedResult = await _commandDispatcher!.DispatchAsync(new CreateLeveragedPositionCommand
                {
                    Name = Name,
                    CurrencyCode = SelectedCurrency,
                    Symbol = Symbol,
                    Collateral = CollateralFiat.Value,
                    EntryPrice = EntryPriceFiat.Value,
                    CurrentPrice = leveragedCurrentPrice,
                    Leverage = Leverage,
                    LiquidationPrice = LiquidationPriceFiat.Value,
                    IsLong = IsLong,
                    PriceSource = (int)leveragedPriceSource,
                    IncludeInNetWorth = IncludeInNetWorth,
                    Visible = Visible
                });

                if (leveragedResult.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, leveragedResult.Error!.Message, GetWindow!());
                    return;
                }

                CloseDialog?.Invoke(new Response(true, leveragedResult.Value!.AssetId));
                break;

            default:
                await MessageBoxHelper.ShowErrorAsync(language.Error, "Unsupported asset type", GetWindow!());
                return;
        }
    }

    private async Task EditExistingAssetAsync(AssetTypes assetType)
    {
        AssetDetailsInputDTO details;

        switch (assetType)
        {
            case AssetTypes.Stock:
            case AssetTypes.Etf:
            case AssetTypes.Crypto:
            case AssetTypes.Commodity:
            case AssetTypes.Custom:
                var priceSource = Enum.Parse<AssetPriceSource>(SelectedPriceSource);
                var currentPrice = CurrentPriceFiat.Value;

                // Fetch price from provider if not Manual
                if (priceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(Symbol))
                {
                    var priceResult = await _priceProviderSelector!.GetPriceAsync(priceSource, Symbol, SelectedCurrency);
                    if (priceResult is not null)
                    {
                        currentPrice = priceResult.Price;
                    }
                }

                details = new BasicAssetDetailsInputDTO
                {
                    AssetType = (int)assetType,
                    CurrencyCode = SelectedCurrency,
                    Symbol = Symbol,
                    Quantity = Quantity,
                    CurrentPrice = currentPrice,
                    PriceSource = (int)priceSource,
                    AcquisitionDate = AcquisitionDate.HasValue ? DateOnly.FromDateTime(AcquisitionDate.Value.DateTime) : null,
                    AcquisitionPrice = AcquisitionPriceFiat.Value > 0 ? AcquisitionPriceFiat.Value : null
                };
                break;

            case AssetTypes.RealEstate:
                details = new RealEstateAssetDetailsInputDTO
                {
                    CurrencyCode = SelectedCurrency,
                    CurrentValue = CurrentValueFiat.Value,
                    Address = string.IsNullOrWhiteSpace(Address) ? null : Address,
                    MonthlyRentalIncome = MonthlyRentalIncomeFiat.Value > 0 ? MonthlyRentalIncomeFiat.Value : null,
                    AcquisitionDate = AcquisitionDate.HasValue ? DateOnly.FromDateTime(AcquisitionDate.Value.DateTime) : null,
                    AcquisitionPrice = AcquisitionPriceFiat.Value > 0 ? AcquisitionPriceFiat.Value : null
                };
                break;

            case AssetTypes.LeveragedPosition:
                var leveragedPriceSource = Enum.Parse<AssetPriceSource>(SelectedPriceSource);
                var leveragedCurrentPrice = CurrentPriceFiat.Value;

                // Fetch price from provider if not Manual
                if (leveragedPriceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(Symbol))
                {
                    var priceResult = await _priceProviderSelector!.GetPriceAsync(leveragedPriceSource, Symbol, SelectedCurrency);
                    if (priceResult is not null)
                    {
                        leveragedCurrentPrice = priceResult.Price;
                    }
                }

                details = new LeveragedPositionDetailsInputDTO
                {
                    CurrencyCode = SelectedCurrency,
                    Symbol = Symbol,
                    Collateral = CollateralFiat.Value,
                    EntryPrice = EntryPriceFiat.Value,
                    CurrentPrice = leveragedCurrentPrice,
                    Leverage = Leverage,
                    LiquidationPrice = LiquidationPriceFiat.Value,
                    IsLong = IsLong,
                    PriceSource = (int)leveragedPriceSource
                };
                break;

            default:
                await MessageBoxHelper.ShowErrorAsync(language.Error, "Unsupported asset type", GetWindow!());
                return;
        }

        var result = await _commandDispatcher!.DispatchAsync(new EditAssetCommand
        {
            AssetId = _assetId!,
            Name = Name,
            Details = details,
            IncludeInNetWorth = IncludeInNetWorth,
            Visible = Visible
        });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
            return;
        }

        CloseDialog?.Invoke(new Response(true, _assetId));
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
