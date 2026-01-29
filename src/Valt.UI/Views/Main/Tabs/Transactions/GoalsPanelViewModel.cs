using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.Commands.CopyGoalsFromLastMonth;
using Valt.App.Modules.Goals.Commands.DeleteGoal;
using Valt.App.Modules.Goals.Commands.RecalculateGoal;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoals;
using Valt.Core.Modules.Goals;
using Valt.Infra.Modules.Goals.Services;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State;
using Valt.UI.Views.Main.Modals.ManageGoal;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class GoalsPanelViewModel : ValtViewModel, IDisposable
{
    private readonly ICommandDispatcher _commandDispatcher = null!;
    private readonly IQueryDispatcher _queryDispatcher = null!;
    private readonly IModalFactory _modalFactory = null!;
    private readonly IGoalProgressState _goalProgressState = null!;
    private readonly CurrencySettings _currencySettings = null!;
    private readonly FilterState _filterState = null!;
    private readonly ILogger<GoalsPanelViewModel> _logger = null!;
    private readonly SecureModeState _secureModeState = null!;

    [ObservableProperty] private AvaloniaList<GoalEntryViewModel> _goalEntries = new();

    [ObservableProperty] private GoalEntryViewModel? _selectedGoal;

    public GoalsPanelViewModel(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        IModalFactory modalFactory,
        IGoalProgressState goalProgressState,
        CurrencySettings currencySettings,
        FilterState filterState,
        ILogger<GoalsPanelViewModel> logger,
        SecureModeState secureModeState)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _modalFactory = modalFactory;
        _goalProgressState = goalProgressState;
        _currencySettings = currencySettings;
        _filterState = filterState;
        _logger = logger;
        _secureModeState = secureModeState;

        _filterState.PropertyChanged += FilterStateOnPropertyChanged;
        _secureModeState.PropertyChanged += SecureModeStateOnPropertyChanged;

        WeakReferenceMessenger.Default.Register<FilterDateRangeChanged>(this, OnFilterDateRangeChangedReceive);
        WeakReferenceMessenger.Default.Register<GoalListChanged>(this, OnGoalListChangedReceive);
        WeakReferenceMessenger.Default.Register<GoalProgressUpdated>(this, OnGoalProgressUpdatedReceive);

        _ = FetchGoals();
    }

    private void FilterStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(GoalsCurrentMonthDescription));
    }

    private void SecureModeStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(IsSecureModeEnabled));
        });
    }

    private void OnFilterDateRangeChangedReceive(object recipient, FilterDateRangeChanged message)
    {
        _ = FetchGoals();
    }

    private void OnGoalListChangedReceive(object recipient, GoalListChanged message)
    {
        _ = FetchGoals();
    }

    private void OnGoalProgressUpdatedReceive(object recipient, GoalProgressUpdated message)
    {
        _ = UpdateGoalProgress();
    }

    public bool IsSecureModeEnabled => _secureModeState.IsEnabled;

    public string GoalsCurrentMonthDescription =>
        $"({DateOnly.FromDateTime(_filterState.MainDate).ToString("MM/yy")})";

    private async Task FetchGoals()
    {
        try
        {
            var currentDate = DateOnly.FromDateTime(_filterState.MainDate);
            var goals = await _queryDispatcher.DispatchAsync(new GetGoalsQuery { FilterDate = currentDate });

            var sortedGoals = goals
                .OrderBy(g => GetGoalSortOrder(g))
                .ThenBy(g => g.GoalType.TypeId)
                .ThenBy(g => g.RefDate)
                .ToList();

            GoalEntries.Clear();
            foreach (var goal in sortedGoals)
            {
                GoalEntries.Add(new GoalEntryViewModel(goal, _currencySettings.MainFiatCurrency));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching goals");
        }
    }

    /// <summary>
    /// Updates existing goal entries in place to enable progress animation.
    /// Only adds/removes entries when the goal list changes.
    /// </summary>
    private async Task UpdateGoalProgress()
    {
        try
        {
            var currentDate = DateOnly.FromDateTime(_filterState.MainDate);
            var goals = await _queryDispatcher.DispatchAsync(new GetGoalsQuery { FilterDate = currentDate });

            var goalsDict = goals.ToDictionary(g => g.Id, g => g);

            // Update existing entries in place (this triggers animation)
            foreach (var entry in GoalEntries.ToList())
            {
                if (goalsDict.TryGetValue(entry.Id, out var updatedGoal))
                {
                    entry.UpdateGoal(updatedGoal);
                    goalsDict.Remove(entry.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating goal progress");
        }
    }

    /// <summary>
    /// Returns a sort order value for goals:
    /// 0 = Monthly Open goals
    /// 1 = Yearly Open goals
    /// 2 = Completed goals
    /// 3 = Failed goals
    /// </summary>
    private static int GetGoalSortOrder(GoalDTO goal)
    {
        return goal.State switch
        {
            (int)GoalStates.Open => goal.Period == (int)GoalPeriods.Monthly ? 0 : 1,
            (int)GoalStates.Completed => 2,
            (int)GoalStates.Failed => 3,
            _ => 4
        };
    }

    #region Commands

    [RelayCommand]
    private async Task AddGoal()
    {
        var ownerWindow = GetUserControlOwnerWindow();

        var window =
            (ManageGoalView)await _modalFactory.CreateAsync(ApplicationModalNames.ManageGoal, ownerWindow)!;

        var result = await window.ShowDialog<ManageGoalViewModel.Response?>(ownerWindow!);

        if (result is null)
            return;

        await FetchGoals();
        WeakReferenceMessenger.Default.Send(new GoalListChanged());
    }

    [RelayCommand]
    private async Task EditGoal(GoalEntryViewModel? entry)
    {
        if (entry is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow();

        var window = (ManageGoalView)await _modalFactory.CreateAsync(
            ApplicationModalNames.ManageGoal,
            ownerWindow, entry.Id)!;

        _ = await window.ShowDialog<ManageGoalViewModel.Response?>(ownerWindow!);

        await FetchGoals();
        WeakReferenceMessenger.Default.Send(new GoalListChanged());
    }

    [RelayCommand]
    private async Task RecalculateGoal(GoalEntryViewModel? entry)
    {
        if (entry is null)
            return;

        var result = await _commandDispatcher.DispatchAsync(new RecalculateGoalCommand
        {
            GoalId = entry.Id
        });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetUserControlOwnerWindow()!);
            return;
        }

        _goalProgressState.MarkAsStale();
        await FetchGoals();
        WeakReferenceMessenger.Default.Send(new GoalListChanged());
    }

    [RelayCommand]
    private async Task DeleteGoal(GoalEntryViewModel? entry)
    {
        if (entry is null)
            return;

        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.Goals_DeleteConfirm_Title,
            language.Goals_DeleteConfirm_Message,
            GetUserControlOwnerWindow()!);

        if (!confirmed)
            return;

        var result = await _commandDispatcher.DispatchAsync(new DeleteGoalCommand
        {
            GoalId = entry.Id
        });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetUserControlOwnerWindow()!);
            return;
        }

        await FetchGoals();
        WeakReferenceMessenger.Default.Send(new GoalListChanged());
    }

    [RelayCommand]
    private async Task CopyFromLastMonth()
    {
        try
        {
            var currentDate = DateOnly.FromDateTime(_filterState.MainDate);

            var result = await _commandDispatcher.DispatchAsync(new CopyGoalsFromLastMonthCommand
            {
                CurrentDate = currentDate
            });

            if (result.IsFailure)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetUserControlOwnerWindow()!);
                return;
            }

            if (result.Value!.CopiedCount > 0)
            {
                await FetchGoals();
                WeakReferenceMessenger.Default.Send(new GoalListChanged());
                _goalProgressState.MarkAsStale();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying goals from last month");
        }
    }

    #endregion

    public void Dispose()
    {
        _filterState.PropertyChanged -= FilterStateOnPropertyChanged;
        _secureModeState.PropertyChanged -= SecureModeStateOnPropertyChanged;

        WeakReferenceMessenger.Default.Unregister<FilterDateRangeChanged>(this);
        WeakReferenceMessenger.Default.Unregister<GoalListChanged>(this);
        WeakReferenceMessenger.Default.Unregister<GoalProgressUpdated>(this);
    }
}
