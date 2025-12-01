using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;
using Valt.UI.State.Events;

namespace Valt.UI.State;

public partial class RatesState : ObservableObject, IRecipient<LivePriceUpdateMessage>, IDisposable
{
    [ObservableProperty]
    private decimal? _bitcoinPrice;
    [ObservableProperty]
    private decimal? _previousBitcoinPrice;
    [ObservableProperty]
    private IReadOnlyDictionary<string, decimal>? _fiatRates;
    
    [ObservableProperty] private bool _isUpToDate;
    
    public RatesState()
    {
        WeakReferenceMessenger.Default.Register<LivePriceUpdateMessage>(this);
    }

    public void Receive(LivePriceUpdateMessage message)
    {
        BitcoinPrice = message.Btc.Items.SingleOrDefault(x => x.CurrencyCode == FiatCurrency.Usd.Code)!.Price;
        PreviousBitcoinPrice = message.Btc.Items.SingleOrDefault(x => x.CurrencyCode == FiatCurrency.Usd.Code)!.PreviousPrice;
        FiatRates = message.Fiat.Items.ToDictionary(x => x.CurrencyCode, x => x.Price);
        IsUpToDate = message.IsUpToDate;

        WeakReferenceMessenger.Default.Send(new RatesUpdated());
    }
    
    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<LivePriceUpdateMessage>(this);
    }
}