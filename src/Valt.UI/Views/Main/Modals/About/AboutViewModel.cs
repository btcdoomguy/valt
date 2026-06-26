using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Valt.Infra;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.About;

public partial class AboutViewModel : ValtModalViewModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AboutViewModel> _logger = null!;
    private const string DONATION_URL = "https://raw.githubusercontent.com/btcdoomguy/valt-data/refs/heads/master/donation.txt";
    
    [ObservableProperty]
    private string _donationAddresses = "Loading...";
    
    [ObservableProperty]
    private string _appVersion = $"v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown"}";

    public AboutViewModel(IHttpClientFactory httpClientFactory, ILogger<AboutViewModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public AboutViewModel()
    {
        _httpClientFactory = null!;
    }
        
    [RelayCommand]
    private async Task LoadDonationAddressesAsync()
    {
        using var client = _httpClientFactory.CreateClient(HttpClientNames.PriceProvider);

        try
        {
            // Use GetStringAsync to ensure full content is downloaded within timeout
            DonationAddresses = await client.GetStringAsync(DONATION_URL).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading donation addresses");
        }
    }
}