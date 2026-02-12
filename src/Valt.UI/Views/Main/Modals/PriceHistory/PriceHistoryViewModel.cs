using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Base;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.PriceHistory;

public partial class PriceHistoryViewModel : ValtModalViewModel, IDisposable
{
    private const int PageSize = 100;

    private readonly IPriceDatabase _priceDatabase;
    private readonly IConfigurationManager _configurationManager;

    private List<(DateTime Date, decimal Price)> _allData = new();

    public ObservableCollection<string> AvailableCurrencies { get; } = new();
    public ObservableCollection<PriceHistoryItem> PageItems { get; } = new();
    public PriceHistoryChartData ChartData { get; } = new();

    [ObservableProperty] private string? _selectedCurrency;
    [ObservableProperty] private int _currentPage;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _priceColumnHeader = language.PriceHistory_Column_Price;
    [ObservableProperty] private string _pageInfo = string.Empty;
    [ObservableProperty] private string _totalRecordsText = string.Empty;
    [ObservableProperty] private bool _hasData;

    public bool CanGoBack => CurrentPage > 1;
    public bool CanGoForward => CurrentPage < TotalPages;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public PriceHistoryViewModel()
    {
        _priceDatabase = null!;
        _configurationManager = null!;
    }

    public PriceHistoryViewModel(IPriceDatabase priceDatabase, IConfigurationManager configurationManager)
    {
        _priceDatabase = priceDatabase;
        _configurationManager = configurationManager;
    }

    public void Initialize()
    {
        AvailableCurrencies.Clear();
        AvailableCurrencies.Add("BTC");

        var fiatCurrencies = _configurationManager.GetAvailableFiatCurrencies()
            .OrderBy(c => c)
            .ToList();

        foreach (var currency in fiatCurrencies)
            AvailableCurrencies.Add(currency);

        SelectedCurrency = "BTC";
    }

    partial void OnSelectedCurrencyChanged(string? value)
    {
        if (value is null) return;
        LoadData(value);
    }

    private void LoadData(string currency)
    {
        _allData.Clear();

        if (currency == "BTC")
        {
            var btcData = _priceDatabase.GetBitcoinData().FindAll()
                .OrderByDescending(x => x.Date)
                .Select(x => (x.Date, x.Price))
                .ToList();

            _allData = btcData;
            PriceColumnHeader = language.PriceHistory_Column_Price;
        }
        else
        {
            var fiatData = _priceDatabase.GetFiatData()
                .Find(x => x.Currency == currency)
                .OrderByDescending(x => x.Date)
                .Select(x => (x.Date, x.Price))
                .ToList();

            _allData = fiatData;
            PriceColumnHeader = language.PriceHistory_Column_Rate;
        }

        HasData = _allData.Count > 0;
        TotalPages = _allData.Count > 0 ? (int)Math.Ceiling((double)_allData.Count / PageSize) : 0;
        TotalRecordsText = string.Format(language.PriceHistory_TotalRecords, _allData.Count);
        CurrentPage = _allData.Count > 0 ? 1 : 0;
        UpdatePage();

        // Chart uses chronological order (ascending)
        var chartData = _allData.AsEnumerable().Reverse().ToList();
        ChartData.RefreshChart(chartData, currency == "BTC");
    }

    private void UpdatePage()
    {
        PageItems.Clear();

        if (_allData.Count == 0)
        {
            PageInfo = string.Empty;
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            return;
        }

        var skip = (CurrentPage - 1) * PageSize;
        var pageData = _allData.Skip(skip).Take(PageSize);

        foreach (var (date, price) in pageData)
        {
            PageItems.Add(new PriceHistoryItem(
                date.ToString("yyyy-MM-dd"),
                price.ToString("N2")
            ));
        }

        PageInfo = string.Format(language.PriceHistory_PageInfo, CurrentPage, TotalPages);
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (!CanGoBack) return;
        CurrentPage--;
        UpdatePage();
    }

    [RelayCommand]
    private void NextPage()
    {
        if (!CanGoForward) return;
        CurrentPage++;
        UpdatePage();
    }

    public void Dispose()
    {
        ChartData.Dispose();
    }
}
