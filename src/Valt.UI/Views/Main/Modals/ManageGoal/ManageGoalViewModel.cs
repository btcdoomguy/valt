using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.Commands.CreateGoal;
using Valt.App.Modules.Goals.Commands.EditGoal;
using Valt.App.Modules.Goals.Queries.GetGoal;
using Valt.Core.Modules.Goals;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

namespace Valt.UI.Views.Main.Modals.ManageGoal;

public partial class ManageGoalViewModel : ValtModalValidatorViewModel
{
    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly CurrencySettings? _currencySettings;

    #region Form Data

    private string? _goalId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMonthSelector))]
    private string _selectedPeriod = GoalPeriods.Monthly.ToString();

    [ObservableProperty]
    private string _selectedMonth = DateTime.Today.Month.ToString();

    [ObservableProperty]
    private int _selectedYear = DateTime.Today.Year;

    [ObservableProperty]
    private string _selectedGoalType = GoalTypeNames.StackBitcoin.ToString();

    [ObservableProperty]
    private IGoalTypeEditorViewModel? _currentGoalTypeEditor;

    [ObservableProperty]
    private bool _isEditMode;

    public bool ShowMonthSelector => SelectedPeriod == GoalPeriods.Monthly.ToString();

    public static List<ComboBoxValue> AvailablePeriods =>
    [
        new(language.GoalPeriod_Monthly, GoalPeriods.Monthly.ToString()),
        new(language.GoalPeriod_Yearly, GoalPeriods.Yearly.ToString())
    ];

    public static List<ComboBoxValue> AvailableGoalTypes =>
    [
        new(language.GoalType_StackBitcoin, GoalTypeNames.StackBitcoin.ToString()),
        new(language.GoalType_SpendingLimit, GoalTypeNames.SpendingLimit.ToString()),
        new(language.GoalType_Dca, GoalTypeNames.Dca.ToString()),
        new(language.GoalType_IncomeFiat, GoalTypeNames.IncomeFiat.ToString()),
        new(language.GoalType_IncomeBtc, GoalTypeNames.IncomeBtc.ToString()),
        new(language.GoalType_ReduceExpenseCategory, GoalTypeNames.ReduceExpenseCategory.ToString()),
        new(language.GoalType_BitcoinHodl, GoalTypeNames.BitcoinHodl.ToString())
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
        SelectedPeriod = GoalPeriods.Monthly.ToString();
        SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();
        CurrentGoalTypeEditor = new StackBitcoinGoalTypeEditorViewModel();
    }

    public ManageGoalViewModel(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        CurrencySettings currencySettings)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _currencySettings = currencySettings;

        SelectedPeriod = GoalPeriods.Monthly.ToString();
        SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();
        CurrentGoalTypeEditor = CreateEditorForGoalType(GoalTypeNames.StackBitcoin);
    }

    partial void OnSelectedGoalTypeChanged(string value)
    {
        if (Enum.TryParse<GoalTypeNames>(value, out var goalTypeName))
        {
            CurrentGoalTypeEditor = CreateEditorForGoalType(goalTypeName);
        }
    }

    private IGoalTypeEditorViewModel CreateEditorForGoalType(GoalTypeNames goalTypeName)
    {
        return goalTypeName switch
        {
            GoalTypeNames.StackBitcoin => new StackBitcoinGoalTypeEditorViewModel(),
            GoalTypeNames.SpendingLimit => _currencySettings != null
                ? new SpendingLimitGoalTypeEditorViewModel(_currencySettings)
                : new SpendingLimitGoalTypeEditorViewModel(),
            GoalTypeNames.Dca => new DcaGoalTypeEditorViewModel(),
            GoalTypeNames.IncomeFiat => _currencySettings != null
                ? new IncomeFiatGoalTypeEditorViewModel(_currencySettings)
                : new IncomeFiatGoalTypeEditorViewModel(),
            GoalTypeNames.IncomeBtc => new IncomeBtcGoalTypeEditorViewModel(),
            GoalTypeNames.ReduceExpenseCategory => _currencySettings != null && _queryDispatcher != null
                ? new ReduceExpenseCategoryGoalTypeEditorViewModel(_currencySettings, _queryDispatcher)
                : new ReduceExpenseCategoryGoalTypeEditorViewModel(),
            GoalTypeNames.BitcoinHodl => new BitcoinHodlGoalTypeEditorViewModel(),
            _ => throw new ArgumentOutOfRangeException(nameof(goalTypeName))
        };
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not null && Parameter is string goalIdString)
        {
            var goal = await _queryDispatcher!.DispatchAsync(new GetGoalQuery { GoalId = goalIdString });

            if (goal is null)
            {
                await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, language.Error_GoalNotFound, GetWindow!());
                return;
            }

            _goalId = goal.Id;
            IsEditMode = true;
            SelectedPeriod = ((GoalPeriods)goal.Period).ToString();
            SelectedYear = goal.RefDate.Year;
            SelectedMonth = goal.RefDate.Month.ToString();
            SelectedGoalType = ((GoalTypeNames)goal.GoalType.TypeId).ToString();

            CurrentGoalTypeEditor?.LoadFromDTO(goal.GoalType);
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors && CurrentGoalTypeEditor != null)
        {
            var period = Enum.Parse<GoalPeriods>(SelectedPeriod);
            var month = period == GoalPeriods.Yearly ? 1 : int.Parse(SelectedMonth);
            var refDate = new DateOnly(SelectedYear, month, 1);
            var goalTypeDto = CurrentGoalTypeEditor.CreateGoalTypeDTO();

            if (_goalId is null)
            {
                var result = await _commandDispatcher!.DispatchAsync(new CreateGoalCommand
                {
                    RefDate = refDate,
                    Period = (int)period,
                    GoalType = goalTypeDto
                });

                if (result.IsSuccess)
                {
                    CloseDialog?.Invoke(new Response(true, result.Value!.GoalId));
                }
                else
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                }
            }
            else
            {
                var result = await _commandDispatcher!.DispatchAsync(new EditGoalCommand
                {
                    GoalId = _goalId,
                    RefDate = refDate,
                    Period = (int)period,
                    GoalType = goalTypeDto
                });

                if (result.IsSuccess)
                {
                    CloseDialog?.Invoke(new Response(true, _goalId));
                }
                else
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                }
            }
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

    public record Response(bool Ok, string? GoalId = null);
}
