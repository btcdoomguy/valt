using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Infra.Modules.AvgPrice.Queries;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;
using Valt.UI.Base;
using static Valt.UI.Base.TaskExtensions;
using Valt.UI.Services;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Modals.AvgPriceLineEditor;
using Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.AvgPrice.Models;

namespace Valt.UI.Views.Main.Tabs.AvgPrice;

public partial class AvgPriceViewModel : ValtTabViewModel, IDisposable
{
    private readonly IAvgPriceQueries _avgPriceQueries = null!;
    private readonly IAvgPriceRepository _avgPriceRepository = null!;
    private readonly IAvgPriceTotalizer _avgPriceTotalizer = null!;
    private readonly IModalFactory _modalFactory = null!;
    private readonly IClock _clock = null!;
    private readonly SecureModeState? _secureModeState;
    private readonly ILogger<AvgPriceViewModel>? _logger;

    [ObservableProperty] private AvaloniaList<AvgPriceProfileDTO> _profiles = new();
    [ObservableProperty] private AvgPriceProfileDTO? _selectedProfile;

    [ObservableProperty] private AvaloniaList<AvgPriceLineViewModel> _lines = new();

    // Totals filter properties
    [ObservableProperty] private DateTime _totalsFilterDate;
    [ObservableProperty] private DateRange _totalsFilterRange = new(DateTime.MinValue, DateTime.MinValue);

    // Totals data
    [ObservableProperty] private AvaloniaList<AvgPriceTotalsRowViewModel> _totalsRows = new();
    [ObservableProperty] private bool _isTotalsLoading;

    // Current position summary (from last line)
    [ObservableProperty] private string _currentPosition = "-";
    [ObservableProperty] private string _currentAvgPrice = "-";

    partial void OnCurrentPositionChanged(string value) => OnPropertyChanged(nameof(DisplayCurrentPosition));
    partial void OnCurrentAvgPriceChanged(string value) => OnPropertyChanged(nameof(DisplayCurrentAvgPrice));

    public string DisplayCurrentPosition => _secureModeState?.IsEnabled == true ? "---" : CurrentPosition;
    public string DisplayCurrentAvgPrice => _secureModeState?.IsEnabled == true ? "---" : CurrentAvgPrice;
    public bool IsSecureModeEnabled => _secureModeState?.IsEnabled == true;

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

        var testDto = new AvgPriceLineDTO(new AvgPriceLineId().Value, new DateOnly(2025, 12, 1), 0, (int)AvgPriceLineTypes.Buy,
            0.5m, 600000m, "Test", 600000m, 3000m, 0.5m);
        Lines = new AvaloniaList<AvgPriceLineViewModel>
        {
            new AvgPriceLineViewModel(testDto, 8, "pt-BR")
        };

