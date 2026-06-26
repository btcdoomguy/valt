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
using Valt.UI.Services;
using Valt.UI.Services.Exceptions;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.LoanStateHistory;
using Valt.UI.Views.Main.Modals.UpdateLoanState;

namespace Valt.UI.Views.Main.Modals.ManageAsset;

public partial class ManageAssetViewModel : ValtModalValidatorViewModel
{
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IAssetPriceProviderSelector? _priceProviderSelector;
    private readonly CurrencySettings? _currencySettings;
    private readonly IConfigurationManager? _configurationManager;
    private readonly ILogger<ManageAssetViewModel>? _logger;
    private readonly IModalFactory? _modalFactory;

    private readonly IAssetFormBuilder? _assetFormBuilder;

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
    [NotifyPropertyChangedFor(nameof(ShowBtcLoanFields))]
    [NotifyPropertyChangedFor(nameof(ShowBtcLendingFields))]
    [NotifyPropertyChangedFor(nameof(ShowFixedTotalDebtToggle))]
    [NotifyPropertyChangedFor(nameof(ShowLoanStateActions))]
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
    private DateTime? _acquisitionDate;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCollateralMode))]
    [NotifyPropertyChangedFor(nameof(IsExactPositionMode))]
    private bool _useExactPosition;

    [ObservableProperty]
    private decimal _positionSize;

    private bool _isAutoCalculating;

    // Currency symbol helpers
    public string CurrencySymbol => FiatCurrency.GetFromCode(SelectedCurrency).Symbol;
    public bool SymbolOnRight => FiatCurrency.GetFromCode(SelectedCurrency).SymbolOnRight;

    [ObservableProperty]
    private bool _isLong = true;

    // BTC Loan fields
    [ObservableProperty]
    private string _platformName = string.Empty;

    [ObservableProperty]
    private long _collateralSats;

    [ObservableProperty]
    private FiatValue _loanAmountFiat = FiatValue.Empty;

    [ObservableProperty]
    private decimal _aprPercentage;

    [ObservableProperty]
    private decimal _initialLtvPercentage;

    [ObservableProperty]
    private decimal _marginCallLtvPercentage;

    [ObservableProperty]
    private decimal _liquidationLtvPercentage;

    [ObservableProperty]
    private FiatValue _feesFiat = FiatValue.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DerivedAprDisplay))]
    private DateTime? _loanStartDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DerivedAprDisplay))]
    private DateTime? _repaymentDateOffset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRepaymentDateField))]
    [NotifyPropertyChangedFor(nameof(ShowFixedTotalDebtToggle))]
    private bool _isIndefiniteLoan;

    public bool ShowRepaymentDateField => !IsIndefiniteLoan;

    partial void OnIsIndefiniteLoanChanged(bool value)
    {
        if (value)
        {
            RepaymentDateOffset = null;
            // Fixed total debt requires a repayment date — disable when indefinite
            UseFixedTotalDebt = false;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowApr))]
    [NotifyPropertyChangedFor(nameof(ShowFixedTotalDebtField))]
    [NotifyPropertyChangedFor(nameof(DerivedAprDisplay))]
    private bool _useFixedTotalDebt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DerivedAprDisplay))]
    private FiatValue _fixedTotalDebtFiat = FiatValue.Empty;

    public bool ShowFixedTotalDebtToggle => ShowBtcLoanFields && !IsIndefiniteLoan;
    public bool ShowFixedTotalDebtField => UseFixedTotalDebt;
    public bool ShowApr => !UseFixedTotalDebt;

    public string DerivedAprDisplay
    {
        get
        {
            if (!UseFixedTotalDebt || !LoanStartDate.HasValue || !RepaymentDateOffset.HasValue)
                return "-";

            var start = DateOnly.FromDateTime(LoanStartDate.Value);
            var end = DateOnly.FromDateTime(RepaymentDateOffset.Value);
            var apr = Core.Modules.Assets.Details.BtcLoanDetails.DeriveAprFromFixedDebt(
                LoanAmountFiat.Value, FixedTotalDebtFiat.Value, start, end);
            return (apr * 100m).ToString("0.##") + "%";
        }
    }

    partial void OnLoanAmountFiatChanged(FiatValue value) => OnPropertyChanged(nameof(DerivedAprDisplay));
    partial void OnFixedTotalDebtFiatChanged(FiatValue value) => OnPropertyChanged(nameof(DerivedAprDisplay));

    // BTC Lending fields
    [ObservableProperty]
    private string _borrowerOrPlatformName = string.Empty;

    [ObservableProperty]
    private FiatValue _amountLentFiat = FiatValue.Empty;

    [ObservableProperty]
    private decimal _lendingAprPercentage;

    [ObservableProperty]
    private DateTime? _lendingStartDateOffset;

    [ObservableProperty]
    private DateTime? _expectedRepaymentDateOffset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowExpectedRepaymentDateField))]
    private bool _isIndefiniteLending;

    public bool ShowExpectedRepaymentDateField => !IsIndefiniteLending;

    partial void OnIsIndefiniteLendingChanged(bool value)
    {
        if (value)
            ExpectedRepaymentDateOffset = null;
    }

    // Common fields
    [ObservableProperty]
    private bool _includeInNetWorth = true;

    [ObservableProperty]
    private bool _visible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLoanStateActions))]
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
    public bool ShowBtcLoanFields => SelectedAssetType == "BtcLoan";
    public bool ShowBtcLendingFields => SelectedAssetType == "BtcLending";
    public bool ShowLoanStateActions => ShowBtcLoanFields && IsEditMode;

    // Leveraged position Bitcoin/Custom toggle helpers
    public bool IsBitcoinLeveraged => ShowLeveragedFields && IsBitcoinUnderlyingAsset;
    public bool IsCustomLeveraged => ShowLeveragedFields && !IsBitcoinUnderlyingAsset;
    public bool ShowLeveragedSymbolRow => ShowLeveragedFields && !IsBitcoinUnderlyingAsset;

    // Leveraged position input mode helpers
    public bool IsCollateralMode => !UseExactPosition;
    public bool IsExactPositionMode => UseExactPosition;

    public static List<ComboBoxValue> AvailableAssetTypes =>
    [
        new(language.Assets_Type_Stock, AssetTypes.Stock.ToString()),
        new(language.Assets_Type_Etf, AssetTypes.Etf.ToString()),
        new(language.Assets_Type_Crypto, AssetTypes.Crypto.ToString()),
        new(language.Assets_Type_Commodity, AssetTypes.Commodity.ToString()),
        new(language.Assets_Type_RealEstate, AssetTypes.RealEstate.ToString()),
        new(language.Assets_Type_LeveragedPosition, AssetTypes.LeveragedPosition.ToString()),
        new(language.Assets_Type_Custom, AssetTypes.Custom.ToString()),
        new(language.Assets_Type_BtcLoan, AssetTypes.BtcLoan.ToString()),
        new(language.Assets_Type_BtcLending, AssetTypes.BtcLending.ToString())
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
        ILogger<ManageAssetViewModel> logger,
        IModalFactory modalFactory,
        IAssetFormBuilder assetFormBuilder)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _priceProviderSelector = priceProviderSelector;
        _currencySettings = currencySettings;
        _configurationManager = configurationManager;
        _logger = logger;
        _modalFactory = modalFactory;
        _assetFormBuilder = assetFormBuilder;

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

            var values = _assetFormBuilder!.LoadFromDto(assetDto);

            // Load common fields (set SelectedAssetType LAST so date pickers
            // are populated before their section becomes visible)
            Name = values.Name;
            SelectedCurrency = values.SelectedCurrency;
            IncludeInNetWorth = values.IncludeInNetWorth;
            Visible = values.Visible;

            // Load type-specific fields BEFORE setting SelectedAssetType
            Symbol = values.Symbol;
            Quantity = values.Quantity;
            CurrentPriceFiat = values.CurrentPriceFiat;
            SelectedPriceSource = values.SelectedPriceSource;
            Address = values.Address;
            CurrentValueFiat = values.CurrentValueFiat;
            MonthlyRentalIncomeFiat = values.MonthlyRentalIncomeFiat;
            AcquisitionDate = values.AcquisitionDate;
            AcquisitionPriceFiat = values.AcquisitionPriceFiat;
            IsBitcoinUnderlyingAsset = values.IsBitcoinUnderlyingAsset;
            CollateralFiat = values.CollateralFiat;
            EntryPriceFiat = values.EntryPriceFiat;
            Leverage = values.Leverage;
            LiquidationPriceFiat = values.LiquidationPriceFiat;
            IsLong = values.IsLong;
            UseExactPosition = values.UseExactPosition;
            PositionSize = values.PositionSize;
            PlatformName = values.PlatformName;
            CollateralSats = values.CollateralSats;
            LoanAmountFiat = values.LoanAmountFiat;
            AprPercentage = values.AprPercentage;
            InitialLtvPercentage = values.InitialLtvPercentage;
            LiquidationLtvPercentage = values.LiquidationLtvPercentage;
            MarginCallLtvPercentage = values.MarginCallLtvPercentage;
            FeesFiat = values.FeesFiat;
            LoanStartDate = values.LoanStartDate;
            RepaymentDateOffset = values.RepaymentDateOffset;
            IsIndefiniteLoan = values.IsIndefiniteLoan;
            UseFixedTotalDebt = values.UseFixedTotalDebt;
            FixedTotalDebtFiat = values.FixedTotalDebtFiat;
            BorrowerOrPlatformName = values.BorrowerOrPlatformName;
            AmountLentFiat = values.AmountLentFiat;
            LendingAprPercentage = values.LendingAprPercentage;
            LendingStartDateOffset = values.LendingStartDateOffset;
            ExpectedRepaymentDateOffset = values.ExpectedRepaymentDateOffset;
            IsIndefiniteLending = values.IsIndefiniteLending;

            // Set asset type LAST — this makes the type-specific section visible,
            // and by this point all field values (including dates) are already set
            SelectedAssetType = values.SelectedAssetType;
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
    private void SetCollateralMode()
    {
        UseExactPosition = false;
        RecalculateCollateralOrPosition();
    }

    [RelayCommand]
    private void SetExactPositionMode()
    {
        UseExactPosition = true;
        RecalculateCollateralOrPosition();
    }

    partial void OnPositionSizeChanged(decimal value)
    {
        if (_isAutoCalculating) return;
        if (!UseExactPosition) return;
        if (value <= 0 || Leverage <= 0 || EntryPriceFiat.Value <= 0) return;

        _isAutoCalculating = true;
        CollateralFiat = FiatValue.New(value * EntryPriceFiat.Value / Leverage);
        _isAutoCalculating = false;
    }

    partial void OnCollateralFiatChanged(FiatValue value)
    {
        if (_isAutoCalculating) return;
        if (UseExactPosition) return;
        RecalculatePositionSize();
    }

    partial void OnLeverageChanged(decimal value)
    {
        RecalculateCollateralOrPosition();
    }

    partial void OnEntryPriceFiatChanged(FiatValue value)
    {
        RecalculateCollateralOrPosition();
    }

    private void RecalculateCollateralOrPosition()
    {
        if (_isAutoCalculating) return;
        _isAutoCalculating = true;

        if (UseExactPosition)
        {
            // Recalculate collateral from position size
            if (PositionSize > 0 && Leverage > 0 && EntryPriceFiat.Value > 0)
                CollateralFiat = FiatValue.New(PositionSize * EntryPriceFiat.Value / Leverage);
        }
        else
        {
            // Recalculate position size from collateral
            RecalculatePositionSize();
        }

        _isAutoCalculating = false;
    }

    private void RecalculatePositionSize()
    {
        if (CollateralFiat.Value > 0 && Leverage > 0 && EntryPriceFiat.Value > 0)
            PositionSize = CollateralFiat.Value * Leverage / EntryPriceFiat.Value;
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
                await CreateNewAssetAsync();
            }
            else
            {
                // Edit existing asset
                await EditExistingAssetAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving asset");
            await MessageBoxHelper.ShowErrorAsync(language.Error, ex.Message, GetWindow!());
        }
    }

    private AssetFormSnapshot BuildSnapshot() => new(
        Name: Name,
        SelectedAssetType: SelectedAssetType,
        SelectedCurrency: SelectedCurrency,
        IncludeInNetWorth: IncludeInNetWorth,
        Visible: Visible,
        Symbol: Symbol,
        Quantity: Quantity,
        CurrentPriceFiat: CurrentPriceFiat,
        SelectedPriceSource: SelectedPriceSource,
        Address: Address,
        CurrentValueFiat: CurrentValueFiat,
        MonthlyRentalIncomeFiat: MonthlyRentalIncomeFiat,
        AcquisitionDate: AcquisitionDate,
        AcquisitionPriceFiat: AcquisitionPriceFiat,
        IsBitcoinUnderlyingAsset: IsBitcoinUnderlyingAsset,
        CollateralFiat: CollateralFiat,
        EntryPriceFiat: EntryPriceFiat,
        Leverage: Leverage,
        LiquidationPriceFiat: LiquidationPriceFiat,
        IsLong: IsLong,
        UseExactPosition: UseExactPosition,
        PositionSize: PositionSize,
        PlatformName: PlatformName,
        CollateralSats: CollateralSats,
        LoanAmountFiat: LoanAmountFiat,
        AprPercentage: AprPercentage,
        InitialLtvPercentage: InitialLtvPercentage,
        LiquidationLtvPercentage: LiquidationLtvPercentage,
        MarginCallLtvPercentage: MarginCallLtvPercentage,
        FeesFiat: FeesFiat,
        LoanStartDate: LoanStartDate,
        RepaymentDateOffset: RepaymentDateOffset,
        IsIndefiniteLoan: IsIndefiniteLoan,
        UseFixedTotalDebt: UseFixedTotalDebt,
        FixedTotalDebtFiat: FixedTotalDebtFiat,
        BorrowerOrPlatformName: BorrowerOrPlatformName,
        AmountLentFiat: AmountLentFiat,
        LendingAprPercentage: LendingAprPercentage,
        LendingStartDateOffset: LendingStartDateOffset,
        ExpectedRepaymentDateOffset: ExpectedRepaymentDateOffset,
        IsIndefiniteLending: IsIndefiniteLending);

    private async Task CreateNewAssetAsync()
    {
        var snapshot = BuildSnapshot();
        var envelope = await _assetFormBuilder!.BuildCreateCommandAsync(snapshot);

        switch (envelope)
        {
            case BasicAssetCommandEnvelope basic:
            {
                var result = await _commandDispatcher!.DispatchAsync(basic.Command);
                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                    return;
                }
                CloseDialog?.Invoke(new Response(true, result.Value!.AssetId));
                break;
            }
            case RealEstateAssetCommandEnvelope realEstate:
            {
                var result = await _commandDispatcher!.DispatchAsync(realEstate.Command);
                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                    return;
                }
                CloseDialog?.Invoke(new Response(true, result.Value!.AssetId));
                break;
            }
            case LeveragedPositionCommandEnvelope leveraged:
            {
                var result = await _commandDispatcher!.DispatchAsync(leveraged.Command);
                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                    return;
                }
                CloseDialog?.Invoke(new Response(true, result.Value!.AssetId));
                break;
            }
            case BtcLoanCommandEnvelope btcLoan:
            {
                var result = await _commandDispatcher!.DispatchAsync(btcLoan.Command);
                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                    return;
                }
                CloseDialog?.Invoke(new Response(true, result.Value!.AssetId));
                break;
            }
            case BtcLendingCommandEnvelope btcLending:
            {
                var result = await _commandDispatcher!.DispatchAsync(btcLending.Command);
                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                    return;
                }
                CloseDialog?.Invoke(new Response(true, result.Value!.AssetId));
                break;
            }
            default:
                throw new AssetFormBuildException();
        }
    }

    private async Task EditExistingAssetAsync()
    {
        var snapshot = BuildSnapshot();
        var details = await _assetFormBuilder!.BuildEditDetailsAsync(snapshot);

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
    private async Task UpdateLoanState()
    {
        if (_assetId is null)
            return;
        if (_modalFactory is null)
            return;

        var ownerWindow = GetWindow?.Invoke();
        if (ownerWindow is null)
            return;

        var modalFactory = _modalFactory;
        var modal = (UpdateLoanStateView)await modalFactory.CreateAsync(
            ApplicationModalNames.UpdateLoanState,
            ownerWindow,
            new UpdateLoanStateViewModel.Request { AssetId = _assetId });
        if (modal is null)
            return;

        await modal.ShowDialogSafeAsync<UpdateLoanStateViewModel.Response?>(ownerWindow);
    }

    [RelayCommand]
    private async Task OpenLoanStateHistory()
    {
        if (_assetId is null)
            return;
        if (_modalFactory is null)
            return;

        var ownerWindow = GetWindow?.Invoke();
        if (ownerWindow is null)
            return;

        var modalFactory = _modalFactory;
        var modal = (LoanStateHistoryView)await modalFactory.CreateAsync(
            ApplicationModalNames.LoanStateHistory,
            ownerWindow,
            new LoanStateHistoryViewModel.Request { AssetId = _assetId });
        if (modal is null)
            return;

        await modal.ShowDialogSafeAsync<LoanStateHistoryViewModel.Response?>(ownerWindow);
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
