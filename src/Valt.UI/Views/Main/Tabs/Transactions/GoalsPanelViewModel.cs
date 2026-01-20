using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
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
    private readonly IModalFactory _modalFactory;
    private readonly IGoalRepository _goalRepository;
    private readonly GoalProgressState _goalProgressState;
    private readonly CurrencySettings _currencySettings;
    private readonly FilterState _filterState;
    private readonly ILogger<GoalsPanelViewModel> _logger;
    private readonly SecureModeState _secureModeState;

    [ObservableProperty] private AvaloniaList<GoalEntryViewModel> _goalEntries = new();

    [ObservableProperty] private GoalEntryViewModel? _selectedGoal;

    public GoalsPanelViewModel(
        IModalFactory modalFactory,
        IGoalRepository goalRepository,
        GoalProgressState goalProgressState,
        CurrencySettings currencySettings,
        FilterState filterState,
        ILogger<GoalsPanelViewModel> logger,
        SecureModeState secureModeState)
    {
        _modalFactory = modalFactory;
        _goalRepository = goalRepository;
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
            var allGoals = await _goalRepository.GetAllAsync();

            var goalsForPeriod = allGoals
                .Where(g =>
                {
                    var range = g.GetPeriodRange();
                    return currentDate >= range.Start && currentDate <= range.End;
                })
                .OrderBy(g => GetGoalSortOrder(g))
                .ThenBy(g => g.GoalType.TypeName)
                .ThenBy(g => g.RefDate)
                .ToList();

            GoalEntries.Clear();
            foreach (var goal in goalsForPeriod)
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
            var allGoals = await _goalRepository.GetAllAsync();

            var goalsForPeriod = allGoals
                .Where(g =>
                {
                    var range = g.GetPeriodRange();
                    return currentDate >= range.Start && currentDate <= range.End;
                })
                .ToDictionary(g => g.Id.ToString());

            // Update existing entries in place (this triggers animation)
            foreach (var entry in GoalEntries.ToList())
            {
                if (goalsForPeriod.TryGetValue(entry.Id, out var updatedGoal))
                {
                    entry.UpdateGoal(updatedGoal);
                    goalsForPeriod.Remove(entry.Id);
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
    private static int GetGoalSortOrder(Goal goal)
    {
        return goal.State switch
        {
            GoalStates.Open => goal.Period == GoalPeriods.Monthly ? 0 : 1,
            GoalStates.Completed => 2,
            GoalStates.Failed => 3,
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

        var goal = await _goalRepository.GetByIdAsync(new GoalId(entry.Id));
        if (goal is null)
            return;

        goal.Recalculate();
        await _goalRepository.SaveAsync(goal);
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

        var goal = await _goalRepository.GetByIdAsync(new GoalId(entry.Id));
        if (goal is null)
            return;

        await _goalRepository.DeleteAsync(goal);

        await FetchGoals();
        WeakReferenceMessenger.Default.Send(new GoalListChanged());
    }

    [RelayCommand]
    private async Task CopyFromLastMonth()
    {
        try
        {
            var currentDate = DateOnly.FromDateTime(_filterState.MainDate);
            var currentMonthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            var allGoals = await _goalRepository.GetAllAsync();

            var previousMonthGoals = allGoals
                .Where(g => g.Period == GoalPeriods.Monthly &&
                           g.RefDate.Year == previousMonthStart.Year &&
                           g.RefDate.Month == previousMonthStart.Month)
                .ToList();

            var currentMonthGoals = allGoals
                .Where(g => g.Period == GoalPeriods.Monthly &&
                           g.RefDate.Year == currentMonthStart.Year &&
                           g.RefDate.Month == currentMonthStart.Month)
                .ToList();

            var copiedCount = 0;

            foreach (var previousGoal in previousMonthGoals)
            {
                var isDuplicate = currentMonthGoals.Any(g =>
                    g.GoalType.HasSameTargetAs(previousGoal.GoalType));

                if (isDuplicate)
                    continue;

                var newGoalType = previousGoal.GoalType.WithResetProgress();
                var newGoal = Goal.New(currentMonthStart, GoalPeriods.Monthly, newGoalType);
                await _goalRepository.SaveAsync(newGoal);
                copiedCount++;
            }

            if (copiedCount > 0)
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