        TotalsFilterDate = DateTime.Now;
    }

    public AvgPriceViewModel(IAvgPriceQueries avgPriceQueries,
        IAvgPriceRepository avgPriceRepository,
        IAvgPriceTotalizer avgPriceTotalizer,
        IModalFactory modalFactory,
        IClock clock,
        SecureModeState secureModeState,
        ILogger<AvgPriceViewModel> logger)
    {
        _avgPriceQueries = avgPriceQueries;
        _avgPriceRepository = avgPriceRepository;
        _avgPriceTotalizer = avgPriceTotalizer;
        _modalFactory = modalFactory;
        _clock = clock;
        _secureModeState = secureModeState;
        _logger = logger;

        _secureModeState.PropertyChanged += OnSecureModeStatePropertyChanged;

        TotalsFilterDate = _clock.GetCurrentDateTimeUtc();
    }

    public void Initialize()
    {
        FetchAvgPriceProfiles().SafeFireAndForget(logger: _logger, callerName: nameof(FetchAvgPriceProfiles));
    }

    partial void OnTotalsFilterRangeChanged(DateRange value)
    {
        FetchTotals().SafeFireAndForget(logger: _logger, callerName: nameof(FetchTotals));
    }

    private async Task FetchAvgPriceProfiles()
    {
        var profiles = await _avgPriceQueries.GetProfilesAsync(false);

        Profiles.Clear();
        Profiles.AddRange(profiles);

        // Auto-select first profile if none selected
        if (SelectedProfile is null && Profiles.Count > 0)
            SelectedProfile = Profiles.First();
    }

    partial void OnSelectedProfileChanged(AvgPriceProfileDTO? oldValue, AvgPriceProfileDTO? newValue)
    {
        AddOperationCommand.NotifyCanExecuteChanged();
        FetchAvgPriceLines().SafeFireAndForget(logger: _logger, callerName: nameof(FetchAvgPriceLines));
        FetchTotals().SafeFireAndForget(logger: _logger, callerName: nameof(FetchTotals));
    }

    private async Task FetchAvgPriceLines(string? selectLineId = null)
    {
        if (SelectedProfile is null)
        {
            Lines.Clear();
            CurrentPosition = "-";
            CurrentAvgPrice = "-";
            SelectedLine = null;
            return;
        }

        var dtos = await _avgPriceQueries.GetLinesOfProfileAsync(SelectedProfile.Id);
        var currency = FiatCurrency.GetFromCode(SelectedProfile.CurrencyCode);
        var viewModels = dtos.Select(dto => new AvgPriceLineViewModel(dto, SelectedProfile.Precision, currency.CultureName)).ToList();

        Lines.Clear();
        Lines.AddRange(viewModels);

        // Restore selection if ID provided
        if (!string.IsNullOrEmpty(selectLineId))
        {
            SelectedLine = Lines.FirstOrDefault(l => l.Id == selectLineId);
        }
        else
        {
            SelectedLine = null;
        }

        // Update current position summary from the last line
        UpdateCurrentPositionSummary();
    }

    private void UpdateCurrentPositionSummary()
    {
        if (Lines.Count == 0 || SelectedProfile is null)
        {
            CurrentPosition = "-";
            CurrentAvgPrice = "-";
            return;
        }

        var lastLine = Lines.Last();
        CurrentPosition = lastLine.TotalQuantity;
        CurrentAvgPrice = lastLine.AvgCostOfAcquisition;
    }

    [ObservableProperty] private AvgPriceLineViewModel? _selectedLine;

    partial void OnSelectedLineChanged(AvgPriceLineViewModel? value)
    {
        EditOperationCommand.NotifyCanExecuteChanged();
        DeleteOperationCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    public bool CanAddOperation => SelectedProfile is not null;
    public bool CanEditOperation => SelectedProfile is not null && SelectedLine is not null;
    public bool CanDeleteOperation => SelectedProfile is not null && SelectedLine is not null;

    public bool CanMoveUp
    {
        get
        {
            if (SelectedProfile is null || SelectedLine is null)
                return false;

            var linesOnSameDate = Lines.Where(x => x.Date == SelectedLine.Date).OrderBy(x => x.DisplayOrder).ToList();

            // Need at least 2 lines on the same date and selected line must not be first
            return linesOnSameDate.Count > 1 && linesOnSameDate.First().Id != SelectedLine.Id;
        }
    }

    public bool CanMoveDown
    {
        get
        {
            if (SelectedProfile is null || SelectedLine is null)
                return false;

            var linesOnSameDate = Lines.Where(x => x.Date == SelectedLine.Date).OrderBy(x => x.DisplayOrder).ToList();

            // Need at least 2 lines on the same date and selected line must not be last
            return linesOnSameDate.Count > 1 && linesOnSameDate.Last().Id != SelectedLine.Id;
        }
    }

    [RelayCommand]
    private async Task ManageProfiles()
    {
        var ownerWindow = GetUserControlOwnerWindow()!;

        var modal =
            (ManageAvgPriceProfilesView)await _modalFactory.CreateAsync(ApplicationModalNames.AvgPriceProfileManager,
                ownerWindow,
                SelectedProfile?.Id)!;

        _ = await modal.ShowDialog<ManageAvgPriceProfilesViewModel.Response?>(ownerWindow);

        await FetchAvgPriceProfiles();
        await FetchAvgPriceLines();
        await FetchTotals();
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
        await FetchTotals();
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
                    ExistingLine = SelectedLine.ToDto()
                })!;

        var result = await modal.ShowDialog<AvgPriceLineEditorViewModel.Response?>(ownerWindow);

        if (result is null || !result.Ok)
            return;

        await FetchAvgPriceLines();
        await FetchTotals();
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

        // Find next item to select after deletion
        var currentIndex = Lines.IndexOf(SelectedLine);
        string? nextLineId = null;

        if (currentIndex >= 0 && Lines.Count > 1)
        {
            // Try to select next item, or previous if deleting last
            if (currentIndex < Lines.Count - 1)
                nextLineId = Lines[currentIndex + 1].Id;
            else if (currentIndex > 0)
                nextLineId = Lines[currentIndex - 1].Id;
        }

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

            await FetchAvgPriceLines(nextLineId);
            await FetchTotals();
        }
        catch (Exception e)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, e.Message, ownerWindow);
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

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private async Task MoveUp()
    {
        if (SelectedProfile is null || SelectedLine is null)
            return;

        // Save ID before refresh
        var lineIdToSelect = SelectedLine.Id;

        try
        {
            var profileId = new AvgPriceProfileId(SelectedProfile.Id);
            var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profileId);

            if (profile is null)
                return;

            var lineToMove = FindLineById(profile, lineIdToSelect);

            if (lineToMove is not null)
            {
                profile.MoveLineUp(lineToMove);
                await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);
            }

            await FetchAvgPriceLines(lineIdToSelect);
            await FetchTotals();
        }
        catch (Exception e)
        {
            var ownerWindow = GetUserControlOwnerWindow()!;
            await MessageBoxHelper.ShowErrorAsync(language.Error, e.Message, ownerWindow);
        }
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private async Task MoveDown()
    {
        if (SelectedProfile is null || SelectedLine is null)
            return;

        // Save ID before refresh
        var lineIdToSelect = SelectedLine.Id;

        try
        {
            var profileId = new AvgPriceProfileId(SelectedProfile.Id);
            var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profileId);

            if (profile is null)
                return;

            var lineToMove = FindLineById(profile, lineIdToSelect);

            if (lineToMove is not null)
            {
                profile.MoveLineDown(lineToMove);
                await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);
            }

            await FetchAvgPriceLines(lineIdToSelect);
            await FetchTotals();
        }
        catch (Exception e)
        {
            var ownerWindow = GetUserControlOwnerWindow()!;
            await MessageBoxHelper.ShowErrorAsync(language.Error, e.Message, ownerWindow);
        }
    }

    private async Task FetchTotals()
    {
        if (SelectedProfile is null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => TotalsRows.Clear());
            return;
        }

        IsTotalsLoading = true;

        try
        {
            var year = TotalsFilterDate.Year;
            var profileId = new AvgPriceProfileId(SelectedProfile.Id);
            var currency = FiatCurrency.GetFromCode(SelectedProfile.CurrencyCode);

            var totals = await _avgPriceTotalizer.GetTotalsAsync(year, new[] { profileId });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalsRows.Clear();

                // Add monthly rows
                foreach (var monthlyTotal in totals.MonthlyTotals)
                {
                    TotalsRows.Add(new AvgPriceTotalsRowViewModel(monthlyTotal.Month, monthlyTotal.Values, currency));
                }

                // Add yearly total row
                TotalsRows.Add(new AvgPriceTotalsRowViewModel(
                    new DateTime(year, 1, 1),
                    totals.YearlyTotals,
                    currency,
                    isYearlyTotal: true));

                IsTotalsLoading = false;
            });
        }
        catch (Exception)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalsRows.Clear();
                IsTotalsLoading = false;
            });
        }
    }

    #region Event Handlers

    private void OnSecureModeStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(DisplayCurrentPosition));
        OnPropertyChanged(nameof(DisplayCurrentAvgPrice));
        OnPropertyChanged(nameof(IsSecureModeEnabled));
    }

    #endregion

    public void Dispose()
    {
        if (_secureModeState is not null)
            _secureModeState.PropertyChanged -= OnSecureModeStatePropertyChanged;
    }
}