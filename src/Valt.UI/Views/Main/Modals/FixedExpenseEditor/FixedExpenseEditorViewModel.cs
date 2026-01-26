using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Accounts.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries;
using Valt.App.Modules.Budget.FixedExpenses.Commands.CreateFixedExpense;
using Valt.App.Modules.Budget.FixedExpenses.Commands.EditFixedExpense;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpense;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.FixedExpenseEditor;

public partial class FixedExpenseEditorViewModel : ValtModalValidatorViewModel
{
    private readonly ICommandDispatcher _commandDispatcher = null!;
    private readonly IQueryDispatcher _queryDispatcher = null!;
    private readonly DisplaySettings _displaySettings = null!;
    private readonly ITransactionTermService _transactionTermService = null!;
    private readonly IConfigurationManager? _configurationManager;

    public AvaloniaList<CategoryDTO> AvailableCategories { get; set; } = new();
    public AvaloniaList<AccountDTO> AvailableAccounts { get; set; } = new();

    [ObservableProperty] private string _windowTitle = language.FixedExpensesEditor_AddTitle;

    [ObservableProperty] private DateOnly? _lastFixedExpenseRecordReferenceDate;
    [ObservableProperty] private FixedExpenseRange? _currentFixedExpenseRange;

    // Edit mode state
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRecurrenceInfoLocked))]
    [NotifyPropertyChangedFor(nameof(ShowChangeRecurrenceButton))]
    [NotifyPropertyChangedFor(nameof(ShowCancelChangeRecurrenceButton))]
    private bool _isInChangeRecurrenceMode;

    // Store original recurrence values for cancel operation
    private string? _originalPeriod;
    private int _originalDay;
    private DateTime? _originalPeriodStart;

    public bool IsEditing => FixedExpenseId != null;
    public bool IsRecurrenceInfoLocked => IsEditing && !IsInChangeRecurrenceMode;
    public bool ShowChangeRecurrenceButton => IsEditing && !IsInChangeRecurrenceMode;
    public bool ShowCancelChangeRecurrenceButton => IsEditing && IsInChangeRecurrenceMode;

    #region Form Data

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(IsRecurrenceInfoLocked))]
    [NotifyPropertyChangedFor(nameof(ShowChangeRecurrenceButton))]
    [NotifyPropertyChangedFor(nameof(ShowCancelChangeRecurrenceButton))]
    private FixedExpenseId? _fixedExpenseId;

    [Required(ErrorMessage = "Name is required")] [ObservableProperty]
    private string _name = string.Empty;

    [Required(ErrorMessage = "Category is required")] [ObservableProperty]
    private CategoryDTO _category = null!;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsDefaultAccountSelectorVisible))]
    private bool _isAttachedToDefaultAccount = true;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsDefaultAccountSelectorVisible))]
    private bool _isAttachedToCurrency;

    [ObservableProperty] [RequiredIfAttachedToDefaultAccount]
    private AccountDTO? _defaultAccount;

    [ObservableProperty] [RequiredIfAttachedToCurrency]
    private string? _currency;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsFixedSelectorVisible))]
    private bool _isFixedAmount = true;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsFixedSelectorVisible))]
    private bool _isVariableAmount;

    [ObservableProperty] [RequiredIfFixedAmount]
    private FiatValue? _fixedAmount = FiatValue.Empty;

    [ObservableProperty] [RequiredIfVariableAmount] [RangedAmountMinLessThanMax]
    private FiatValue? _rangedAmountMin = FiatValue.Empty;

    [ObservableProperty] [RequiredIfVariableAmount] [RangedAmountMinLessThanMax]
    private FiatValue? _rangedAmountMax = FiatValue.Empty;

    [Required(ErrorMessage = "Start date is required")]
    [ObservableProperty]
    [ValidPeriodStartForExistingExpense]
    [NotifyPropertyChangedFor(nameof(PeriodStartDisplayText))]
    private DateTime? _periodStart = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayDaySelector))]
    [NotifyPropertyChangedFor(nameof(PeriodDisplayText))]
    [NotifyPropertyChangedFor(nameof(DayDisplayText))]
    private string _period = FixedExpensePeriods.Monthly.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdaptedDay))]
    [NotifyPropertyChangedFor(nameof(DayDisplayText))]
    private int _day = 5;

    public string AdaptedDay
    {
        get => Day.ToString();
        set => Day = int.Parse(value);
    }

    [ObservableProperty] private bool _enabled = true;

    [ObservableProperty] private TransactionTermResult? _transactionTermResult;

    public static List<ComboBoxValue> AvailablePeriods =>
    [
        new(language.FixedExpenses_Period_Weekly, FixedExpensePeriods.Weekly.ToString()),
        new(language.FixedExpenses_Period_Biweekly, FixedExpensePeriods.Biweekly.ToString()),
        new(language.FixedExpenses_Period_Monthly, FixedExpensePeriods.Monthly.ToString()),
        new(language.FixedExpenses_Period_Yearly, FixedExpensePeriods.Yearly.ToString()),
    ];

    public static List<ComboBoxValue> AvailableDaysOfWeek =>
    [
        new(language.DaysOfWeek_Sunday, ((int)DayOfWeek.Sunday).ToString()),
        new(language.DaysOfWeek_Monday, ((int)DayOfWeek.Monday).ToString()),
        new(language.DaysOfWeek_Tuesday, ((int)DayOfWeek.Tuesday).ToString()),
        new(language.DaysOfWeek_Wednesday, ((int)DayOfWeek.Wednesday).ToString()),
        new(language.DaysOfWeek_Thursday, ((int)DayOfWeek.Thursday).ToString()),
        new(language.DaysOfWeek_Friday, ((int)DayOfWeek.Friday).ToString()),
        new(language.DaysOfWeek_Saturday, ((int)DayOfWeek.Saturday).ToString())
    ];

    public List<string> AvailableCurrencies => _configurationManager?.GetAvailableFiatCurrencies()
        ?? FiatCurrency.GetAll().Select(x => x.Code).ToList();

    public bool IsDefaultAccountSelectorVisible => IsAttachedToDefaultAccount;

    public bool IsFixedSelectorVisible => IsFixedAmount;

    public bool DisplayDaySelector => Period == FixedExpensePeriods.Monthly.ToString() ||
                                      Period == FixedExpensePeriods.Yearly.ToString();

    // Display properties for read-only mode
    public string PeriodDisplayText => Period switch
    {
        nameof(FixedExpensePeriods.Weekly) => language.FixedExpenses_Period_Weekly,
        nameof(FixedExpensePeriods.Biweekly) => language.FixedExpenses_Period_Biweekly,
        nameof(FixedExpensePeriods.Monthly) => language.FixedExpenses_Period_Monthly,
        nameof(FixedExpensePeriods.Yearly) => language.FixedExpenses_Period_Yearly,
        _ => Period
    };

    public string DayDisplayText => DisplayDaySelector
        ? Day.ToString()
        : ((DayOfWeek)Day) switch
        {
            DayOfWeek.Sunday => language.DaysOfWeek_Sunday,
            DayOfWeek.Monday => language.DaysOfWeek_Monday,
            DayOfWeek.Tuesday => language.DaysOfWeek_Tuesday,
            DayOfWeek.Wednesday => language.DaysOfWeek_Wednesday,
            DayOfWeek.Thursday => language.DaysOfWeek_Thursday,
            DayOfWeek.Friday => language.DaysOfWeek_Friday,
            DayOfWeek.Saturday => language.DaysOfWeek_Saturday,
            _ => Day.ToString()
        };

    public string PeriodStartDisplayText => PeriodStart?.ToString("d") ?? string.Empty;

    #endregion

    public FixedExpenseEditorViewModel()
    {
        if (!Design.IsDesignMode) return;
    }

    public FixedExpenseEditorViewModel(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        DisplaySettings displaySettings,
        ITransactionTermService transactionTermService,
        IConfigurationManager configurationManager)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _displaySettings = displaySettings;
        _transactionTermService = transactionTermService;
        _configurationManager = configurationManager;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await FetchCategoriesAsync();
        await FetchAccountsAsync();
    }

    private async Task FetchCategoriesAsync()
    {
        var result = await _queryDispatcher.DispatchAsync(new GetCategoriesQuery());
        var categories = result.Items.OrderBy(x => x.Name);

        AvailableCategories.Clear();
        foreach (var category in categories)
            AvailableCategories.Add(category);
    }

    private async Task FetchAccountsAsync()
    {
        var accounts = await _queryDispatcher.DispatchAsync(
            new GetAccountsQuery(_displaySettings.ShowHiddenAccounts));

        AvailableAccounts.Clear();
        foreach (var account in accounts)
            if (!account.IsBtcAccount)
                AvailableAccounts.Add(account);
    }

    partial void OnTransactionTermResultChanged(TransactionTermResult? value)
    {
        if (value is null)
            return;

        if (value.Name != Name)
            return;

        var selectedCategory = AvailableCategories.SingleOrDefault(x => x.Id == value.CategoryId);
        if (selectedCategory is not null)
        {
            Category = selectedCategory;
        }
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not Request request)
            return;

        if (request.FixedExpenseId is not null)
        {
            var fixedExpense = await _queryDispatcher.DispatchAsync(
                new GetFixedExpenseQuery { FixedExpenseId = request.FixedExpenseId.Value });

            if (fixedExpense is null)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_FixedExpenseNotFound, GetWindow!());
                Close();
                return;
            }

            FixedExpenseId = request.FixedExpenseId;

            WindowTitle = language.FixedExpensesEditor_EditTitle;

            Name = fixedExpense.Name;
            Category = AvailableCategories.FirstOrDefault(c => c.Id == fixedExpense.CategoryId)!;

            IsAttachedToDefaultAccount = fixedExpense.DefaultAccountId is not null;
            IsAttachedToCurrency = fixedExpense.Currency is not null;
            DefaultAccount = fixedExpense.DefaultAccountId is not null
                ? AvailableAccounts.FirstOrDefault(a => a.Id == fixedExpense.DefaultAccountId)
                : null;
            Currency = fixedExpense.Currency;

            var latestRange = fixedExpense.LatestRange;

            Period = ((FixedExpensePeriods)latestRange.PeriodId).ToString();
            Day = latestRange.Day;
            PeriodStart = latestRange.PeriodStart.ToDateTime(TimeOnly.MinValue);
            Enabled = fixedExpense.Enabled;

            IsFixedAmount = latestRange.FixedAmount is not null;
            IsVariableAmount = latestRange.RangedAmountMin is not null;
            if (latestRange.RangedAmountMin is not null)
            {
                RangedAmountMin = FiatValue.New(latestRange.RangedAmountMin.Value);
                RangedAmountMax = FiatValue.New(latestRange.RangedAmountMax!.Value);
            }
            else
            {
                FixedAmount = latestRange.FixedAmount.HasValue ? FiatValue.New(latestRange.FixedAmount.Value) : null;
            }
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (HasErrors)
            return;

        var periodId = (int)Enum.Parse<FixedExpensePeriods>(Period);
        var periodStart = DateOnly.FromDateTime(PeriodStart!.Value);

        if (FixedExpenseId != null)
        {
            var command = new EditFixedExpenseCommand
            {
                FixedExpenseId = FixedExpenseId.Value,
                Name = Name,
                CategoryId = Category.Id,
                DefaultAccountId = IsDefaultAccountSelectorVisible ? DefaultAccount?.Id : null,
                Currency = IsAttachedToCurrency ? Currency : null,
                Enabled = Enabled,
                NewRange = new FixedExpenseRangeInputDTO
                {
                    PeriodStart = periodStart,
                    PeriodId = periodId,
                    Day = Day,
                    FixedAmount = IsFixedSelectorVisible ? FixedAmount?.Value : null,
                    RangedAmountMin = IsVariableAmount ? RangedAmountMin?.Value : null,
                    RangedAmountMax = IsVariableAmount ? RangedAmountMax?.Value : null
                }
            };
            var result = await _commandDispatcher.DispatchAsync(command);
            if (result.IsFailure)
            {
                await ResultExtensions.ShowErrorAsync(result.Error!, GetWindow!());
                return;
            }
        }
        else
        {
            var command = new CreateFixedExpenseCommand
            {
                Name = Name,
                CategoryId = Category.Id,
                DefaultAccountId = IsDefaultAccountSelectorVisible ? DefaultAccount?.Id : null,
                Currency = IsAttachedToCurrency ? Currency : null,
                Enabled = Enabled,
                Ranges = new List<FixedExpenseRangeInputDTO>
                {
                    new()
                    {
                        PeriodStart = periodStart,
                        PeriodId = periodId,
                        Day = Day,
                        FixedAmount = IsFixedSelectorVisible ? FixedAmount?.Value : null,
                        RangedAmountMin = IsVariableAmount ? RangedAmountMin?.Value : null,
                        RangedAmountMax = IsVariableAmount ? RangedAmountMax?.Value : null
                    }
                }
            };
            var result = await _commandDispatcher.DispatchAsync(command);
            if (result.IsFailure)
            {
                await ResultExtensions.ShowErrorAsync(result.Error!, GetWindow!());
                return;
            }
        }

        CloseDialog?.Invoke(new Response(true));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    [RelayCommand]
    private void Close()
    {
        CloseWindow?.Invoke();
    }

    [RelayCommand]
    private void EnterChangeRecurrenceMode()
    {
        // Store original values for cancel operation
        _originalPeriod = Period;
        _originalDay = Day;
        _originalPeriodStart = PeriodStart;

        // Set the minimum valid date for the new period start
        // Must be after the last recorded transaction
        if (LastFixedExpenseRecordReferenceDate.HasValue)
        {
            PeriodStart = LastFixedExpenseRecordReferenceDate.Value.AddDays(1).ToValtDateTime();
        }

        IsInChangeRecurrenceMode = true;
    }

    [RelayCommand]
    private void CancelChangeRecurrenceMode()
    {
        // Restore original values
        Period = _originalPeriod ?? Period;
        Day = _originalDay;
        PeriodStart = _originalPeriodStart;

        IsInChangeRecurrenceMode = false;
    }

    public Task<IEnumerable<object>> GetTransactionTermsAsync(string? term, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(term)
            ? Task.FromResult(Enumerable.Empty<object>())
            : Task.FromResult<IEnumerable<object>>(_transactionTermService!.Search(term, 5));
    }

    public record Request
    {
        public FixedExpenseId? FixedExpenseId { get; init; }
    }

    public record Response(bool Ok);
}