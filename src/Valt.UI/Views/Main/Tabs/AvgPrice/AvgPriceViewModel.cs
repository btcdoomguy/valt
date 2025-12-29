using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Infra.Modules.AvgPrice.Queries;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;
using Valt.UI.Base;
using Valt.UI.Services;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.AvgPriceLineEditor;
using Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles;

namespace Valt.UI.Views.Main.Tabs.AvgPrice;

public partial class AvgPriceViewModel : ValtTabViewModel
{
    private readonly IAvgPriceQueries _avgPriceQueries;
    private readonly IAvgPriceRepository _avgPriceRepository;
    private readonly IModalFactory _modalFactory;

    [ObservableProperty] private AvaloniaList<AvgPriceProfileDTO> _profiles = new();
    [ObservableProperty] private AvgPriceProfileDTO? _selectedProfile;

    [ObservableProperty] private AvaloniaList<AvgPriceLineDTO> _lines = new();
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
        IAvgPriceRepository avgPriceRepository,
        IModalFactory modalFactory)
    {
        _avgPriceQueries = avgPriceQueries;
        _avgPriceRepository = avgPriceRepository;
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

    partial void OnSelectedProfileChanged(AvgPriceProfileDTO? oldValue, AvgPriceProfileDTO? newValue)
    {
        AddOperationCommand.NotifyCanExecuteChanged();
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

    [ObservableProperty] private AvgPriceLineDTO? _selectedLine;

    partial void OnSelectedLineChanged(AvgPriceLineDTO? value)
    {
        EditOperationCommand.NotifyCanExecuteChanged();
        DeleteOperationCommand.NotifyCanExecuteChanged();
    }

    public bool CanAddOperation => SelectedProfile is not null;
    public bool CanEditOperation => SelectedProfile is not null && SelectedLine is not null;
    public bool CanDeleteOperation => SelectedProfile is not null && SelectedLine is not null;

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

    [RelayCommand(CanExecute = nameof(CanAddOperation))]
    private async Task AddOperation()
    {
        if (SelectedProfile is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow()!;

        var currency = FiatCurrency.GetFromCode(SelectedProfile.CurrencyCode);

        var modal =
            (AvgPriceLineEditorView)await _modalFactory.CreateAsync(ApplicationModalNames.AvgPriceLineEditor,
                ownerWindow,
                new AvgPriceLineEditorViewModel.Request
                {
                    ProfileId = SelectedProfile.Id,
                    AssetName = SelectedProfile.AssetName,
                    AssetPrecision = SelectedProfile.Precision,
                    CurrencySymbol = currency.Symbol,
                    CurrencySymbolOnRight = currency.SymbolOnRight
                })!;

        var result = await modal.ShowDialog<AvgPriceLineEditorViewModel.Response?>(ownerWindow);

        if (result is null || !result.Ok)
            return;

        await FetchAvgPriceLines();
    }

    [RelayCommand(CanExecute = nameof(CanEditOperation))]
    private async Task EditOperation()
    {
        if (SelectedProfile is null || SelectedLine is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow()!;

        var currency = FiatCurrency.GetFromCode(SelectedProfile.CurrencyCode);

        var modal =
            (AvgPriceLineEditorView)await _modalFactory.CreateAsync(ApplicationModalNames.AvgPriceLineEditor,
                ownerWindow,
                new AvgPriceLineEditorViewModel.Request
                {
                    ProfileId = SelectedProfile.Id,
                    AssetName = SelectedProfile.AssetName,
                    AssetPrecision = SelectedProfile.Precision,
                    CurrencySymbol = currency.Symbol,
                    CurrencySymbolOnRight = currency.SymbolOnRight,
                    ExistingLine = SelectedLine
                })!;

        var result = await modal.ShowDialog<AvgPriceLineEditorViewModel.Response?>(ownerWindow);

        if (result is null || !result.Ok)
            return;

        await FetchAvgPriceLines();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteOperation))]
    private async Task DeleteOperation()
    {
        if (SelectedProfile is null || SelectedLine is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow()!;

        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.AvgPrice_DeleteConfirm_Title,
            language.AvgPrice_DeleteConfirm_Message,
            ownerWindow);

        if (!confirmed)
            return;

        try
        {
            var profileId = new AvgPriceProfileId(SelectedProfile.Id);
            var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profileId);

            if (profile is null)
                return;

            var lineToDelete = FindLineById(profile, SelectedLine.Id);

            if (lineToDelete is not null)
            {
                profile.RemoveLine(lineToDelete);
                await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);
            }

            await FetchAvgPriceLines();
        }
        catch (Exception e)
        {
            await MessageBoxHelper.ShowErrorAsync("Error", e.Message, ownerWindow);
        }
    }

    private static AvgPriceLine? FindLineById(AvgPriceProfile profile, string lineId)
    {
        foreach (var line in profile.AvgPriceLines)
        {
            if (line.Id.Value == lineId)
                return line;
        }

        return null;
    }
}