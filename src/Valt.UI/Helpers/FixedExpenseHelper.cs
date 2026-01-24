using System;
using Valt.Core.Common;
using Valt.Infra.Settings;
using Valt.UI.State;

namespace Valt.UI.Helpers;

public class FixedExpenseHelper
{
    private readonly RatesState _ratesState;
    private readonly CurrencySettings _currencySettings;

    public FixedExpenseHelper(RatesState ratesState, CurrencySettings currencySettings)
    {
        _ratesState = ratesState;
        _currencySettings = currencySettings;
    }

    public (decimal minValue, decimal maxValue) CalculateFixedExpenseRange(decimal? fixedAmount,
        decimal? rangedAmountMin, decimal? rangedAmountMax, string displayCurrency)
    {
        decimal fixedAmountMin = 0;
        decimal fixedAmountMax = 0;

        if (fixedAmount is not null)
        {
            fixedAmountMin = fixedAmountMax = fixedAmount.Value;
        }
        else
        {
            fixedAmountMin = rangedAmountMin!.Value;
            fixedAmountMax = rangedAmountMax!.Value;
        }

        if (displayCurrency == FiatCurrency.Usd.Code &&
            _currencySettings.MainFiatCurrency != FiatCurrency.Usd.Code)
        {
            var mainCurrency = _currencySettings.MainFiatCurrency!;
            if (_ratesState.FiatRates is null || !_ratesState.FiatRates.ContainsKey(mainCurrency))
                throw new ApplicationException("Currency not found");

            fixedAmountMin = _ratesState.FiatRates[mainCurrency] *
                             fixedAmountMin;
            fixedAmountMax = _ratesState.FiatRates[mainCurrency] *
                             fixedAmountMax;
        }
        else if (displayCurrency != _currencySettings.MainFiatCurrency)
        {
            if (_ratesState.FiatRates is null || !_ratesState.FiatRates.ContainsKey(displayCurrency))
                throw new ApplicationException("Currency not found");

            var mainCurrency = _currencySettings.MainFiatCurrency ?? FiatCurrency.Usd.Code;
            if (!_ratesState.FiatRates.ContainsKey(mainCurrency))
                throw new ApplicationException("Currency not found");

            //convert to usd then back
            var fixedAmountMinConvertedToUsd =
                fixedAmountMin / _ratesState.FiatRates[displayCurrency];
            fixedAmountMin = _ratesState.FiatRates[mainCurrency] *
                             fixedAmountMinConvertedToUsd;

            var fixedAmountMaxConvertedToUsd =
                fixedAmountMax / _ratesState.FiatRates[displayCurrency];
            fixedAmountMax = _ratesState.FiatRates[mainCurrency] *
                             fixedAmountMaxConvertedToUsd;
        }

        return (fixedAmountMin, fixedAmountMax);
    }
}