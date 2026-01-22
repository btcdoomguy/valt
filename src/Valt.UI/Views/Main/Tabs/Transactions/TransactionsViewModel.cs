using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State;
using Valt.UI.Views.Main.Modals.ManageAccount;
using Valt.UI.Views.Main.Modals.ManageAccountGroup;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionsViewModel : ValtTabViewModel, IDisposable
{
    // Animation settings - adjust these to change the wealth counter animation speed
    private const int WealthAnimationDurationMs = 1500; // 1.5 seconds
    private const int WealthAnimationIntervalMs = 16;   // ~60fps

    private readonly IModalFactory? _modalFactory;
    private readonly IAccountRepository? _accountRepository;
    private readonly IAccountGroupRepository? _accountGroupRepository;
    private readonly AccountsTotalState? _accountsTotalState;
    private readonly RatesState _ratesState;
    private readonly CurrencySettings? _currencySettings;
    private readonly DisplaySettings? _displaySettings;
    private readonly AccountDisplayOrderManager? _accountDisplayOrderManager;
    private readonly FilterState? _filterState;
    private readonly ILogger<TransactionsViewModel> _logger;
    private readonly IAccountQueries? _accountQueries;
    private readonly SecureModeState _secureModeState;

    //instances of the sub contents
    private readonly TransactionListViewModel _transactionListViewModel = null!;

    // Animation fields for wealth values
    private Timer? _wealthAnimationTimer;
    private DateTime _wealthAnimationStartTime;

    // Raw values for animation (source and target)
    private long _animatedWealthInSats;
    private long _targetWealthInSats;
    private long _startWealthInSats;

    private decimal _animatedWealthInFiat;
    private decimal _targetWealthInFiat;
    private decimal _startWealthInFiat;

    private decimal _animatedAllWealthInFiat;
    private decimal _targetAllWealthInFiat;
    private decimal _startAllWealthInFiat;

    private decimal _animatedWealthInBtcRatio;
    private decimal _targetWealthInBtcRatio;
    private decimal _startWealthInBtcRatio;

    [ObservableProperty] private AvaloniaList<AccountViewModel> _accounts = new();

    /// <summary>
    /// Flat list of items for the accounts ListBox, containing both group headers and accounts.
    /// </summary>
    [ObservableProperty] private AvaloniaList<IAccountListItem> _accountListItems = new();

    /// <summary>
    /// Available groups for the "Move to Group" context menu.
    /// </summary>
    [ObservableProperty] private AvaloniaList<AccountGroupMenuItem> _availableGroups = new();

    [ObservableProperty] private AccountViewModel? _selectedAccount;

    [ObservableProperty] private string _allWealthInSats = "12.34567890";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DisplayWealthInBtcRatio))]
    private string _wealthInBtcRatio = "65.3%";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DisplayWealthInSats))]
    private string _wealthInSats = "11.34567890";
    [ObservableProperty] private string _wealthNotInSats = "1.00000000";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DisplayWealthInFiat))]
    private string _wealthInFiat = "R$ 12.000.000,00";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DisplayAllWealthInFiat))]
    private string _allWealthInFiat = "R$ 13.000.000,00";

    [ObservableProperty] private FiatCurrency _mainFiatCurrency = FiatCurrency.Brl;

    [ObservableProperty] private ValtViewModel? _subContent;

    public TransactionsViewModel(IAccountQueries accountQueries,
        IModalFactory modalFactory, IAccountRepository accountRepository,
        IAccountGroupRepository accountGroupRepository,
        AccountsTotalState accountsTotalState,
        RatesState ratesState,
        CurrencySettings currencySettings,
        DisplaySettings displaySettings,
        AccountDisplayOrderManager accountDisplayOrderManager,
        FilterState filterState,
        ITransactionTabFactory transactionTabFactory,
        ILogger<TransactionsViewModel> logger,
        SecureModeState secureModeState)
    {
        _accountQueries = accountQueries;
        _secureModeState = secureModeState;
        _modalFactory = modalFactory;
        _accountRepository = accountRepository;
        _accountGroupRepository = accountGroupRepository;
        _accountsTotalState = accountsTotalState;
        _ratesState = ratesState;
        _currencySettings = currencySettings;
        _displaySettings = displaySettings;
        _accountDisplayOrderManager = accountDisplayOrderManager;
        _filterState = filterState;
        _logger = logger;

        _transactionListViewModel = (TransactionListViewModel)transactionTabFactory.Create(TransactionsTabNames.List);

        _accountsTotalState.PropertyChanged += AccountsTotalStateOnPropertyChanged;
        _filterState.PropertyChanged += FilterStateOnPropertyChanged;
        _secureModeState.PropertyChanged += SecureModeStateOnPropertyChanged;

        _ = InitializeAsync();

        MainFiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);

        WeakReferenceMessenger.Default.Register<TransactionListChanged>(this, OnTransactionListChangedReceive);

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

    private void OnTransactionListChangedReceive(object recipient, TransactionListChanged message)
    {
        _ = FetchAccounts();
    }

    private void FilterStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CurrentMonthYearDisplay));
    }

    private void SecureModeStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(DisplayWealthInSats));
            OnPropertyChanged(nameof(DisplayWealthInFiat));
            OnPropertyChanged(nameof(DisplayAllWealthInFiat));
            OnPropertyChanged(nameof(DisplayWealthInBtcRatio));

            // Update account display for secure mode
            foreach (var account in Accounts)
            {
                account.SecureModeEnabled = _secureModeState.IsEnabled;
            }

            // Update accounts in the flat list
            foreach (var item in AccountListItems)
            {
                if (item is AccountViewModel account)
                {
                    account.SecureModeEnabled = _secureModeState.IsEnabled;
                }
            }
        });
    }

    public string DisplayWealthInSats => _secureModeState.IsEnabled ? "---" : CurrencyDisplay.FormatSatsAsBitcoin(_animatedWealthInSats);
    public string DisplayWealthInFiat => _secureModeState.IsEnabled ? "---" : CurrencyDisplay.FormatFiat(_animatedWealthInFiat, _currencySettings?.MainFiatCurrency ?? "USD");
    public string DisplayAllWealthInFiat => _secureModeState.IsEnabled ? "---" : CurrencyDisplay.FormatFiat(_animatedAllWealthInFiat, _currencySettings?.MainFiatCurrency ?? "USD");
    public string DisplayWealthInBtcRatio => _secureModeState.IsEnabled ? "---" : _animatedWealthInBtcRatio.ToString("F1", CultureInfo.InvariantCulture) + "%";

    private void AccountsTotalStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_accountsTotalState is null) return;
            if (_currencySettings is null) return;

            // Keep string properties updated for compatibility
            AllWealthInSats = CurrencyDisplay.FormatSatsAsBitcoin(_accountsTotalState.CurrentWealth.AllWealthInSats);
            WealthInSats = CurrencyDisplay.FormatSatsAsBitcoin(_accountsTotalState.CurrentWealth.WealthInSats);
            WealthNotInSats = CurrencyDisplay.FormatSatsAsBitcoin(_accountsTotalState.CurrentWealth.AllWealthInSats -
                                                                 _accountsTotalState.CurrentWealth.WealthInSats);
            AllWealthInFiat = CurrencyDisplay.FormatFiat(_accountsTotalState.CurrentWealth.AllWealthInMainFiatCurrency,
                _currencySettings.MainFiatCurrency);
            WealthInFiat = CurrencyDisplay.FormatFiat(_accountsTotalState.CurrentWealth.WealthInMainFiatCurrency,
                _currencySettings.MainFiatCurrency);
            WealthInBtcRatio =
                _accountsTotalState.CurrentWealth.WealthInBtcRatio.ToString(CultureInfo.InvariantCulture) + "%";

            // Start animation to new target values
            StartWealthAnimation(
                _accountsTotalState.CurrentWealth.WealthInSats,
                _accountsTotalState.CurrentWealth.WealthInMainFiatCurrency,
                _accountsTotalState.CurrentWealth.AllWealthInMainFiatCurrency,
                _accountsTotalState.CurrentWealth.WealthInBtcRatio);
        });
    }

    private void StartWealthAnimation(long targetSats, decimal targetFiat, decimal targetAllFiat, decimal targetRatio)
    {
        // Store current animated values as start values
        _startWealthInSats = _animatedWealthInSats;
        _startWealthInFiat = _animatedWealthInFiat;
        _startAllWealthInFiat = _animatedAllWealthInFiat;
        _startWealthInBtcRatio = _animatedWealthInBtcRatio;

        // Set target values
        _targetWealthInSats = targetSats;
        _targetWealthInFiat = targetFiat;
        _targetAllWealthInFiat = targetAllFiat;
        _targetWealthInBtcRatio = targetRatio;

        // If all values are the same, no need to animate
        if (_startWealthInSats == _targetWealthInSats &&
            _startWealthInFiat == _targetWealthInFiat &&
            _startAllWealthInFiat == _targetAllWealthInFiat &&
            _startWealthInBtcRatio == _targetWealthInBtcRatio)
        {
            return;
        }

        // Stop any existing animation
        _wealthAnimationTimer?.Dispose();

        _wealthAnimationStartTime = DateTime.UtcNow;
        _wealthAnimationTimer = new Timer(OnWealthAnimationTick, null, 0, WealthAnimationIntervalMs);
    }

    private void OnWealthAnimationTick(object? state)
    {
        var elapsed = (DateTime.UtcNow - _wealthAnimationStartTime).TotalMilliseconds;
        var progress = Math.Min(elapsed / WealthAnimationDurationMs, 1.0);

        // Cubic ease-out: 1 - (1 - t)^3
        var easedProgress = 1 - Math.Pow(1 - progress, 3);

        // Interpolate all values
        var currentSats = _startWealthInSats + (long)(easedProgress * (_targetWealthInSats - _startWealthInSats));
        var currentFiat = _startWealthInFiat + (decimal)easedProgress * (_targetWealthInFiat - _startWealthInFiat);
        var currentAllFiat = _startAllWealthInFiat + (decimal)easedProgress * (_targetAllWealthInFiat - _startAllWealthInFiat);
        var currentRatio = _startWealthInBtcRatio + (decimal)easedProgress * (_targetWealthInBtcRatio - _startWealthInBtcRatio);

        Dispatcher.UIThread.Post(() =>
        {
            _animatedWealthInSats = currentSats;
            _animatedWealthInFiat = currentFiat;
            _animatedAllWealthInFiat = currentAllFiat;
            _animatedWealthInBtcRatio = currentRatio;

            OnPropertyChanged(nameof(DisplayWealthInSats));
            OnPropertyChanged(nameof(DisplayWealthInFiat));
            OnPropertyChanged(nameof(DisplayAllWealthInFiat));
            OnPropertyChanged(nameof(DisplayWealthInBtcRatio));
        });

        if (progress >= 1.0)
        {
            _wealthAnimationTimer?.Dispose();
            _wealthAnimationTimer = null;
        }
    }

    private async Task InitializeAsync()
    {
        SubContent = _transactionListViewModel;

        if (_accountDisplayOrderManager is null) return;

        await _accountDisplayOrderManager.NormalizeDisplayOrdersAsync(null);

        Dispatcher.UIThread.Post(() =>
        {
            _ = FetchAccounts();
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
        var groups = await _accountQueries.GetAccountGroupsAsync();

        WeakReferenceMessenger.Default.Send(accounts);

        // Build all accounts list (for compatibility with existing code)
        Accounts.Clear();
        Accounts.AddRange(accounts.Items.Select(x => new AccountViewModel(x) { SecureModeEnabled = _secureModeState.IsEnabled }));

        // Build flat list with group headers interspersed
        AccountListItems.Clear();

        // First, add grouped accounts with their headers
        foreach (var group in groups.OrderBy(g => g.DisplayOrder))
        {
            var groupAccounts = accounts.Items
                .Where(a => a.GroupId == group.Id)
                .Select(x => new AccountViewModel(x) { SecureModeEnabled = _secureModeState.IsEnabled })
                .ToList();

            // Only add group header if it has accounts
            if (groupAccounts.Count > 0)
            {
                var header = new AccountGroupHeaderViewModel(group.Id, group.Name);
                AccountListItems.Add(header);

                foreach (var account in groupAccounts)
                {
                    AccountListItems.Add(account);
                }
            }
        }

        // Then add ungrouped accounts
        var ungrouped = accounts.Items
            .Where(a => a.GroupId is null)
            .Select(x => new AccountViewModel(x) { SecureModeEnabled = _secureModeState.IsEnabled });

        foreach (var account in ungrouped)
        {
            AccountListItems.Add(account);
        }

        // Populate available groups for the "Move to Group" context menu
        AvailableGroups.Clear();
        AvailableGroups.AddRange(groups.OrderBy(g => g.Name).Select(g => new AccountGroupMenuItem(g.Id, g.Name)));

        if (currentSelectedAccountId is not null)
            SelectedAccount = AccountListItems
                .OfType<AccountViewModel>()
                .SingleOrDefault(x => x.Id == currentSelectedAccountId);
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
    private async Task AddAccountGroup()
    {
        var ownerWindow = GetUserControlOwnerWindow!();

        var window =
            (ManageAccountGroupView)await _modalFactory!.CreateAsync(ApplicationModalNames.ManageAccountGroup, ownerWindow)!;

        var result = await window.ShowDialog<ManageAccountGroupViewModel.Response?>(ownerWindow!);

        if (result is null)
            return;

        await FetchAccounts();
    }

    [RelayCommand]
    private async Task EditAccountGroup(AccountGroupHeaderViewModel group)
    {
        var ownerWindow = GetUserControlOwnerWindow!();

        var window =
            (ManageAccountGroupView)await _modalFactory!.CreateAsync(ApplicationModalNames.ManageAccountGroup, ownerWindow, group.Id)!;

        var result = await window.ShowDialog<ManageAccountGroupViewModel.Response?>(ownerWindow!);

        if (result is null)
            return;

        await FetchAccounts();
    }

    [RelayCommand]
    private async Task DeleteAccountGroup(AccountGroupHeaderViewModel group)
    {
        var ownerWindow = GetUserControlOwnerWindow!();

        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.Transactions_DeleteGroup,
            language.Transactions_DeleteGroupConfirmation,
            ownerWindow!);

        if (!confirmed)
            return;

        await _accountGroupRepository!.DeleteAsync(new AccountGroupId(group.Id));
        await FetchAccounts();
    }

    [RelayCommand]
    private async Task MoveUpAccountGroup(AccountGroupHeaderViewModel group)
    {
        await MoveAccountGroup(group.Id, moveUp: true);
    }

    [RelayCommand]
    private async Task MoveDownAccountGroup(AccountGroupHeaderViewModel group)
    {
        await MoveAccountGroup(group.Id, moveUp: false);
    }

    private async Task MoveAccountGroup(string groupId, bool moveUp)
    {
        // Get all groups ordered by display order
        var allGroups = (await _accountGroupRepository!.GetAllAsync()).OrderBy(g => g.DisplayOrder).ToList();
        var currentIndex = allGroups.FindIndex(g => g.Id.Value == groupId);

        if (currentIndex < 0) return;

        // Check if move is valid
        if (moveUp && currentIndex == 0) return;
        if (!moveUp && currentIndex == allGroups.Count - 1) return;

        // Swap positions in the list
        var targetIndex = moveUp ? currentIndex - 1 : currentIndex + 1;
        (allGroups[currentIndex], allGroups[targetIndex]) = (allGroups[targetIndex], allGroups[currentIndex]);

        // Re-assign sequential display orders and save all groups
        for (var i = 0; i < allGroups.Count; i++)
        {
            allGroups[i].ChangeDisplayOrder(i);
            await _accountGroupRepository.SaveAsync(allGroups[i]);
        }

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

    /// <summary>
    /// Stores the account for which the context menu was opened.
    /// Set by SetContextMenuAccount command when "Move to Group" submenu is opened.
    /// </summary>
    private AccountViewModel? _contextMenuAccount;

    [RelayCommand]
    private void SetContextMenuAccount(AccountViewModel? account)
    {
        _contextMenuAccount = account;
    }

    [RelayCommand]
    private async Task MoveAccountToGroup(AccountGroupMenuItem? group)
    {
        if (group is null || _contextMenuAccount is null) return;

        var account = await _accountRepository!.GetAccountByIdAsync(_contextMenuAccount.Id);

        if (account is null)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_AccountNotFound, GetUserControlOwnerWindow()!);
            return;
        }

        var newGroupId = new AccountGroupId(group.Id);
        var newDisplayOrder = _accountDisplayOrderManager!.GetNextDisplayOrderForGroup(
            new LiteDB.ObjectId(group.Id));

        account.ChangeDisplayOrder(newDisplayOrder);
        account.AssignToGroup(newGroupId);

        await _accountRepository.SaveAccountAsync(account);
        await FetchAccounts();
    }

    [RelayCommand]
    private async Task RemoveAccountFromGroup(AccountViewModel? selectedAccount)
    {
        if (selectedAccount is null) return;

        var account = await _accountRepository!.GetAccountByIdAsync(selectedAccount.Id);

        if (account is null)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_AccountNotFound, GetUserControlOwnerWindow()!);
            return;
        }

        var newDisplayOrder = _accountDisplayOrderManager!.GetNextDisplayOrderForGroup(null);

        account.ChangeDisplayOrder(newDisplayOrder);
        account.AssignToGroup(null);

        await _accountRepository.SaveAccountAsync(account);
        await FetchAccounts();
    }

    #endregion

    /// <summary>
    /// Returns the current month and year formatted for display (e.g., "January 2026")
    /// </summary>
    public string CurrentMonthYearDisplay =>
        DateOnly.FromDateTime(_filterState!.MainDate).ToString("MMMM yyyy", CultureInfo.CurrentCulture);

    partial void OnSelectedAccountChanged(AccountViewModel? value)
    {
        WeakReferenceMessenger.Default.Send(new AccountSelectedChanged(value));
    }

    public void Dispose()
    {
        _wealthAnimationTimer?.Dispose();
        _wealthAnimationTimer = null;

        if (_accountsTotalState is not null)
            _accountsTotalState.PropertyChanged -= AccountsTotalStateOnPropertyChanged;

        if (_filterState is not null)
            _filterState.PropertyChanged -= FilterStateOnPropertyChanged;

        _secureModeState.PropertyChanged -= SecureModeStateOnPropertyChanged;

        WeakReferenceMessenger.Default.Unregister<TransactionListChanged>(this);
        WeakReferenceMessenger.Default.Unregister<SettingsChangedMessage>(this);
    }

    public override MainViewTabNames TabName => MainViewTabNames.TransactionsPageContent;
}

/// <summary>
/// Represents a group option in the "Move to Group" context menu.
/// </summary>
public record AccountGroupMenuItem(string Id, string Name)
{
    public override string ToString() => Name;
}
