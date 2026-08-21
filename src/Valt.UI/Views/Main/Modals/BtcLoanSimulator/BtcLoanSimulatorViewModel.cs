using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Assets.Simulation;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.State;

namespace Valt.UI.Views.Main.Modals.BtcLoanSimulator;

public partial class BtcLoanSimulatorViewModel : ValtModalViewModel
{
    private readonly CurrencySettings _currencySettings;
    private readonly RatesState _ratesState;
    private readonly IConfigurationManager? _configurationManager;
    private readonly IClock? _clock;

    // Inputs
    [ObservableProperty] private BtcValue _collateralBtcValue = BtcValue.Empty;
    [ObservableProperty] private FiatValue _amountTakenFiatValue = FiatValue.Empty;
    [ObservableProperty] private FiatValue _feesFiatValue = FiatValue.Empty;
    [ObservableProperty] private string _liquidationLtvText = string.Empty;
    [ObservableProperty] private string _aprText = string.Empty;
    [ObservableProperty] private DateTime? _startDate;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] private bool _isSimple = false;

    // Currency
    [ObservableProperty] private FiatCurrency? _selectedCurrency;
    public ObservableCollection<FiatCurrency> AvailableCurrencies { get; } = new();

    // Results
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private string _totalRepayFiat = string.Empty;
    [ObservableProperty] private string _totalRepaySats = string.Empty;
    [ObservableProperty] private string _principalFiat = string.Empty;
    [ObservableProperty] private string _principalSats = string.Empty;
    [ObservableProperty] private string _interestFiat = string.Empty;
    [ObservableProperty] private string _interestSats = string.Empty;
    [ObservableProperty] private string _feesFiat = string.Empty;
    [ObservableProperty] private string _feesSats = string.Empty;
    [ObservableProperty] private string _liquidationPriceFiat = string.Empty;
    [ObservableProperty] private string _effectiveAprText = string.Empty;
    [ObservableProperty] private string _distanceToLiquidation = string.Empty;
    [ObservableProperty] private string _distanceToLiquidationColor = "#4CAF50";
    [ObservableProperty] private string _conversionBasis = string.Empty;
    [ObservableProperty] private bool _isBtcPriceAvailable;

    public string CurrencyCode => SelectedCurrency?.Code ?? _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;
    public string CurrencySymbol => SelectedCurrency?.Symbol ?? FiatCurrency.Usd.Symbol;
    public bool SymbolOnRight => SelectedCurrency?.SymbolOnRight ?? FiatCurrency.Usd.SymbolOnRight;

    /// <summary>
    /// Design-time constructor.
    /// </summary>
    public BtcLoanSimulatorViewModel()
    {
        _currencySettings = null!;
        _ratesState = null!;
    }

    public BtcLoanSimulatorViewModel(
        CurrencySettings currencySettings,
        RatesState ratesState,
        IConfigurationManager configurationManager,
        IClock clock)
    {
        _currencySettings = currencySettings;
        _ratesState = ratesState;
        _configurationManager = configurationManager;
        _clock = clock;

        LoadAvailableCurrencies();
        SetDefaultDates();
    }

    private void LoadAvailableCurrencies()
    {
        var currencyCodes = _configurationManager?.GetAvailableFiatCurrencies()
            ?? new List<string> { FiatCurrency.Usd.Code };

        AvailableCurrencies.Clear();
        foreach (var code in currencyCodes)
        {
            try
            {
                AvailableCurrencies.Add(FiatCurrency.GetFromCode(code));
            }
            catch
            {
                // Skip invalid currency codes
            }
        }

        if (AvailableCurrencies.Count == 0)
            AvailableCurrencies.Add(FiatCurrency.Usd);

        var mainCode = _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;
        SelectedCurrency = AvailableCurrencies.FirstOrDefault(c => c.Code == mainCode)
            ?? AvailableCurrencies.First();
    }

    private void SetDefaultDates()
    {
        var today = _clock?.GetCurrentLocalDate() ?? DateOnly.FromDateTime(DateTime.Today);
        StartDate = today.ToDateTime(TimeOnly.MinValue);
        EndDate = today.AddDays(30).ToDateTime(TimeOnly.MinValue);
        FeesFiatValue = FiatValue.New(0m);
    }

    partial void OnCollateralBtcValueChanged(BtcValue value) => Recalculate();
    partial void OnAmountTakenFiatValueChanged(FiatValue value) => Recalculate();
    partial void OnFeesFiatValueChanged(FiatValue value) => Recalculate();
    partial void OnLiquidationLtvTextChanged(string value) => Recalculate();
    partial void OnAprTextChanged(string value) => Recalculate();
    partial void OnStartDateChanged(DateTime? value) => Recalculate();
    partial void OnEndDateChanged(DateTime? value) => Recalculate();
    partial void OnIsSimpleChanged(bool value) => Recalculate();

    partial void OnSelectedCurrencyChanged(FiatCurrency? value)
    {
        OnPropertyChanged(nameof(CurrencyCode));
        OnPropertyChanged(nameof(CurrencySymbol));
        OnPropertyChanged(nameof(SymbolOnRight));
        Recalculate();
    }

    [RelayCommand]
    private void SetSimple()
    {
        IsSimple = true;
    }

    [RelayCommand]
    private void SetCompound()
    {
        IsSimple = false;
    }

    private void Recalculate()
    {
        if (!TryBuildInput(out var input))
        {
            ClearResults();
            return;
        }

        try
        {
            var result = BtcLoanSimulationCalculator.Calculate(input);
            FormatResults(result);
        }
        catch
        {
            ClearResults();
        }
    }

    private bool TryBuildInput(out BtcLoanSimulationInput input)
    {
        input = null!;

        var collateral = CollateralBtcValue;
        if (collateral.Sats <= 0)
            return false;

        var principal = AmountTakenFiatValue.Value;
        if (principal <= 0)
            return false;

        if (!TryParseDecimal(LiquidationLtvText, out var liquidationLtv)
            || liquidationLtv <= 0
            || liquidationLtv > 100)
        {
            return false;
        }

        if (!TryParseDecimal(AprText, out var apr) || apr < 0)
            return false;

        var fees = FeesFiatValue.Value;
        if (fees < 0)
            return false;

        if (!StartDate.HasValue || !EndDate.HasValue)
            return false;

        var startDate = DateOnly.FromDateTime(StartDate.Value);
        var endDate = DateOnly.FromDateTime(EndDate.Value);
        if (endDate <= startDate)
            return false;

        var interestMode = IsSimple ? BtcLoanInterestMode.Simple : BtcLoanInterestMode.Compound;

        input = new BtcLoanSimulationInput
        {
            CollateralSats = collateral.Sats,
            PrincipalAmount = principal,
            CurrencyCode = CurrencyCode,
            Apr = apr / 100m,
            LiquidationLtv = liquidationLtv,
            Fees = fees,
            StartDate = startDate,
            EndDate = endDate,
            InterestMode = interestMode
        };

        return true;
    }

    private void FormatResults(BtcLoanSimulationResult result)
    {
        var currencyCode = CurrencyCode;
        var btcPriceInCurrency = GetBtcPriceInCurrency();
        IsBtcPriceAvailable = btcPriceInCurrency.HasValue && btcPriceInCurrency.Value > 0m;

        TotalRepayFiat = CurrencyDisplay.FormatFiat(result.TotalRepay, currencyCode);
        PrincipalFiat = CurrencyDisplay.FormatFiat(result.Principal, currencyCode);
        InterestFiat = CurrencyDisplay.FormatFiat(result.Interest, currencyCode);
        FeesFiat = CurrencyDisplay.FormatFiat(result.Fees, currencyCode);
        LiquidationPriceFiat = CurrencyDisplay.FormatFiat(result.LiquidationPrice, currencyCode);
        EffectiveAprText = (result.EffectiveApr * 100m).ToString("N2", CultureInfo.CurrentUICulture) + "%";

        if (IsBtcPriceAvailable)
        {
            var price = btcPriceInCurrency!.Value;
            TotalRepaySats = FormatSats(result.TotalRepay, price);
            PrincipalSats = FormatSats(result.Principal, price);
            InterestSats = FormatSats(result.Interest, price);
            FeesSats = FormatSats(result.Fees, price);

            if (result.LiquidationPrice > 0)
            {
                var pct = (price - result.LiquidationPrice) / result.LiquidationPrice;
                var sign = pct >= 0 ? "+" : "";
                DistanceToLiquidation = $"{sign}{(pct * 100m).ToString("N2", CultureInfo.CurrentUICulture)}%";
                DistanceToLiquidationColor = price <= result.LiquidationPrice ? "#F44336" : "#4CAF50";
            }
            else
            {
                DistanceToLiquidation = string.Empty;
                DistanceToLiquidationColor = "#4CAF50";
            }

            ConversionBasis = $"1 BTC = {price.ToString("N2", CultureInfo.CurrentUICulture)} {currencyCode}";
        }
        else
        {
            TotalRepaySats = string.Empty;
            PrincipalSats = string.Empty;
            InterestSats = string.Empty;
            FeesSats = string.Empty;
            DistanceToLiquidation = string.Empty;
            DistanceToLiquidationColor = "#4CAF50";
            ConversionBasis = "Current BTC price unavailable — sats values hidden";
        }

        HasResults = true;
    }

    private string FormatSats(decimal fiatAmount, decimal btcPriceInCurrency)
    {
        var sats = (long)Math.Round(fiatAmount / btcPriceInCurrency * 100_000_000m);
        return CurrencyDisplay.FormatSatsAsNumber(sats);
    }

    private decimal? GetBtcPriceInCurrency()
    {
        if (_ratesState?.BitcoinPrice is null || _ratesState.BitcoinPrice <= 0m)
            return null;

        var btcPriceUsd = _ratesState.BitcoinPrice.Value;
        var currencyCode = CurrencyCode;

        if (currencyCode == FiatCurrency.Usd.Code)
            return btcPriceUsd;

        if (_ratesState.FiatRates is not null
            && _ratesState.FiatRates.TryGetValue(currencyCode, out var fiatRate)
            && fiatRate > 0m)
        {
            return btcPriceUsd * fiatRate;
        }

        return null;
    }

    private void ClearResults()
    {
        HasResults = false;
        TotalRepayFiat = string.Empty;
        TotalRepaySats = string.Empty;
        PrincipalFiat = string.Empty;
        PrincipalSats = string.Empty;
        InterestFiat = string.Empty;
        InterestSats = string.Empty;
        FeesFiat = string.Empty;
        FeesSats = string.Empty;
        LiquidationPriceFiat = string.Empty;
        EffectiveAprText = string.Empty;
        DistanceToLiquidation = string.Empty;
        DistanceToLiquidationColor = "#4CAF50";
        ConversionBasis = string.Empty;
        IsBtcPriceAvailable = false;
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return false;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentUICulture, out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
