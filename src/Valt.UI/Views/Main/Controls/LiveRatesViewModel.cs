using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Settings;
using Valt.UI.State;
using Valt.UI.State.Events;

namespace Valt.UI.Views.Main.Controls;

public partial class LiveRatesViewModel : ObservableObject, IDisposable
{
    private readonly CurrencySettings _currencySettings;
    private readonly LiveRateState _liveRateState;
    private readonly ILocalDatabase _localDatabase;
    private readonly BackgroundJobManager _backgroundJobManager;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BtcUsdText))]
    [NotifyPropertyChangedFor(nameof(BtcUsdVariationText))]
    [NotifyPropertyChangedFor(nameof(BtcUsdVariationBrush))]
    private decimal _btcUsdPrice;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(BtcUsdVariationText))]
    [NotifyPropertyChangedFor(nameof(BtcUsdVariationBrush))]
    private decimal? _previousBtcUsdPrice;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(UsdText))]
    [NotifyPropertyChangedFor(nameof(UsdVariationText))]
    [NotifyPropertyChangedFor(nameof(UsdVariationBrush))]
    private decimal _usdPrice;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(UsdVariationText))]
    [NotifyPropertyChangedFor(nameof(UsdVariationBrush))]
    private decimal? _previousUsdPrice;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BtcFiatText))]
    [NotifyPropertyChangedFor(nameof(BtcFiatVariationText))]
    [NotifyPropertyChangedFor(nameof(BtcFiatVariationBrush))]
    private decimal _btcFiatPrice;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(BtcFiatVariationText))]
    [NotifyPropertyChangedFor(nameof(BtcFiatVariationBrush))]
    private decimal? _previousBtcFiatPrice;
    [ObservableProperty] private bool _hasDatabaseOpen;
    public string BtcUsdText => $"{CurrencyDisplay.FormatFiat(BtcUsdPrice, FiatCurrency.Usd.Code)}";
    public bool ShowUsdFiatLabels => _currencySettings.MainFiatCurrency != FiatCurrency.Usd.Code;

    public string BtcUsdVariationText
    {
        get
        {
            if (PreviousBtcUsdPrice is null)
                return string.Empty;

            var variation = Math.Round((decimal)((BtcUsdPrice - PreviousBtcUsdPrice.Value) / PreviousBtcUsdPrice.Value * 100), 2);
            
            return (variation > 0 ? "+" : "") + Math.Round((decimal)((BtcUsdPrice - PreviousBtcUsdPrice.Value) / PreviousBtcUsdPrice.Value * 100), 2).ToString(CultureInfo.CurrentCulture) + "%";
        }
    }

    public SolidColorBrush BtcUsdVariationBrush
    {
        get
        {
            if (PreviousBtcUsdPrice is null)
                return new SolidColorBrush(Colors.White);
            
            var variation = Math.Round((decimal)((BtcUsdPrice - PreviousBtcUsdPrice.Value) / PreviousBtcUsdPrice.Value * 100), 2);
            
            return variation >= 0 ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Red);
        }
    }
    
    public string BtcFiatPairText => $"BTC/{_currencySettings.MainFiatCurrency}";
    public string BtcFiatText => CurrencyDisplay.FormatFiat(BtcFiatPrice, FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency).Code);
    
    public string BtcFiatVariationText
    {
        get
        {
            if (PreviousBtcFiatPrice is null)
                return string.Empty;

            var variation = Math.Round((decimal)((BtcFiatPrice - PreviousBtcFiatPrice.Value) / PreviousBtcFiatPrice.Value * 100), 2);
            
            return (variation > 0 ? "+" : "") + Math.Round((decimal)((BtcFiatPrice - PreviousBtcFiatPrice.Value) / PreviousBtcFiatPrice.Value * 100), 2).ToString(CultureInfo.CurrentCulture) + "%";
        }
    }
    
    public SolidColorBrush BtcFiatVariationBrush
    {
        get
        {
            if (PreviousBtcFiatPrice is null)
                return new SolidColorBrush(Colors.White);
            
            var variation = Math.Round((decimal)((BtcFiatPrice - PreviousBtcFiatPrice.Value) / PreviousBtcFiatPrice.Value * 100), 2);
            
            return variation >= 0 ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Red);
        }
    }
    
    public string UsdWithPairText => $"USD/{_currencySettings.MainFiatCurrency}";
    public string UsdText => $"{CurrencyDisplay.FormatFiat(UsdPrice, _currencySettings.MainFiatCurrency)}";
    
    public string UsdVariationText
    {
        get
        {
            if (PreviousUsdPrice is null || PreviousUsdPrice.Value == 0m)
                return string.Empty;

            var variation = Math.Round((decimal)((UsdPrice - PreviousUsdPrice.Value) / PreviousUsdPrice.Value * 100), 2);
            
            return (variation > 0 ? "+" : "") + Math.Round((decimal)((UsdPrice - PreviousUsdPrice.Value) / PreviousUsdPrice.Value * 100), 2).ToString(CultureInfo.CurrentCulture) + "%";
        }
    }

    public SolidColorBrush UsdVariationBrush
    {
        get
        {
            if (PreviousUsdPrice is null || PreviousUsdPrice.Value == 0m)
                return new SolidColorBrush(Colors.White);
            
            var variation = Math.Round((decimal)((UsdPrice - PreviousUsdPrice.Value) / PreviousUsdPrice.Value * 100), 2);
            
            return variation >= 0 ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Red);
        }
    }

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public LiveRatesViewModel()
    {
        _currencySettings = new CurrencySettings(_localDatabase);
        
        HasDatabaseOpen = true;
        _currencySettings.MainFiatCurrency = FiatCurrency.Brl.Code;
    }
    
    public LiveRatesViewModel(CurrencySettings currencySettings,
        LiveRateState liveRateState,
        ILocalDatabase localDatabase,
        BackgroundJobManager backgroundJobManager)
    {
        _currencySettings = currencySettings;
        _liveRateState = liveRateState;
        _localDatabase = localDatabase;
        _backgroundJobManager = backgroundJobManager;

        _localDatabase.PropertyChanged += LocalDatabaseOnPropertyChanged;
        
        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            if (message.Value == nameof(CurrencySettings.MainFiatCurrency))
            {
                OnPropertyChanged(nameof(UsdWithPairText));
                OnPropertyChanged(nameof(UsdVariationText));
                OnPropertyChanged(nameof(BtcFiatPairText));
                OnPropertyChanged(nameof(BtcFiatText));
                OnPropertyChanged(nameof(BtcFiatVariationText));
                OnPropertyChanged(nameof(ShowUsdFiatLabels));
                
                _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.LivePricesUpdater);
            }
        });
        
        WeakReferenceMessenger.Default.Register<LivePriceUpdated>(this, (recipient, message) =>
        {
            BtcUsdPrice = _liveRateState.BitcoinPrice;
            if (_liveRateState.PreviousBitcoinPrice is not null)
                PreviousBtcUsdPrice = _liveRateState.PreviousBitcoinPrice.GetValueOrDefault();

            UsdPrice = _liveRateState.UsdPrice;
            PreviousUsdPrice = _liveRateState.PreviousUsdPrice;

            if (_currencySettings.MainFiatCurrency != FiatCurrency.Usd.Code)
            {
                BtcFiatPrice = _liveRateState.FiatBtcPrice;
                PreviousBtcFiatPrice = _liveRateState.PreviousFiatBtcPrice;
            }
        });
    }

    private void LocalDatabaseOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HasDatabaseOpen = _localDatabase!.HasDatabaseOpen;
        Dispatcher.UIThread.Post(() => { BtcUsdPrice = _liveRateState.BitcoinPrice; });
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<SettingsChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<LivePriceUpdated>(this);
    }
}