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
using Valt.Infra.Modules.Configuration;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ManageGoal;

public partial class ManageGoalViewModel : ValtModalValidatorViewModel
{
    private readonly IGoalRepository? _goalRepository;
    private readonly IConfigurationManager? _configurationManager;

    #region Form Data

    private GoalId? _goalId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMonthSelector))]
    private string _selectedPeriod = GoalPeriods.Monthly.ToString();

    [ObservableProperty]
    private string _selectedMonth = DateTime.Today.Month.ToString();

    [ObservableProperty]
    private int _selectedYear = DateTime.Today.Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStackBitcoinInput))]
    [NotifyPropertyChangedFor(nameof(ShowSpendingLimitInput))]
    [NotifyPropertyChangedFor(nameof(ShowDcaInput))]
    private string _selectedGoalType = GoalTypeNames.StackBitcoin.ToString();

    [ObservableProperty]
    private BtcValue _targetBtcAmount = BtcValue.Empty;

    [ObservableProperty]
    private FiatValue _targetFiatAmount = FiatValue.Empty;

    [ObservableProperty]
    private string _selectedCurrency = FiatCurrency.Usd.Code;

    [ObservableProperty]
    private int _targetPurchaseCount = 4;

    [ObservableProperty]
    private bool _isEditMode;

    public bool ShowMonthSelector => SelectedPeriod == GoalPeriods.Monthly.ToString();

    public bool ShowStackBitcoinInput => SelectedGoalType == GoalTypeNames.StackBitcoin.ToString();

    public bool ShowSpendingLimitInput => SelectedGoalType == GoalTypeNames.SpendingLimit.ToString();

    public bool ShowDcaInput => SelectedGoalType == GoalTypeNames.Dca.ToString();

    public static List<ComboBoxValue> AvailablePeriods =>
    [
        new(language.GoalPeriod_Monthly, GoalPeriods.Monthly.ToString()),
        new(language.GoalPeriod_Yearly, GoalPeriods.Yearly.ToString())
    ];

    public static List<ComboBoxValue> AvailableGoalTypes =>
    [
        new(language.GoalType_StackBitcoin, GoalTypeNames.StackBitcoin.ToString()),
        new(language.GoalType_SpendingLimit, GoalTypeNames.SpendingLimit.ToString()),
        new(language.GoalType_Dca, GoalTypeNames.Dca.ToString())
    ];

    public List<ComboBoxValue> AvailableCurrencies =>
        (_configurationManager?.GetAvailableFiatCurrencies() ?? FiatCurrency.GetAll().Select(c => c.Code).ToList())
            .Select(c => new ComboBoxValue(c, c))
            .ToList();

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
        TargetBtcAmount = BtcValue.New(1_000_000);
    }

    public ManageGoalViewModel(IGoalRepository goalRepository, IConfigurationManager configurationManager)
    {
        _goalRepository = goalRepository;
        _configurationManager = configurationManager;

        SelectedPeriod = GoalPeriods.Monthly.ToString();
        SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();
        SelectedCurrency = AvailableCurrencies.FirstOrDefault()?.Value ?? FiatCurrency.Usd.Code;
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
            SelectedPeriod = goal.Period.ToString();
            SelectedYear = goal.RefDate.Year;
            SelectedMonth = goal.RefDate.Month.ToString();
            SelectedGoalType = goal.GoalType.TypeName.ToString();

            switch (goal.GoalType)
            {
                case StackBitcoinGoalType stackBitcoin:
                    TargetBtcAmount = stackBitcoin.TargetAmount;
                    break;
                case SpendingLimitGoalType spendingLimit:
                    TargetFiatAmount = FiatValue.New(spendingLimit.TargetAmount);
                    SelectedCurrency = spendingLimit.Currency;
                    break;
                case DcaGoalType dca:
                    TargetPurchaseCount = dca.TargetPurchaseCount;
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
            var period = Enum.Parse<GoalPeriods>(SelectedPeriod);
            var month = period == GoalPeriods.Yearly ? 1 : int.Parse(SelectedMonth);
            var refDate = new DateOnly(SelectedYear, month, 1);

            Goal goal;
            if (_goalId is null)
            {
                var goalTypeName = Enum.Parse<GoalTypeNames>(SelectedGoalType);
                IGoalType goalType = goalTypeName switch
                {
                    GoalTypeNames.StackBitcoin => new StackBitcoinGoalType(TargetBtcAmount),
                    GoalTypeNames.SpendingLimit => new SpendingLimitGoalType(TargetFiatAmount.Value, SelectedCurrency),
                    GoalTypeNames.Dca => new DcaGoalType(TargetPurchaseCount),
                    _ => throw new ArgumentOutOfRangeException()
                };

                goal = Goal.New(refDate, period, goalType);
            }
            else
            {
                var existingGoal = await _goalRepository!.GetByIdAsync(_goalId);

                if (existingGoal is null)
                    throw new EntityNotFoundException(nameof(Goal), _goalId);

                // Preserve calculated values from existing goal type when editing
                var goalTypeName = Enum.Parse<GoalTypeNames>(SelectedGoalType);
                IGoalType goalType = goalTypeName switch
                {
                    GoalTypeNames.StackBitcoin => existingGoal.GoalType is StackBitcoinGoalType existing
                        ? new StackBitcoinGoalType(TargetBtcAmount.Sats, existing.CalculatedSats)
                        : new StackBitcoinGoalType(TargetBtcAmount),
                    GoalTypeNames.SpendingLimit => existingGoal.GoalType is SpendingLimitGoalType existingSpending
                        ? new SpendingLimitGoalType(TargetFiatAmount.Value, SelectedCurrency, existingSpending.CalculatedSpending)
                        : new SpendingLimitGoalType(TargetFiatAmount.Value, SelectedCurrency),
                    GoalTypeNames.Dca => existingGoal.GoalType is DcaGoalType existingDca
                        ? new DcaGoalType(TargetPurchaseCount, existingDca.CalculatedPurchaseCount)
                        : new DcaGoalType(TargetPurchaseCount),
                    _ => throw new ArgumentOutOfRangeException()
                };

                goal = Goal.Create(
                    existingGoal.Id,
                    refDate,
                    period,
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
