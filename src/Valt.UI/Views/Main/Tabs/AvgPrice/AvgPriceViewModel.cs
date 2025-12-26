using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Infra.Modules.AvgPrice.Queries;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Tabs.AvgPrice;

public partial class AvgPriceViewModel : ValtTabViewModel
{
    private readonly IAvgPriceQueries _avgPriceQueries;

    [ObservableProperty] private AvaloniaList<AvgPriceProfileListDTO> _profiles = new();
    [ObservableProperty]
    private AvgPriceProfileListDTO? _selectedProfile;
    
    [ObservableProperty]
    private AvaloniaList<AvgPriceLineDTO> _lines;

    public AvgPriceViewModel()
    {
        if (!Design.IsDesignMode)
            return;

        Profiles = new AvaloniaList<AvgPriceProfileListDTO>()
        {
            new AvgPriceProfileListDTO(new AvgPriceProfileId().Value, "Test", "BTC", true, Icon.Empty.Name, Icon.Empty.Unicode,
                Icon.Empty.Color, FiatCurrency.Brl.Code, (int)AvgPriceCalculationMethod.BrazilianRule)
        };

        SelectedProfile = Profiles.FirstOrDefault();

        Lines = new AvaloniaList<AvgPriceLineDTO>()
        {
            new AvgPriceLineDTO(new AvgPriceLineId().Value, new DateOnly(2025, 12, 1), 0, (int)AvgPriceLineTypes.Buy,
                0.5m, 600000m, "Test", 600000m, 3000m, 0.5m)
        };
    }
    
    public AvgPriceViewModel(IAvgPriceQueries avgPriceQueries)
    {
        _avgPriceQueries = avgPriceQueries;
    }

    public void Initialize()
    {
        _ = FetchAvgPriceProfiles();
    }

    private async Task FetchAvgPriceProfiles()
    {
        var profile = await _avgPriceQueries.GetProfilesAsync(false);
        
        Profiles.Clear();
        Profiles.AddRange(profile);
    }

    partial void OnSelectedProfileChanged(AvgPriceProfileListDTO? value)
    {
        _ = FetchAvgPriceLines();
    }

    private async Task FetchAvgPriceLines()
    {
        if (SelectedProfile is null)
            return;

        var lines = await _avgPriceQueries.GetLinesOfProfileAsync(SelectedProfile.Id);
        
        Lines.Clear();
        Lines.AddRange(lines);
    }

    public override MainViewTabNames TabName => MainViewTabNames.AvgPricePageContent;
}