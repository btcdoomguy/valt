using System;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.DeleteLoanStateUpdate;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetLoanStateTimeline;
using Valt.Infra.Kernel;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State.Events;
using Valt.UI.Views.Main.Modals.UpdateLoanState;

namespace Valt.UI.Views.Main.Modals.LoanStateHistory;

public partial class LoanStateHistoryViewModel : ValtModalViewModel
{
    private readonly IQueryDispatcher _queryDispatcher = null!;
    private readonly ICommandDispatcher _commandDispatcher = null!;
    private readonly IModalFactory _modalFactory = null!;

    [ObservableProperty] private string _windowTitle = language.LoanStateHistory_Title;
    [ObservableProperty] private string _assetId = string.Empty;
    [ObservableProperty] private LoanStateHistoryItemViewModel? _selectedSnapshot;

    public AvaloniaList<LoanStateHistoryItemViewModel> Snapshots { get; set; } = new();

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public LoanStateHistoryViewModel()
    {
        if (!Design.IsDesignMode) return;

        Snapshots.Add(new LoanStateHistoryItemViewModel
        {
            EffectiveDate = new DateOnly(2024, 6, 1),
            EffectiveDateFormatted = "6/1/2024",
            CurrentTotalDebtFormatted = "$ 105 000.00",
            CollateralSatsFormatted = "5 000 000 sats",
            AprFormatted = "12.00%",
            FeesFormatted = "$ 500.00"
        });

        Snapshots.CollectionChanged += (_, _) => DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    public LoanStateHistoryViewModel(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        IModalFactory modalFactory)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _modalFactory = modalFactory;

        Snapshots.CollectionChanged += (_, _) => DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not Request request)
            return;

        AssetId = request.AssetId;
        await LoadTimelineAsync();
    }

    private async Task LoadTimelineAsync()
    {
        var snapshots = await _queryDispatcher.DispatchAsync(
            new GetLoanStateTimelineQuery { AssetId = AssetId });

        Snapshots.Clear();
        foreach (var s in snapshots)
        {
            Snapshots.Add(new LoanStateHistoryItemViewModel
            {
                EffectiveDate = s.EffectiveDate,
                EffectiveDateFormatted = s.EffectiveDate.ToShortDateString(),
                CurrentTotalDebtFormatted = CurrencyDisplay.FormatFiat(s.CurrentTotalDebt, s.CurrencyCode),
                CollateralSatsFormatted = $"{s.CollateralSats:N0} sats",
                AprFormatted = $"{s.Apr * 100m:N2}%",
                FeesFormatted = CurrencyDisplay.FormatFiat(s.Fees, s.CurrencyCode)
            });
        }

        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task AddNewState()
    {
        if (GetWindow is null)
            return;

        var ownerWindow = GetWindow();
        CloseWindow?.Invoke();

        var modal = (UpdateLoanStateView)await _modalFactory.CreateAsync(
            ApplicationModalNames.UpdateLoanState,
            ownerWindow,
            new UpdateLoanStateViewModel.Request { AssetId = AssetId });

        var result = await modal.ShowDialogSafeAsync<UpdateLoanStateViewModel.Response?>(ownerWindow);
        if (result?.Ok == true)
        {
            WeakReferenceMessenger.Default.Send(new LoanStateUpdatedMessage());
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelected()
    {
        if (SelectedSnapshot is null || GetWindow is null)
            return;

        var ownerWindow = GetWindow();
        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.LoanStateHistory_DeleteConfirmationTitle,
            string.Format(language.LoanStateHistory_DeleteConfirmationMessage, SelectedSnapshot.EffectiveDateFormatted),
            ownerWindow);

        if (!confirmed)
            return;

        var result = await _commandDispatcher.DispatchAsync(
            new DeleteLoanStateUpdateCommand
            {
                AssetId = AssetId,
                EffectiveDate = SelectedSnapshot.EffectiveDate
            });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(
                language.Error,
                string.Format(language.LoanStateHistory_DeleteError, result.Error!.Message),
                ownerWindow);
            return;
        }

        WeakReferenceMessenger.Default.Send(new LoanStateUpdatedMessage());
        await LoadTimelineAsync();
    }

    private bool CanDeleteSelected() => SelectedSnapshot is not null && Snapshots.Count > 1;

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    partial void OnSelectedSnapshotChanged(LoanStateHistoryItemViewModel? value)
    {
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    public record Request
    {
        public required string AssetId { get; init; }
    }

    public record Response
    {
    }

    public record LoanStateHistoryItemViewModel
    {
        public required DateOnly EffectiveDate { get; init; }
        public required string EffectiveDateFormatted { get; init; }
        public required string CurrentTotalDebtFormatted { get; init; }
        public required string CollateralSatsFormatted { get; init; }
        public required string AprFormatted { get; init; }
        public required string FeesFormatted { get; init; }
    }
}
