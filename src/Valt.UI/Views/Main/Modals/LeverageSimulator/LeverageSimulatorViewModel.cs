using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Queries.GetAssets;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.State;

namespace Valt.UI.Views.Main.Modals.LeverageSimulator;

public partial class LeverageSimulatorViewModel : ValtModalViewModel
{
    private readonly CurrencySettings _currencySettings;
    private readonly RatesState _ratesState;
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly IConfigurationManager? _configurationManager;

    // Inputs
    [ObservableProperty] private bool _isLong = true;
    [ObservableProperty] private string _entryPriceText = string.Empty;
    [ObservableProperty] private string _collateralText = string.Empty;
    [ObservableProperty] private string _leverageText = string.Empty;
    [ObservableProperty] private string _liquidationPriceText = string.Empty;
    [ObservableProperty] private string _simulatedPriceText = string.Empty;

    // Currency dropdown
    [ObservableProperty] private FiatCurrency? _selectedCurrency;
    public ObservableCollection<FiatCurrency> AvailableCurrencies { get; } = new();

    // Position dropdown
    [ObservableProperty] private LeveragePositionItem? _selectedPosition;
    public ObservableCollection<LeveragePositionItem> AvailablePositions { get; } = new();

    // Results
    [ObservableProperty] private string _positionValueFiat = string.Empty;
    [ObservableProperty] private string _positionValueSats = string.Empty;
    [ObservableProperty] private string _pnLFiat = string.Empty;
    [ObservableProperty] private string _pnLPercentage = string.Empty;
    [ObservableProperty] private string _distanceToLiquidation = string.Empty;
    [ObservableProperty] private bool _isProfitable;
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private bool _showLiquidation;
    [ObservableProperty] private string _pnLColor = "#4CAF50";

    public string CurrencyCode => SelectedCurrency?.Code ?? _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public LeverageSimulatorViewModel()
    {
        _currencySettings = null!;
        _ratesState = null!;
        _queryDispatcher = null;
        _configurationManager = null;
    }

    public LeverageSimulatorViewModel(
        CurrencySettings currencySettings,
        RatesState ratesState,
        IQueryDispatcher queryDispatcher,
        IConfigurationManager configurationManager)
    {
        _currencySettings = currencySettings;
        _ratesState = ratesState;
        _queryDispatcher = queryDispatcher;
        _configurationManager = configurationManager;
    }

