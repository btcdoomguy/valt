using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State;
using Valt.UI.Views.Main.Modals.ManageAccount;
using Valt.UI.Views.Main.Modals.FixedExpenseEditor;
using Valt.UI.Views.Main.Modals.ManageFixedExpenses;
using Valt.UI.Views.Main.Modals.TransactionEditor;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionsViewModel : ValtTabViewModel, IDisposable
{
    private readonly IModalFactory? _modalFactory;
    private readonly IAccountRepository? _accountRepository;
    private readonly IFixedExpenseProvider? _fixedExpenseProvider;
    private readonly AccountsTotalState? _accountsTotalState;
    private readonly RatesState _ratesState;
    private readonly CurrencySettings? _currencySettings;
    private readonly DisplaySettings? _displaySettings;
    private readonly AccountDisplayOrderManager? _accountDisplayOrderManager;
    private readonly FilterState? _filterState;
    private readonly IFixedExpenseRecordService? _fixedExpenseRecordService;
    private readonly IClock _clock;
    private readonly ILogger<TransactionsViewModel> _logger;
    private readonly IAccountQueries? _accountQueries;

    //instances of the sub contents
    private readonly TransactionListViewModel _transactionListViewModel = null!;

    [ObservableProperty] private AvaloniaList<AccountViewModel> _accounts = new();
    [ObservableProperty] private AvaloniaList<FixedExpensesEntryViewModel> _fixedExpenseEntries = new();
    [ObservableProperty] private string _remainingFixedExpensesAmount = "~ R$ 12.345,67";
    [ObservableProperty] private string? _remainingFixedExpensesTooltip;

    [ObservableProperty] private AccountViewModel? _selectedAccount;
    [ObservableProperty] private FixedExpensesEntryViewModel? _selectedFixedExpense;

    [ObservableProperty] private string _allWealthInSats = "12.34567890";
    [ObservableProperty] private string _wealthInBtcRatio = "65.3%";
    [ObservableProperty] private string _wealthInSats = "11.34567890";
    [ObservableProperty] private string _wealthNotInSats = "1.00000000";
    [ObservableProperty] private string _wealthInFiat = "R$ 12.000.000,00";
    [ObservableProperty] private string _allWealthInFiat = "R$ 13.000.000,00";

    [ObservableProperty] private FiatCurrency _mainFiatCurrency = FiatCurrency.Brl;

    [ObservableProperty] private ValtViewModel? _subContent;

    public TransactionsViewModel(IAccountQueries accountQueries,
        IModalFactory modalFactory, IAccountRepository accountRepository,
        IFixedExpenseProvider fixedExpenseProvider,
        AccountsTotalState accountsTotalState,
        RatesState ratesState,
        CurrencySettings currencySettings,
        DisplaySettings displaySettings,
        AccountDisplayOrderManager accountDisplayOrderManager,
        FilterState filterState,
        ITransactionTabFactory transactionTabFactory,
        IFixedExpenseRecordService fixedExpenseRecordService,
        IClock clock,
        ILogger<TransactionsViewModel> logger)
    {
        _accountQueries = accountQueries;
        _modalFactory = modalFactory;
        _accountRepository = accountRepository;
        _fixedExpenseProvider = fixedExpenseProvider;
        _accountsTotalState = accountsTotalState;
        _ratesState = ratesState;
        _currencySettings = currencySettings;
        _displaySettings = displaySettings;
        _accountDisplayOrderManager = accountDisplayOrderManager;
        _filterState = filterState;
        _fixedExpenseRecordService = fixedExpenseRecordService;
        _clock = clock;
        _logger = logger;

        _transactionListViewModel = (TransactionListViewModel)transactionTabFactory.Create(TransactionsTabNames.List);

        _accountsTotalState.PropertyChanged += AccountsTotalStateOnPropertyChanged;
        _filterState.PropertyChanged += FilterStateOnPropertyChanged;

        _ = InitializeAsync();

        MainFiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);

        WeakReferenceMessenger.Default.Register<TransactionListChanged>(this, OnTransactionListChangedReceive);
        WeakReferenceMessenger.Default.Register<FilterDateRangeChanged>(this, OnCurrentDateRangeChangedReceive);

        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            switch (message.Value)
            {
                case nameof(CurrencySettings.MainFiatCurrency):
                    MainFiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);
                    break;
                case nameof(DisplaySettings.ShowHiddenAccounts):
                    _ = FetchAccounts();
                    break;
            }
        });
    }
    
    private void OnCurrentDateRangeChangedReceive(object recipient, FilterDateRangeChanged message)
    {
        _ = FetchFixedExpenses();
    }

    private void OnTransactionListChangedReceive(object recipient, TransactionListChanged message)
    {
        _ = FetchAccounts();
        _ = FetchFixedExpenses();
    }

    private void FilterStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(FixedExpenseCurrentMonthDescription));
    }

    private void AccountsTotalStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_accountsTotalState is null) return;
            if (_currencySettings is null) return;

            AllWealthInSats = CurrencyDisplay.FormatSatsAsBitcoin(_accountsTotalState.CurrentWealth.AllWealthInSats);
            WealthInSats = CurrencyDisplay.FormatSatsAsBitcoin(_accountsTotalState.CurrentWealth.WealthInSats);
            WealthNotInSats =CurrencyDisplay.FormatSatsAsBitcoin(_accountsTotalState.CurrentWealth.AllWealthInSats -
                                                                 _accountsTotalState.CurrentWealth.WealthInSats);
            AllWealthInFiat = CurrencyDisplay.FormatFiat(_accountsTotalState.CurrentWealth.AllWealthInMainFiatCurrency,
                _currencySettings.MainFiatCurrency);
            WealthInFiat = CurrencyDisplay.FormatFiat(_accountsTotalState.CurrentWealth.WealthInMainFiatCurrency,
                _currencySettings.MainFiatCurrency);
            WealthInBtcRatio =
                _accountsTotalState.CurrentWealth.WealthInBtcRatio.ToString(CultureInfo.InvariantCulture) + "%";
        });
    }

    private async Task InitializeAsync()
    {
        SubContent = _transactionListViewModel;

        if (_accountDisplayOrderManager is null) return;

        await _accountDisplayOrderManager.NormalizeDisplayOrdersAsync(null);

        Dispatcher.UIThread.Post(() =>
        {
            _ = FetchAccounts();
            _ = FetchFixedExpenses();
            _transactionListViewModel.FetchTransactionsCommand.Execute(null);

            SelectedAccount = Accounts.FirstOrDefault();
        });
    }

    private async Task FetchAccounts()
    {
        if (_accountQueries is null) return;
        if (_displaySettings is null) return;

        var currentSelectedAccountId = SelectedAccount?.Id;

        var accounts = await _accountQueries.GetAccountSummariesAsync(_displaySettings.ShowHiddenAccounts);

        WeakReferenceMessenger.Default.Send(accounts);

        Accounts.Clear();
        Accounts.AddRange(accounts.Items.Select(x => new AccountViewModel(x)));

        if (currentSelectedAccountId is not null)
            SelectedAccount =
                Accounts.SingleOrDefault(x => x.Id == currentSelectedAccountId);
    }

    private async Task FetchFixedExpenses()
    {
        if (_filterState is null) return;
        if (_fixedExpenseProvider is null) return;

        try
        {
            var value = DateOnly.FromDateTime(_filterState.MainDate);
            var fixedExpenses = await _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(value);

            fixedExpenses = fixedExpenses.OrderBy(x => x.State).ThenBy(x => x.ReferenceDate).ToList();

            var minTotal = 0m;
            var maxTotal = 0m;

            var fixedExpenseHelper = new FixedExpenseHelper(_ratesState, _currencySettings);

            FixedExpenseEntries.Clear();
            foreach (var fixedExpense in fixedExpenses)
            {
                FixedExpenseEntries.Add(new FixedExpensesEntryViewModel(fixedExpense, _clock.GetCurrentLocalDate()));

                if (fixedExpense.State != FixedExpenseRecordState.Empty)
                    continue;

                var (fixedAmountMin, fixedAmountMax) = fixedExpenseHelper.CalculateFixedExpenseRange(
                    fixedExpense.FixedAmount,
                    fixedExpense.RangedAmountMin, fixedExpense.RangedAmountMax,
                    fixedExpense.Currency);

                minTotal += fixedAmountMin;
                maxTotal += fixedAmountMax;
            }

            if (minTotal == maxTotal)
            {
                RemainingFixedExpensesAmount =
                    $"{CurrencyDisplay.FormatFiat(minTotal, _currencySettings.MainFiatCurrency)}";
                RemainingFixedExpensesTooltip = null;
            }
            else
            {
                var middle = (maxTotal + minTotal) / 2;

                RemainingFixedExpensesAmount =
                    $"~ {CurrencyDisplay.FormatFiat(middle, _currencySettings.MainFiatCurrency)}";
                RemainingFixedExpensesTooltip =
                    $"{CurrencyDisplay.FormatFiat(minTotal, _currencySettings.MainFiatCurrency)} - {CurrencyDisplay.FormatFiat(maxTotal, _currencySettings.MainFiatCurrency)}";
            }
                
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching fixed expenses");
        }
    }

    #region Account operations

    [RelayCommand]
    private async Task AddAccount()
    {
        var ownerWindow = GetUserControlOwnerWindow!();

        var window =
            (ManageAccountView)await _modalFactory!.CreateAsync(ApplicationModalNames.ManageAccount, ownerWindow)!;

        var result = await window.ShowDialog<ManageAccountViewModel.Response?>(ownerWindow!);

        if (result is null)
            return;

        await _accountDisplayOrderManager!.NormalizeDisplayOrdersAsync(null);
        await FetchAccounts();
    }

    [RelayCommand]
    private async Task EditAccount(AccountViewModel selectedAccount)
    {
        var ownerWindow = GetUserControlOwnerWindow!();

        var window = (ManageAccountView)await _modalFactory!.CreateAsync(ApplicationModalNames.ManageAccount,
            ownerWindow, selectedAccount.Id)!;

        var result = await window.ShowDialog<ManageAccountViewModel.Response?>(ownerWindow!);

        if (result is null)
            return;

        await _accountDisplayOrderManager!.NormalizeDisplayOrdersAsync(null);
        await FetchAccounts();
    }

    [RelayCommand]
    private async Task HideAccount(AccountViewModel selectedAccount)
    {
        var account = await _accountRepository!.GetAccountByIdAsync(selectedAccount.Id);

        if (account is null)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_AccountNotFound, GetUserControlOwnerWindow()!);
            return;
        }

        account.ChangeVisibility(false);

        await _accountRepository.SaveAccountAsync(account);

        await _accountDisplayOrderManager!.NormalizeDisplayOrdersAsync(null);
        await FetchAccounts();
    }

    [RelayCommand]
    private async Task ShowAccount(AccountViewModel selectedAccount)
    {
        var account = await _accountRepository!.GetAccountByIdAsync(selectedAccount.Id);

        if (account is null)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_AccountNotFound, GetUserControlOwnerWindow()!);
            return;
        }

        account.ChangeVisibility(true);

        await _accountRepository.SaveAccountAsync(account);

        await _accountDisplayOrderManager!.NormalizeDisplayOrdersAsync(null);
        await FetchAccounts();
    }

    [RelayCommand]
    private async Task DeleteAccount(AccountViewModel selectedAccount)
    {
        var account = await _accountRepository!.GetAccountByIdAsync(selectedAccount.Id);

        if (account is null)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_AccountNotFound, GetUserControlOwnerWindow()!);
            return;
        }

        try
        {
            await _accountRepository.DeleteAccountAsync(account);
        }
        catch (Exception e)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, e.Message, GetUserControlOwnerWindow()!);
        }

        await _accountDisplayOrderManager!.NormalizeDisplayOrdersAsync(null);
        await FetchAccounts();
        _transactionListViewModel.FetchTransactionsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task MoveUpAccount(AccountViewModel selectedAccount)
    {
        var account = await _accountRepository!.GetAccountByIdAsync(selectedAccount.Id);

        if (account is null)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_AccountNotFound, GetUserControlOwnerWindow()!);
            return;
        }

        await _accountDisplayOrderManager!.NormalizeDisplayOrdersAsync(
            new AccountOrderAction(new AccountId(selectedAccount.Id), true));

        await FetchAccounts();
    }

    [RelayCommand]
    private async Task MoveDownAccount(AccountViewModel selectedAccount)
    {
        var account = await _accountRepository!.GetAccountByIdAsync(selectedAccount.Id);

        if (account is null)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_AccountNotFound, GetUserControlOwnerWindow()!);
            return;
        }

        await _accountDisplayOrderManager!.NormalizeDisplayOrdersAsync(
            new AccountOrderAction(new AccountId(selectedAccount.Id), false));

        await FetchAccounts();
    }

    [RelayCommand]
    private Task SelectAllAccounts()
    {
        SelectedAccount = null;
        return Task.CompletedTask;
    }

    #endregion

    #region Fixed Expense operations

    public string FixedExpenseCurrentMonthDescription =>
        $"({DateOnly.FromDateTime(_filterState!.MainDate).ToString("MM/yy")})";

    [RelayCommand]
    private async Task ManageFixedExpenses()
    {
        var ownerWindow = GetUserControlOwnerWindow!();

        var window = (ManageFixedExpensesView)await _modalFactory!.CreateAsync(
            ApplicationModalNames.ManageFixedExpenses,
            ownerWindow, null)!;

        _ = await window.ShowDialog<ManageFixedExpensesViewModel.Response?>(ownerWindow!);

        await _accountDisplayOrderManager!.NormalizeDisplayOrdersAsync(null);
        await FetchAccounts();
        await FetchFixedExpenses();
    }

    [RelayCommand]
    private async Task EditFixedExpense(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow!();

        var window = (FixedExpenseEditorView)await _modalFactory!.CreateAsync(
            ApplicationModalNames.FixedExpenseEditor,
            ownerWindow, new FixedExpenseEditorViewModel.Request()
            {
                FixedExpenseId = entry.Id
            })!;

        _ = await window.ShowDialog<FixedExpenseEditorViewModel.Response?>(ownerWindow!);

        await FetchFixedExpenses();
    }

    [RelayCommand]
    private async Task OpenFixedExpense()
    {
        if (SelectedFixedExpense is null)
            return;

        TransactionEditorViewModel.Request request;
        if (SelectedFixedExpense.Paid)
        {
            request = new TransactionEditorViewModel.Request()
            {
                TransactionId = new TransactionId(SelectedFixedExpense.TransactionId!)
            };
        }
        else
        {
            request = new TransactionEditorViewModel.Request()
            {
                Date = DateTime.Now,
                AccountId = SelectedFixedExpense.DefaultAccountId is not null
                    ? new AccountId(SelectedFixedExpense.DefaultAccountId)
                    : null,
                CopyTransaction = false,
                DefaultFromFiatValue = SelectedFixedExpense.FixedAmount is not null
                    ? FiatValue.New(SelectedFixedExpense.FixedAmount)
                    : null,
                FixedExpenseReference =
                    new TransactionFixedExpenseReference(SelectedFixedExpense.Id, SelectedFixedExpense.ReferenceDate),
                Name = SelectedFixedExpense.Name,
                CategoryId = new CategoryId(SelectedFixedExpense!.CategoryId!)
            };
        }

        var ownerWindow = GetUserControlOwnerWindow()!;

        var modal =
            (TransactionEditorView)await _modalFactory!.CreateAsync(ApplicationModalNames.TransactionEditor,
                ownerWindow,
                request)!;

        var result = await modal.ShowDialog<TransactionEditorViewModel.Response?>(ownerWindow);

        if (result is null)
            return;

        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        await FetchFixedExpenses();
    }

    [RelayCommand(CanExecute = nameof(CanIgnoreFixedExpense))]
    public async Task IgnoreFixedExpense(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        await _fixedExpenseRecordService!.IgnoreFixedExpenseAsync(
            new TransactionFixedExpenseReference(entry.Id, entry.ReferenceDate));
        await FetchFixedExpenses();
    }

    public bool CanIgnoreFixedExpense(FixedExpensesEntryViewModel? entry) =>
        entry?.State == FixedExpenseRecordState.Empty;

    [RelayCommand(CanExecute = nameof(CanMarkFixedExpenseAsPaid))]
    public async Task MarkFixedExpenseAsPaid(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        await _fixedExpenseRecordService!.MarkFixedExpenseAsPaidAsync(
            new TransactionFixedExpenseReference(entry.Id, entry.ReferenceDate));
        await FetchFixedExpenses();
    }

    public bool CanMarkFixedExpenseAsPaid(FixedExpensesEntryViewModel? entry) =>
        entry?.State is FixedExpenseRecordState.Empty or FixedExpenseRecordState.Ignored;

    [RelayCommand(CanExecute = nameof(CanUndoIgnoreFixedExpense))]
    public async Task UndoIgnoreFixedExpense(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        await _fixedExpenseRecordService!.UndoIgnoreFixedExpenseAsync(
            new TransactionFixedExpenseReference(entry.Id, entry.ReferenceDate));
        await FetchFixedExpenses();
    }

    public bool CanUndoIgnoreFixedExpense(FixedExpensesEntryViewModel? entry) =>
        entry?.State is FixedExpenseRecordState.Ignored;

    [RelayCommand(CanExecute = nameof(CanUnmarkFixedExpenseAsPaid))]
    public async Task UnmarkFixedExpenseAsPaid(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        await _fixedExpenseRecordService!.UnmarkFixedExpenseAsPaidAsync(
            new TransactionFixedExpenseReference(entry.Id, entry.ReferenceDate));
        await FetchFixedExpenses();
    }

    public bool CanUnmarkFixedExpenseAsPaid(FixedExpensesEntryViewModel? entry) =>
        entry?.State is FixedExpenseRecordState.ManuallyPaid;

    #endregion

    partial void OnSelectedAccountChanged(AccountViewModel? value)
    {
        WeakReferenceMessenger.Default.Send(new AccountSelectedChanged(value));
    }
    
    partial void OnSelectedFixedExpenseChanged(FixedExpensesEntryViewModel? value)
    {
        WeakReferenceMessenger.Default.Send(new FixedExpenseChanged(value));
    }

    public void Dispose()
    {
        if (_accountsTotalState is not null)
            _accountsTotalState.PropertyChanged -= AccountsTotalStateOnPropertyChanged;

        if (_filterState is not null)
            _filterState.PropertyChanged -= FilterStateOnPropertyChanged;

        WeakReferenceMessenger.Default.Unregister<TransactionListChanged>(this);
        WeakReferenceMessenger.Default.Unregister<FilterDateRangeChanged>(this);
        WeakReferenceMessenger.Default.Unregister<SettingsChangedMessage>(this);
    }

    public override MainViewTabNames TabName => MainViewTabNames.TransactionsPageContent;
}