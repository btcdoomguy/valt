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
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Infra;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Modules.Budget.Categories.Queries.DTOs;
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
    private readonly IFixedExpenseRepository _fixedExpenseRepository;
    private readonly IAccountQueries _accountQueries;
    private readonly ICategoryQueries _categoryQueries;
    private readonly DisplaySettings _displaySettings;
    private readonly ITransactionTermService _transactionTermService;
    private readonly ConfigurationManager? _configurationManager;

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
    private CategoryDTO _category;

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

    public FixedExpenseEditorViewModel(IFixedExpenseRepository fixedExpenseRepository,
        IAccountQueries accountQueries,
        ICategoryQueries categoryQueries,
        DisplaySettings displaySettings,
        ITransactionTermService transactionTermService,
        ConfigurationManager configurationManager)
    {
        _fixedExpenseRepository = fixedExpenseRepository;
        _accountQueries = accountQueries;
        _categoryQueries = categoryQueries;
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
        var categories = (await _categoryQueries!.GetCategoriesAsync()).Items.OrderBy(x => x.Name);

        AvailableCategories.Clear();
        foreach (var category in categories)
            AvailableCategories.Add(category);
    }

    private async Task FetchAccountsAsync()
    {
        var accounts = await _accountQueries!.GetAccountsAsync(_displaySettings.ShowHiddenAccounts);

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
            var fixedExpense =
                await _fixedExpenseRepository!.GetFixedExpenseByIdAsync(request.FixedExpenseId.Value);

            if (fixedExpense is null)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_FixedExpenseNotFound, GetWindow!());
                Close();
                return;
            }

            FixedExpenseId = request.FixedExpenseId;
            LastFixedExpenseRecordReferenceDate = fixedExpense.LastFixedExpenseRecordDate;
            CurrentFixedExpenseRange = fixedExpense.CurrentRange;

            WindowTitle = language.FixedExpensesEditor_EditTitle;

            Name = fixedExpense.Name;
            Category = AvailableCategories.FirstOrDefault(c => c.Id == fixedExpense.CategoryId.Value);

            IsAttachedToDefaultAccount = fixedExpense.DefaultAccountId is not null;
            IsAttachedToCurrency = fixedExpense.Currency is not null;
            DefaultAccount = fixedExpense.DefaultAccountId is not null
                ? AvailableAccounts.FirstOrDefault(a => a.Id == fixedExpense.DefaultAccountId)
                : null;
            Currency = fixedExpense.Currency?.Code;

            var latestRange = fixedExpense.Ranges.LastOrDefault();

            Period = latestRange.Period.ToString();
            Day = latestRange.Day;
            PeriodStart = latestRange.PeriodStart.ToValtDateTime();
            Enabled = fixedExpense.Enabled;

            IsFixedAmount = latestRange.FixedAmount is not null;
            IsVariableAmount = latestRange.RangedAmount is not null;
            if (latestRange.RangedAmount is not null)
            {
                RangedAmountMin = latestRange.RangedAmount.Min;
                RangedAmountMax = latestRange.RangedAmount.Max;
            }
            else
            {
                FixedAmount = latestRange.FixedAmount;
            }
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (HasErrors)
            return;

        FixedExpense? fixedExpense;
        try
        {
            if (FixedExpenseId != null)
            {
                fixedExpense = await _fixedExpenseRepository!.GetFixedExpenseByIdAsync(FixedExpenseId);

                await EditAsync(fixedExpense!);
            }
            else
            {
                fixedExpense = await CreateAsync();
            }
        }
        catch (Exception e)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, e.Message, GetWindow!());
            return;
        }

        await _fixedExpenseRepository!.SaveFixedExpenseAsync(fixedExpense!);
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

    private async Task<FixedExpense> CreateAsync()
    {
        var name = FixedExpenseName.New(Name);
        var date = DateOnly.FromDateTime(PeriodStart!.Value);

        FixedExpenseRange initialRange;
        if (IsFixedSelectorVisible)
            initialRange =
                FixedExpenseRange.CreateFixedAmount(FixedAmount, Enum.Parse<FixedExpensePeriods>(Period), date, Day);
        else
            initialRange = FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(RangedAmountMin, RangedAmountMax),
                Enum.Parse<FixedExpensePeriods>(Period),
                date,
                Day);

        return FixedExpense.New(name,
            IsDefaultAccountSelectorVisible ? new AccountId(DefaultAccount.Id) : null,
            new CategoryId(Category.Id),
            Currency is not null ? FiatCurrency.GetFromCode(Currency) : null,
            new List<FixedExpenseRange> { initialRange },
            Enabled);
    }

    private Task EditAsync(FixedExpense fixedExpense)
    {
        var name = FixedExpenseName.New(Name);
        var date = DateOnly.FromDateTime(PeriodStart!.Value);

        fixedExpense.Rename(name);
        fixedExpense.SetCategory(fixedExpense.CategoryId.Value);
        fixedExpense.SetEnabled(Enabled);

        if (IsDefaultAccountSelectorVisible)
        {
            fixedExpense.SetDefaultAccountId(DefaultAccount!.Id);
        }
        else
        {
            fixedExpense.SetCurrency(FiatCurrency.GetFromCode(Currency!));
        }

        FixedExpenseRange newRange;
        if (IsFixedSelectorVisible)
            newRange =
                FixedExpenseRange.CreateFixedAmount(FixedAmount, Enum.Parse<FixedExpensePeriods>(Period), date, Day);
        else
            newRange = FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(RangedAmountMin, RangedAmountMax),
                Enum.Parse<FixedExpensePeriods>(Period),
                date,
                Day);

        if (!fixedExpense.ContainsRange(newRange))
            fixedExpense.AddRange(newRange);

        return Task.CompletedTask;
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