    public override async Task OnBindParameterAsync()
    {
        LoadAvailableCurrencies();
        await LoadPositionsAsync();
        SetDefaultSimulatedPrice();
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

    private async Task LoadPositionsAsync()
    {
        if (_queryDispatcher is null) return;

        try
        {
            var assets = await _queryDispatcher.DispatchAsync(new GetAssetsQuery());
            var leveraged = assets
                .Where(a => a.AssetTypeId == (int)AssetTypes.LeveragedPosition)
                .ToList();

            AvailablePositions.Clear();
            AvailablePositions.Add(new LeveragePositionItem
            {
                DisplayName = Lang.language.LeverageSimulator_NewSimulation,
                IsNewSimulation = true
            });

            foreach (var asset in leveraged)
            {
                AvailablePositions.Add(new LeveragePositionItem
                {
                    DisplayName = asset.Name,
                    AssetId = asset.Id,
                    IsLong = asset.IsLong ?? true,
                    EntryPrice = asset.EntryPrice ?? 0,
                    Collateral = asset.Collateral ?? 0,
                    Leverage = asset.Leverage ?? 1,
                    LiquidationPrice = asset.LiquidationPrice ?? 0,
                    CurrencyCode = asset.CurrencyCode,
                    IsNewSimulation = false
                });
            }

            SelectedPosition = AvailablePositions.First();
        }
        catch
        {
            // If assets can't be loaded, just show new simulation option
            AvailablePositions.Clear();
            AvailablePositions.Add(new LeveragePositionItem
            {
                DisplayName = Lang.language.LeverageSimulator_NewSimulation,
                IsNewSimulation = true
            });
            SelectedPosition = AvailablePositions.First();
        }
    }

    private void SetDefaultSimulatedPrice()
    {
        if (_ratesState?.BitcoinPrice is null) return;

        var btcPriceUsd = _ratesState.BitcoinPrice.Value;
        var currencyCode = CurrencyCode;

        decimal btcPriceInCurrency;
        if (currencyCode == FiatCurrency.Usd.Code)
        {
            btcPriceInCurrency = btcPriceUsd;
        }
        else if (_ratesState.FiatRates is not null && _ratesState.FiatRates.TryGetValue(currencyCode, out var fiatRate))
        {
            btcPriceInCurrency = btcPriceUsd * fiatRate;
        }
        else
        {
            btcPriceInCurrency = btcPriceUsd;
        }

        SimulatedPriceText = btcPriceInCurrency.ToString("F2", CultureInfo.CurrentUICulture);
    }

    partial void OnSelectedPositionChanged(LeveragePositionItem? value)
    {
        if (value is null || value.IsNewSimulation) return;

        // Set currency from position before other fields (triggers recalculate)
        if (!string.IsNullOrEmpty(value.CurrencyCode))
        {
            var currency = AvailableCurrencies.FirstOrDefault(c => c.Code == value.CurrencyCode);
            if (currency is not null)
                SelectedCurrency = currency;
        }

        IsLong = value.IsLong;
        EntryPriceText = value.EntryPrice.ToString("F2", CultureInfo.CurrentUICulture);
        CollateralText = value.Collateral.ToString("F2", CultureInfo.CurrentUICulture);
        LeverageText = value.Leverage.ToString("F1", CultureInfo.CurrentUICulture);
        LiquidationPriceText = value.LiquidationPrice.ToString("F2", CultureInfo.CurrentUICulture);
    }

    partial void OnSelectedCurrencyChanged(FiatCurrency? value)
    {
        OnPropertyChanged(nameof(CurrencyCode));
        SetDefaultSimulatedPrice();
    }

    partial void OnIsLongChanged(bool value) => Recalculate();
    partial void OnEntryPriceTextChanged(string value) => Recalculate();
    partial void OnCollateralTextChanged(string value) => Recalculate();
    partial void OnLeverageTextChanged(string value) => Recalculate();
    partial void OnLiquidationPriceTextChanged(string value) => Recalculate();
    partial void OnSimulatedPriceTextChanged(string value) => Recalculate();

    private void Recalculate()
    {
        if (!TryParseDecimal(EntryPriceText, out var entryPrice) || entryPrice <= 0 ||
            !TryParseDecimal(CollateralText, out var collateral) || collateral <= 0 ||
            !TryParseDecimal(LeverageText, out var leverage) || leverage < 1 ||
            !TryParseDecimal(SimulatedPriceText, out var simulatedPrice) || simulatedPrice <= 0)
        {
            ClearResults();
            return;
        }

        TryParseDecimal(LiquidationPriceText, out var liquidationPrice);
        if (liquidationPrice < 0) liquidationPrice = 0;

        try
        {
            var details = new LeveragedPositionDetails(
                collateral, entryPrice, leverage, liquidationPrice,
                simulatedPrice, CurrencyCode, symbol: null,
                priceSource: AssetPriceSource.Manual, isLong: IsLong);

            var positionValue = details.CalculateCurrentValue(simulatedPrice);
            var pnl = details.CalculatePnL(simulatedPrice);
            var pnlPct = details.CalculatePnLPercentage(simulatedPrice);
            var distToLiq = details.CalculateDistanceToLiquidation(simulatedPrice);

            // Convert position value to sats: (fiat_value / simulatedPrice) * 100_000_000
            var posValueSats = simulatedPrice > 0
                ? positionValue / simulatedPrice * 100_000_000m
                : 0m;

            var culture = CultureInfo.CurrentUICulture;
            PositionValueFiat = positionValue.ToString("N2", culture);
            PositionValueSats = posValueSats.ToString("N0", culture);
            PnLFiat = (pnl >= 0 ? "+" : "") + pnl.ToString("N2", culture);
            PnLPercentage = (pnlPct >= 0 ? "+" : "") + pnlPct.ToString("N2", culture) + "%";
            DistanceToLiquidation = distToLiq.ToString("N2", culture) + "%";
            IsProfitable = pnl >= 0;
            PnLColor = pnl >= 0 ? "#4CAF50" : "#F44336";
            ShowLiquidation = liquidationPrice > 0;
            HasResults = true;
        }
        catch
        {
            ClearResults();
        }
    }

    private void ClearResults()
    {
        PositionValueFiat = string.Empty;
        PositionValueSats = string.Empty;
        PnLFiat = string.Empty;
        PnLPercentage = string.Empty;
        DistanceToLiquidation = string.Empty;
        IsProfitable = false;
        ShowLiquidation = false;
        HasResults = false;
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return false;
        }

        // Try current culture first, then invariant
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentUICulture, out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    [RelayCommand]
    private void SetLong()
    {
        IsLong = true;
    }

    [RelayCommand]
    private void SetShort()
    {
        IsLong = false;
    }

    [RelayCommand]
    private void Close()
    {
        CloseWindow?.Invoke();
    }
}

public class LeveragePositionItem
{
    public string DisplayName { get; init; } = string.Empty;
    public string? AssetId { get; init; }
    public bool IsNewSimulation { get; init; }
    public bool IsLong { get; init; }
    public decimal EntryPrice { get; init; }
    public decimal Collateral { get; init; }
    public decimal Leverage { get; init; }
    public decimal LiquidationPrice { get; init; }
    public string? CurrencyCode { get; init; }

    public override string ToString() => DisplayName;
}
