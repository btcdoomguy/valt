using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.About;

public partial class AboutViewModel : ValtModalViewModel
{
    private readonly ILogger<AboutViewModel> _logger;
    private const string DONATION_URL = "https://raw.githubusercontent.com/btcdoomguy/valt-data/refs/heads/master/donation.txt";
    
    [ObservableProperty]
    private string _donationAddresses = "Loading...";
    
    [ObservableProperty]
    private string _appVersion = $"v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown"}";

    public AboutViewModel(ILogger<AboutViewModel> logger)
    {
        _logger = logger;
    }

    public AboutViewModel()
    {
        
    }
        
    [RelayCommand]
    private async Task LoadDonationAddressesAsync()
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };

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