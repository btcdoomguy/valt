using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Infra.Modules.AvgPrice.Queries;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;
using Valt.UI.Base;
using Valt.UI.Services;
using Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles;

namespace Valt.UI.Views.Main.Tabs.AvgPrice;

public partial class AvgPriceViewModel : ValtTabViewModel
{
    private readonly IAvgPriceQueries _avgPriceQueries;
    private readonly IModalFactory _modalFactory;

    [ObservableProperty] private AvaloniaList<AvgPriceProfileDTO> _profiles = new();
    [ObservableProperty] private AvgPriceProfileDTO? _selectedProfile;

    [ObservableProperty] private AvaloniaList<AvgPriceLineDTO> _lines;
    public override MainViewTabNames TabName => MainViewTabNames.AvgPricePageContent;

    public AvgPriceViewModel()
    {
        if (!Design.IsDesignMode)
            return;

        Profiles = new AvaloniaList<AvgPriceProfileDTO>()
        {
            new AvgPriceProfileDTO(new AvgPriceProfileId().Value, "Test", "BTC", 8, true, Icon.Empty.Name,
                Icon.Empty.Unicode,
                Icon.Empty.Color, FiatCurrency.Brl.Code, (int)AvgPriceCalculationMethod.BrazilianRule)
        };

        SelectedProfile = Profiles.FirstOrDefault();

        Lines = new AvaloniaList<AvgPriceLineDTO>()
        {
            new AvgPriceLineDTO(new AvgPriceLineId().Value, new DateOnly(2025, 12, 1), 0, (int)AvgPriceLineTypes.Buy,
                0.5m, 600000m, "Test", 600000m, 3000m, 0.5m)
        };
    }

    public AvgPriceViewModel(IAvgPriceQueries avgPriceQueries,
        IModalFactory modalFactory)
    {
        _avgPriceQueries = avgPriceQueries;
        _modalFactory = modalFactory;
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

    partial void OnSelectedProfileChanged(AvgPriceProfileDTO? value)
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

    [RelayCommand]
    private async Task ManageProfiles()
    {
        var ownerWindow = GetUserControlOwnerWindow()!;

        var modal =
            (ManageAvgPriceProfilesView)await _modalFactory.CreateAsync(ApplicationModalNames.AvgPriceProfileManager,
                ownerWindow,
                SelectedProfile?.Id)!;

        var result = await modal.ShowDialog<ManageAvgPriceProfilesViewModel.Response?>(ownerWindow);

        if (result is null)
            return;

        await FetchAvgPriceProfiles();
    }
}