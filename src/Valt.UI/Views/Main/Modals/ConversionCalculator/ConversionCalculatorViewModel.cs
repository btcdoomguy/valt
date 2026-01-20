using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StringMath;
using Valt.Core.Common;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Currency.Services;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.State;

namespace Valt.UI.Views.Main.Modals.ConversionCalculator;

public partial class ConversionCalculatorViewModel : ValtModalViewModel
{
    private readonly ICurrencyConversionService _conversionService;
    private readonly RatesState _ratesState;
    private readonly CurrencySettings _currencySettings;
    private readonly IConfigurationManager? _configurationManager;

    [ObservableProperty] private string _expression = string.Empty;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    private decimal? _calculatedValue;
    [ObservableProperty] private string _displayValue = string.Empty;
    [ObservableProperty] private CurrencyConversionItem? _selectedCurrency;
    [ObservableProperty] private bool _isResponseMode;

    public record Request(bool ResponseMode = false, string? DefaultCurrencyCode = null);
    public record Response(decimal? Result, string? SelectedCurrencyCode);

    public ObservableCollection<CurrencyConversionItem> Currencies { get; } = new();

    public string DecimalSeparator => CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ConversionCalculatorViewModel()
    {
        _conversionService = null!;
        _ratesState = null!;
        _currencySettings = null!;
        _configurationManager = null;
        InitializeCurrencies();
    }

    public ConversionCalculatorViewModel(
        ICurrencyConversionService conversionService,
        RatesState ratesState,
        CurrencySettings currencySettings,
        IConfigurationManager configurationManager)
    {
        _conversionService = conversionService;
        _ratesState = ratesState;
        _currencySettings = currencySettings;
        _configurationManager = configurationManager;
        InitializeCurrencies();
    }

    private void InitializeCurrencies()
    {
        // Add BTC first
        var btcItem = new CurrencyConversionItem
        {
            CurrencyCode = "BTC",
            CurrencySymbol = "₿",
            Decimals = 8,
            IsBitcoin = true
        };
        Currencies.Add(btcItem);

        // Add SATS after BTC
        var satsItem = new CurrencyConversionItem
        {
            CurrencyCode = "SATS",
            CurrencySymbol = "sats",
            Decimals = 0,
            IsBitcoin = false,
            IsSats = true
        };
        Currencies.Add(satsItem);

        // Get available fiat currencies from configuration
        var availableCurrencyCodes = _configurationManager?.GetAvailableFiatCurrencies() ?? new List<string>();
        var availableCurrencies = availableCurrencyCodes
            .Select(code => FiatCurrency.GetAll().FirstOrDefault(c => c.Code == code))
            .Where(c => c is not null)
            .Cast<FiatCurrency>()
            .ToList();

        // If no currencies configured, fall back to USD
        if (availableCurrencies.Count == 0)
        {
            availableCurrencies.Add(FiatCurrency.Usd);
        }

        // Get main fiat currency
        var mainFiatCode = _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;
        var mainFiat = availableCurrencies.FirstOrDefault(c => c.Code == mainFiatCode)
                       ?? availableCurrencies.First();

        // Add main fiat first
        var mainFiatItem = new CurrencyConversionItem
        {
            CurrencyCode = mainFiat.Code,
            CurrencySymbol = mainFiat.Symbol,
            Decimals = mainFiat.Decimals,
            IsBitcoin = false,
            IsSelected = true
        };
        Currencies.Add(mainFiatItem);
        SelectedCurrency = mainFiatItem;

        // Add remaining available currencies alphabetically
        var remainingCurrencies = availableCurrencies
            .Where(c => c.Code != mainFiatCode)
            .OrderBy(c => c.Code);

        foreach (var currency in remainingCurrencies)
        {
            Currencies.Add(new CurrencyConversionItem
            {
                CurrencyCode = currency.Code,
                CurrencySymbol = currency.Symbol,
                Decimals = currency.Decimals,
                IsBitcoin = false
            });
        }
    }

