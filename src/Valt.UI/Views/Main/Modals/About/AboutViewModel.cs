using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.About;

public partial class AboutViewModel : ValtModalViewModel
{
    private readonly ILogger<AboutViewModel> _logger;
    private const string DONATION_URL = "https://raw.githubusercontent.com/btcdoomguy/valt-data/refs/heads/master/donation.txt";
    
    [ObservableProperty]
    private string _donationAddresses = "Loading...";

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
            var response = await client.GetAsync(DONATION_URL);
            response.EnsureSuccessStatusCode();
            
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            
            using var reader = new StreamReader(stream);

            DonationAddresses = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of Bitcoin Initial Seed Price provider");
        }
    }
}