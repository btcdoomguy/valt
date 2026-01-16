using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Kernel.Exceptions;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ManageGoal;

public partial class ManageGoalViewModel : ValtModalValidatorViewModel
{
    private readonly IGoalRepository? _goalRepository;

    #region Form Data

    private GoalId? _goalId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMonthSelector))]
    private GoalPeriods _selectedPeriod = GoalPeriods.Monthly;

    [ObservableProperty]
    private string _selectedMonth = DateTime.Today.Month.ToString();

    [ObservableProperty]
    private int _selectedYear = DateTime.Today.Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStackBitcoinInput))]
    private string _selectedGoalType = GoalTypeNames.StackBitcoin.ToString();

    [ObservableProperty]
    private BtcValue _targetBtcAmount = BtcValue.Empty;

    [ObservableProperty]
    private bool _isEditMode;

    public bool ShowMonthSelector => SelectedPeriod == GoalPeriods.Monthly;

    public bool ShowStackBitcoinInput => SelectedGoalType == GoalTypeNames.StackBitcoin.ToString();

    public static List<GoalPeriods> AvailablePeriods => Enum.GetValues<GoalPeriods>().ToList();

    public static List<ComboBoxValue> AvailableGoalTypes =>
    [
        new(language.GoalType_StackBitcoin, GoalTypeNames.StackBitcoin.ToString())
    ];

    public static List<ComboBoxValue> AvailableMonths =>
        Enumerable.Range(1, 12)
            .Select(m => new ComboBoxValue(
                CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m),
                m.ToString()))
            .ToList();

    public static List<int> AvailableYears => Enumerable.Range(DateTime.Today.Year - 5, 11).ToList();

    #endregion

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ManageGoalViewModel()
    {
        SelectedPeriod = GoalPeriods.Monthly;
        SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();
        TargetBtcAmount = BtcValue.New(1_000_000);
    }

    public ManageGoalViewModel(IGoalRepository goalRepository)
    {
        _goalRepository = goalRepository;

        SelectedPeriod = GoalPeriods.Monthly;
        SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not null && Parameter is string goalIdString)
        {
            var goal = await _goalRepository!.GetByIdAsync(new GoalId(goalIdString));

            if (goal is null)
            {
                await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, language.Error_GoalNotFound, GetWindow!());
                return;
            }

            _goalId = goal.Id;
            IsEditMode = true;
            SelectedPeriod = goal.Period;
            SelectedYear = goal.RefDate.Year;
            SelectedMonth = goal.RefDate.Month.ToString();
            SelectedGoalType = goal.GoalType.TypeName.ToString();

            switch (goal.GoalType)
            {
                case StackBitcoinGoalType stackBitcoin:
                    TargetBtcAmount = stackBitcoin.TargetAmount;
                    break;
            }
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            var goalTypeName = Enum.Parse<GoalTypeNames>(SelectedGoalType);
            IGoalType goalType = goalTypeName switch
            {
                GoalTypeNames.StackBitcoin => new StackBitcoinGoalType(TargetBtcAmount),
                _ => throw new ArgumentOutOfRangeException()
            };

            var month = SelectedPeriod == GoalPeriods.Yearly ? 1 : int.Parse(SelectedMonth);
            var refDate = new DateOnly(SelectedYear, month, 1);

            Goal goal;
            if (_goalId is null)
            {
                goal = Goal.New(refDate, SelectedPeriod, goalType);
            }
            else
            {
                var existingGoal = await _goalRepository!.GetByIdAsync(_goalId);

                if (existingGoal is null)
                    throw new EntityNotFoundException(nameof(Goal), _goalId);

                goal = Goal.Create(
                    existingGoal.Id,
                    refDate,
                    SelectedPeriod,
                    goalType,
                    existingGoal.Progress,
                    false,
                    existingGoal.LastUpdatedAt,
                    existingGoal.State,
                    existingGoal.Version);
            }

            await _goalRepository!.SaveAsync(goal);
            CloseDialog?.Invoke(new Response(true, goal.Id));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record Response(bool Ok, GoalId? GoalId = null);
}
