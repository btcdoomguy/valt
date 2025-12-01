using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.Infra.DataAccess;
using Valt.Infra.Settings;
using Valt.UI.State.Events;

namespace Valt.UI.State;

/// <summary>
/// State object for the live prices
/// </summary>
public partial class LiveRateState : ObservableObject, IRecipient<RatesUpdated>, IDisposable
{
    private decimal? _lastFiatClosingPrice;
    private DateOnly? _lastFiatClosingDate;
    private string? _lastFiatCurrency;

    private readonly CurrencySettings _currencySettings;
    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalHistoricalPriceProvider _localHistoricalPriceProvider;
    private readonly RatesState _ratesState;
    private readonly ILogger<LiveRateState> _logger;

    [ObservableProperty] private decimal _bitcoinPrice;
    [ObservableProperty] private decimal? _previousBitcoinPrice;

    [ObservableProperty] private decimal _usdPrice;
    [ObservableProperty] private decimal _previousUsdPrice;

    [ObservableProperty] private decimal _fiatBtcPrice;
    [ObservableProperty] private decimal _previousFiatBtcPrice;

    [ObservableProperty] private bool _isOffline;

    public LiveRateState(CurrencySettings currencySettings,
        ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        ILocalHistoricalPriceProvider localHistoricalPriceProvider,
        RatesState ratesState,
        ILogger<LiveRateState> logger)
    {
        _currencySettings = currencySettings;
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _localHistoricalPriceProvider = localHistoricalPriceProvider;
        _ratesState = ratesState;
        _logger = logger;

        WeakReferenceMessenger.Default.Register<RatesUpdated>(this);
    }

    public void Receive(RatesUpdated message)
    {
        try
        {
            IsOffline = !_ratesState.IsUpToDate;

            var usdPrice = _ratesState.BitcoinPrice;
            var previousUsdPrice = _ratesState.PreviousBitcoinPrice;

            if (usdPrice is null)
                return;

            if (usdPrice != BitcoinPrice)
                BitcoinPrice = usdPrice.Value;
            if (previousUsdPrice != PreviousBitcoinPrice)
                PreviousBitcoinPrice = previousUsdPrice;

            if (_ratesState.FiatRates is null)
                return;
            
            if (!_ratesState.FiatRates.TryGetValue(_currencySettings.MainFiatCurrency, out var mainFiatPrice))
            {
                UsdPrice = 0;
                PreviousUsdPrice = 0;
                return;
            }

            if (mainFiatPrice != UsdPrice)
                UsdPrice = mainFiatPrice;

            if (!_localDatabase.HasDatabaseOpen)
            {
                PreviousUsdPrice = 0;
                return;
            }

            var utcNow = DateTime.UtcNow;
            var localDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Local);
            var yesterday = localDate.AddDays(-1);

            var refreshLastPrice = _lastFiatClosingDate is null;

            if (_lastFiatClosingDate is not null && _lastFiatClosingDate < DateOnly.FromDateTime(yesterday))
            {
                refreshLastPrice = true;
            }

            if (_lastFiatCurrency != _currencySettings.MainFiatCurrency)
            {
                refreshLastPrice = true;
            }

            if (refreshLastPrice)
            {
                RefreshLastPrice();
            }

            if (_lastFiatClosingPrice is not null && PreviousUsdPrice != _lastFiatClosingPrice.Value)
            {
                PreviousUsdPrice = _lastFiatClosingPrice.Value;
            }

            if (_currencySettings.MainFiatCurrency != FiatCurrency.Usd.Code)
            {
                FiatBtcPrice = UsdPrice * BitcoinPrice;
                if (PreviousBitcoinPrice is not null)
                    PreviousFiatBtcPrice = PreviousUsdPrice * PreviousBitcoinPrice.GetValueOrDefault();
            }

            WeakReferenceMessenger.Default.Send(new LivePriceUpdated());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LiveRateState] Error during execution");
        }
    }

    private void RefreshLastPrice()
    {
        try
        {
            var lastDateParsed = _priceDatabase
                .GetFiatData()
                .Find(x => x.Currency == _currencySettings.MainFiatCurrency)
                .Max(x => x.Date)
                .Date;
            var previousPrice =
                _localHistoricalPriceProvider.GetFiatRateAtAsync(DateOnly.FromDateTime(lastDateParsed),
                    FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency)).Result;

            _lastFiatClosingDate = DateOnly.FromDateTime(lastDateParsed);
            _lastFiatClosingPrice = previousPrice;
            _lastFiatCurrency = _currencySettings.MainFiatCurrency;
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e, "Error getting last fiat closing price");
            _lastFiatClosingDate = null;
            _lastFiatClosingPrice = null;
            _lastFiatCurrency = null;
        }
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<RatesUpdated>(this);
    }
}