    partial void OnExpressionChanged(string value)
    {
        EvaluateExpression();
    }

    partial void OnSelectedCurrencyChanged(CurrencyConversionItem? value)
    {
        // Update selection state
        foreach (var currency in Currencies)
        {
            currency.IsSelected = currency == value;
        }

        // Recalculate all conversions
        UpdateConversions();
    }

    private void EvaluateExpression()
    {
        if (string.IsNullOrWhiteSpace(Expression))
        {
            CalculatedValue = null;
            DisplayValue = string.Empty;
            ClearConversions();
            return;
        }

        try
        {
            // Normalize the expression for evaluation
            var normalized = NormalizeExpression(Expression);
            var result = normalized.Eval();
            CalculatedValue = Convert.ToDecimal(result);

            // Format display value using current culture
            DisplayValue = CalculatedValue.Value.ToString("G", CultureInfo.CurrentUICulture);

            UpdateConversions();
        }
        catch (Exception)
        {
            CalculatedValue = null;
            DisplayValue = string.Empty;
            ClearConversions();
        }
    }

    private string NormalizeExpression(string expression)
    {
        // Replace culture-specific decimal separator with period
        var normalized = expression.Replace(DecimalSeparator, ".");

        // Replace display operators with actual operators
        normalized = normalized.Replace("×", "*").Replace("÷", "/");

        return normalized;
    }

    private void UpdateConversions()
    {
        if (CalculatedValue is null || SelectedCurrency is null || _conversionService is null || _ratesState is null)
        {
            ClearConversions();
            return;
        }

        var conversions = _conversionService.ConvertToAll(
            CalculatedValue.Value,
            SelectedCurrency.CurrencyCode,
            _ratesState.BitcoinPrice,
            _ratesState.FiatRates);

        foreach (var currency in Currencies)
        {
            if (conversions.TryGetValue(currency.CurrencyCode, out var value))
            {
                currency.ConvertedValue = value;
            }
            else
            {
                currency.ConvertedValue = 0;
            }
        }
    }

    private void ClearConversions()
    {
        foreach (var currency in Currencies)
        {
            currency.ConvertedValue = 0;
        }
    }

    [RelayCommand]
    private void AppendCharacter(string character)
    {
        Expression += character;
    }

    [RelayCommand]
    private void AppendDecimal()
    {
        Expression += DecimalSeparator;
    }

    [RelayCommand]
    private void Clear()
    {
        Expression = string.Empty;
    }

    [RelayCommand]
    private void Backspace()
    {
        if (!string.IsNullOrEmpty(Expression))
        {
            Expression = Expression[..^1];
        }
    }

    [RelayCommand]
    private void Equals()
    {
        // Just trigger evaluation (already happens on expression change)
        EvaluateExpression();
    }

    [RelayCommand]
    private void SelectCurrency(CurrencyConversionItem currency)
    {
        SelectedCurrency = currency;
    }

    [RelayCommand]
    private void Close()
    {
        if (IsResponseMode)
        {
            Cancel();
        }
        else
        {
            CloseWindow?.Invoke();
        }
    }

    public override Task OnBindParameterAsync()
    {
        if (Parameter is Request request)
        {
            IsResponseMode = request.ResponseMode;

            if (!string.IsNullOrEmpty(request.DefaultCurrencyCode))
            {
                var currency = Currencies.FirstOrDefault(c => c.CurrencyCode == request.DefaultCurrencyCode);
                if (currency != null)
                {
                    SelectedCurrency = currency;
                }
            }
        }

        return Task.CompletedTask;
    }

    private bool CanOk() => CalculatedValue is not null;

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok()
    {
        var cleanResult = CalculatedValue < 0 ? CalculatedValue * -1 : CalculatedValue;
        CloseDialog?.Invoke(new Response(cleanResult, SelectedCurrency?.CurrencyCode));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }
}
