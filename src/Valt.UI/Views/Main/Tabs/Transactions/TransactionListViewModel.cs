using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
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
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Transactions.Messages;
using Valt.Infra.Modules.Budget.Transactions.Queries;
using Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.UI.Base;
using static Valt.UI.Base.TaskExtensions;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.LocalStorage;
using Valt.UI.State;
using Valt.UI.UserControls;
using Valt.Core.Modules.AvgPrice;
using Valt.Infra.Modules.AvgPrice.Queries;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;
using Valt.UI.Views.Main.Modals.AvgPriceLineEditor;
using Valt.UI.Views.Main.Modals.ChangeCategoryTransactions;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.TransactionEditor;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionListViewModel : ValtViewModel, IDisposable
{
    private readonly IModalFactory _modalFactory;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionQueries _transactionQueries;
    private readonly ITransactionTermService _transactionTermService;
    private readonly LiveRateState _liveRateState;
    private readonly ILocalDatabase _localDatabase;
    private readonly CurrencySettings _currencySettings;
    private readonly FilterState _filterState;
    private readonly IClock _clock;
    private readonly ILocalStorageService _localStorageService;
    private readonly ILogger<TransactionListViewModel> _logger;
    private readonly IAvgPriceQueries _avgPriceQueries;

    private DateTime _dateForTransaction = DateTime.Now;

    [ObservableProperty] private AccountViewModel? _selectedAccount;
    [ObservableProperty] private FixedExpensesEntryViewModel? _selectedFixedExpense;
    public AvaloniaList<TransactionViewModel> Transactions { get; set; } = new();
    [ObservableProperty] private TransactionViewModel? _selectedTransaction;
    [ObservableProperty] private List<TransactionViewModel>? _selectedTransactions;

    [ObservableProperty] private string _amountHeader = language.Transactions_Columns_Amount;

    [ObservableProperty] private bool _isSingleItemSelected;

    [ObservableProperty] private List<AvgPriceProfileDTO> _matchingAvgPriceProfiles = new();

    #region DataGrid State

    [ObservableProperty] private string? _orderedColumn;
    [ObservableProperty] private ListSortDirection? _sortDirection;

    #endregion

    #region Search fields

    [ObservableProperty] private string _searchTerm = string.Empty;

    [ObservableProperty] private string _appliedSearchTerm = string.Empty;

    #endregion

    public TransactionListViewModel(IModalFactory modalFactory,
        ITransactionRepository transactionRepository,
        ITransactionQueries transactionQueries,
        ITransactionTermService transactionTermService,
        LiveRateState liveRateState,
        ILocalDatabase localDatabase,
        CurrencySettings currencySettings,
        FilterState filterState,
        IClock clock,
        ILocalStorageService localStorageService,
        ILogger<TransactionListViewModel> logger,
        IAvgPriceQueries avgPriceQueries)
    {
        _modalFactory = modalFactory;
        _transactionRepository = transactionRepository;
        _transactionQueries = transactionQueries;
        _transactionTermService = transactionTermService;
        _liveRateState = liveRateState;
        _localDatabase = localDatabase;
        _currencySettings = currencySettings;
        _filterState = filterState;
        _clock = clock;
        _localStorageService = localStorageService;
        _logger = logger;
        _avgPriceQueries = avgPriceQueries;

        _liveRateState.PropertyChanged += LiveRateStateOnPropertyChanged;
        _localDatabase.PropertyChanged += LocalDatabaseOnPropertyChanged;

        WeakReferenceMessenger.Default.Register<AutoSatAmountRefreshed>(this, OnAutoSatAmountRefreshed);
        WeakReferenceMessenger.Default.Register<FilterDateRangeChanged>(this, OnFilterDataRangeChanged);
        WeakReferenceMessenger.Default.Register<FixedExpenseChanged>(this, OnFixedExpenseChanged);
        WeakReferenceMessenger.Default.Register<AccountSelectedChanged>(this, OnAccountSelectedChanged);
        WeakReferenceMessenger.Default.Register<AddTransactionRequested>(this, OnAddTransactionRequested);

        _filterState.MainDate = DateTime.Now;
    }

    private void OnAddTransactionRequested(object recipient, AddTransactionRequested message)
    {
        AddTransactionCommand.Execute(null);
    }

    #region Filter proxy

    public DateTime FilterMainDate
    {
        get => _filterState.MainDate;
        set => _filterState.MainDate = value;
    }

    public DateRange FilterRange
    {
        get => _filterState.Range;
        set => _filterState.Range = value;
    }

    #endregion

    #region DataGrid Settings

    public DataGridSettings GetDataGridSettings()
    {
        if (Design.IsDesignMode) 
            return new DataGridSettings();
        
        var settings = _localStorageService.LoadDataGridSettings();
        OrderedColumn = settings.OrderedColumn;
        SortDirection = settings.SortDirection;
        return settings;
    }

    public void SaveDataGridSettings(IEnumerable<DataGridColumnInfo> columns)
    {
        if (Design.IsDesignMode)
            return;
        
        var columnList = columns.ToList();
        var settings = new DataGridSettings
        {
            OrderedColumn = OrderedColumn,
            SortDirection = SortDirection,
            ColumnWidths = columnList.ToDictionary(c => c.Tag, c => c.Width),
            ColumnOrder = columnList.OrderBy(c => c.DisplayIndex).Select(c => c.Tag).ToList()
        };
        _ = _localStorageService.SaveDataGridSettingsAsync(settings);
    }

    public void UpdateSortState(string? columnHeader)
    {
        if (OrderedColumn == columnHeader)
        {
            SortDirection = SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            OrderedColumn = columnHeader;
            SortDirection = ListSortDirection.Ascending;
        }
    }

    #endregion

    private void LocalDatabaseOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (_localDatabase.HasDatabaseOpen)
            {
                FetchTransactionsCommand.Execute(null);
            }
            else
            {
                ClearState();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to fetch transactions from local database");
        }
    }

    partial void OnSelectedAccountChanged(AccountViewModel? value)
    {
        FetchTransactionsCommand.Execute(null);
    }

    public Task<IEnumerable<object>> GetSearchTermsAsync(string? term, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(term)
            ? Task.FromResult(Enumerable.Empty<object>())
            : Task.FromResult<IEnumerable<object>>(_transactionTermService!.Search(term, 10).Select(x => x.Name)
                .Distinct());
    }

    private void ClearState()
    {
        Transactions.Clear();
    }

    [RelayCommand]
    private async Task AddTransaction()
    {
        var ownerWindow = GetUserControlOwnerWindow()!;

        var modal =
            (TransactionEditorView)await _modalFactory.CreateAsync(ApplicationModalNames.TransactionEditor,
                ownerWindow,
                new TransactionEditorViewModel.Request()
                {
                    AccountId = SelectedAccount?.Id != null ? new AccountId(SelectedAccount.Id) : null,
                    Date = _dateForTransaction
                })!;

        var result = await modal.ShowDialog<TransactionEditorViewModel.Response?>(ownerWindow);

        if (result is null)
            return;

        if (result.TransactionDate is not null)
            _dateForTransaction = result.TransactionDate.Value;

        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        await FetchTransactions();
    }

    [RelayCommand]
    private async Task EditTransaction(TransactionViewModel? selectedTransaction)
    {
        if (selectedTransaction is null)
            return;
        
        var ownerWindow = GetUserControlOwnerWindow()!;

        var modal =
            (TransactionEditorView)await _modalFactory.CreateAsync(ApplicationModalNames.TransactionEditor,
                ownerWindow, new TransactionEditorViewModel.Request()
                {
                    TransactionId = new TransactionId(selectedTransaction.Id)
                })!;

        var result = await modal.ShowDialog<TransactionEditorViewModel.Response?>(ownerWindow);

        if (result is null)
            return;

        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        await FetchTransactions();
    }

    [RelayCommand]
    private async Task CopyTransaction(TransactionViewModel? selectedTransaction)
    {
        if (selectedTransaction is null)
            return;
        
        var ownerWindow = GetUserControlOwnerWindow()!;

        var modal =
            (TransactionEditorView)await _modalFactory.CreateAsync(ApplicationModalNames.TransactionEditor,
                ownerWindow, new TransactionEditorViewModel.Request()
                {
                    TransactionId = new TransactionId(selectedTransaction.Id),
                    CopyTransaction = true
                })!;

        var result = await modal.ShowDialog<TransactionEditorViewModel.Response?>(ownerWindow);

        if (result is null)
            return;

        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        await FetchTransactions();
    }

    [RelayCommand]
    private async Task DeleteTransaction(TransactionViewModel? selectedTransaction)
    {
        if (selectedTransaction is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow();
        if (ownerWindow is null)
            return;

        // First, ask for default confirmation
        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.Transactions_Menu_Delete,
            $"{language.Transactions_Menu_Delete} '{selectedTransaction.Name}'?",
            ownerWindow);

        if (!confirmed)
            return;

        // Check if transaction is part of an installment group
        var transaction = await _transactionRepository.GetTransactionByIdAsync(selectedTransaction.Id);
        if (transaction is not null && transaction.IsPartOfGroup)
        {
            var deleteAll = await MessageBoxHelper.ShowQuestionAsync(
                language.DeleteInstallment_Title,
                language.DeleteInstallment_Message,
                ownerWindow);

            if (deleteAll)
            {
                await _transactionRepository.DeleteTransactionsByGroupIdAsync(transaction.GroupId!);
            }
            else
            {
                await _transactionRepository.DeleteTransactionAsync(selectedTransaction.Id);
            }
        }
        else
        {
            await _transactionRepository.DeleteTransactionAsync(selectedTransaction.Id);
        }

        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        await FetchTransactions();
    }

    [RelayCommand]
    private async Task ChangeNamesAndCategories()
    {
        var ownerWindow = GetUserControlOwnerWindow()!;

        var modal =
            (ChangeCategoryTransactionsView)await _modalFactory.CreateAsync(
                ApplicationModalNames.ChangeCategoryTransactions,
                ownerWindow, null)!;

        var result = await modal.ShowDialog<ChangeCategoryTransactionsViewModel.Response?>(ownerWindow);

        if (result is null)
            return;

        if (SelectedTransactions is null)
            return;

        foreach (var transactionViewModel in SelectedTransactions)
        {
            var transaction =
                await _transactionRepository.GetTransactionByIdAsync(new TransactionId(transactionViewModel.Id));

            if (transaction is null)
                continue;

            if (result.RenameEnabled && !string.IsNullOrWhiteSpace(result.Name))
            {
                transaction.Rename(result.Name);
            }

            if (result.ChangeCategoryEnabled && result.CategoryId is not null)
            {
                transaction.ChangeCategory(new CategoryId(result.CategoryId));
            }

            await _transactionRepository.SaveTransactionAsync(transaction);
        }

        await FetchTransactions();
    }

    #region Fixed Expenses - Transaction List Context Options

    public string BindToFixedExpenseCaption =>
        SelectedTransaction is not null && SelectedTransaction.FixedExpenseRecordId is null && SelectedFixedExpense is not null
            ? $"{language.Transactions_Menu_BindToFixedExpenseCaption} {SelectedFixedExpense.Name}"
            : string.Empty;

    public bool CanBindToFixedExpense => SelectedTransaction is not null &&
                                         SelectedTransaction.FixedExpenseRecordId is null &&
                                         SelectedFixedExpense is not null;
    
    public string UnbindToFixedExpenseCaption =>
        SelectedTransaction is not null && SelectedTransaction.FixedExpenseRecordId is not null
            ? $"{language.Transactions_Menu_UnbindToFixedExpenseCaption} {SelectedTransaction.FixedExpenseName}"
            : string.Empty;

    public bool CanUnbindFromFixedExpense =>
        SelectedTransaction is not null && SelectedTransaction.FixedExpenseRecordId is not null;

    private void RefreshFixedExpensesContextProperties()
    {
        OnPropertyChanged(nameof(BindToFixedExpenseCaption));
        OnPropertyChanged(nameof(UnbindToFixedExpenseCaption));
        OnPropertyChanged(nameof(CanBindToFixedExpense));
        OnPropertyChanged(nameof(CanUnbindFromFixedExpense));
    }

    [RelayCommand]
    private async Task BindToFixedExpense()
    {
        if (SelectedTransaction is null || SelectedFixedExpense is null) return;

        // Note: Consider extracting to an application service (e.g., TransactionFixedExpenseService)
        var transaction = await _transactionRepository.GetTransactionByIdAsync(new TransactionId(SelectedTransaction.Id));

        transaction!.SetFixedExpense(new TransactionFixedExpenseReference(SelectedFixedExpense.Id, SelectedFixedExpense.ReferenceDate));

        await _transactionRepository.SaveTransactionAsync(transaction);
        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        await FetchTransactions();
    }

    [RelayCommand]
    private async Task UnbindFromFixedExpense()
    {
        if (SelectedTransaction is null) return;

        // Note: Consider extracting to an application service (e.g., TransactionFixedExpenseService)
        var transaction = await _transactionRepository.GetTransactionByIdAsync(new TransactionId(SelectedTransaction.Id));

        transaction!.SetFixedExpense(null);

        await _transactionRepository.SaveTransactionAsync(transaction);
        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        await FetchTransactions();
    }

    #endregion

    #region AvgPrice - Send to Profile

    public bool CanSendToAvgPrice =>
        SelectedTransaction is not null &&
        IsSingleItemSelected &&
        SelectedTransaction.TransferType is TransactionTransferTypes.FiatToBitcoin
            or TransactionTransferTypes.BitcoinToFiat &&
        MatchingAvgPriceProfiles.Count > 0;

    private async Task RefreshMatchingAvgPriceProfilesAsync()
    {
        if (SelectedTransaction is null ||
            SelectedTransaction.TransferType is not (TransactionTransferTypes.FiatToBitcoin
                or TransactionTransferTypes.BitcoinToFiat))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MatchingAvgPriceProfiles = new List<AvgPriceProfileDTO>();
                OnPropertyChanged(nameof(CanSendToAvgPrice));
            });
            return;
        }

        var fiatCurrencyCode = SelectedTransaction.FiatCurrencyCode;
        if (string.IsNullOrEmpty(fiatCurrencyCode))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MatchingAvgPriceProfiles = new List<AvgPriceProfileDTO>();
                OnPropertyChanged(nameof(CanSendToAvgPrice));
            });
            return;
        }

        var allProfiles = await _avgPriceQueries.GetProfilesAsync(showHidden: false);

        var filteredProfiles = allProfiles
            .Where(p => p.AssetName == "BTC" &&
                        p.CurrencyCode.Equals(fiatCurrencyCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .ToList();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            MatchingAvgPriceProfiles = filteredProfiles;
            OnPropertyChanged(nameof(CanSendToAvgPrice));
        });
    }

    [RelayCommand]
    private async Task SendToAvgPriceProfile(AvgPriceProfileDTO? profile)
    {
        if (profile is null || SelectedTransaction is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow()!;

        // Determine line type: FiatToBitcoin = Buy, BitcoinToFiat = Sell
        var lineType = SelectedTransaction.TransferType == TransactionTransferTypes.FiatToBitcoin
            ? AvgPriceLineTypes.Buy
            : AvgPriceLineTypes.Sell;

        // Get BTC amount in BTC (not sats) - always positive
        var btcSats = SelectedTransaction.TransferType == TransactionTransferTypes.FiatToBitcoin
            ? SelectedTransaction.ToAmountSats.GetValueOrDefault()
            : Math.Abs(SelectedTransaction.FromAmountSats.GetValueOrDefault());
        var btcQuantity = btcSats / 100_000_000m;

        // Get fiat amount (total cost) - always positive
        var fiatAmount = SelectedTransaction.TransferType == TransactionTransferTypes.FiatToBitcoin
            ? Math.Abs(SelectedTransaction.FromAmountFiat.GetValueOrDefault())
            : SelectedTransaction.ToAmountFiat.GetValueOrDefault();

        var currency = FiatCurrency.GetFromCode(profile.CurrencyCode);

        var modal = (AvgPriceLineEditorView)await _modalFactory.CreateAsync(
            ApplicationModalNames.AvgPriceLineEditor,
            ownerWindow,
            new AvgPriceLineEditorViewModel.Request
            {
                ProfileId = profile.Id,
                AssetName = profile.AssetName,
                AssetPrecision = profile.Precision,
                CurrencySymbol = currency.Symbol,
                CurrencySymbolOnRight = currency.SymbolOnRight,
                PresetDate = SelectedTransaction.Date,
                PresetLineType = lineType,
                PresetQuantity = btcQuantity,
                PresetAmount = fiatAmount
            })!;

        await modal.ShowDialog<AvgPriceLineEditorViewModel.Response?>(ownerWindow);
    }

    #endregion

    [RelayCommand]
    private async Task FetchTransactions()
    {
        var currentSelectedTransactionId = SelectedTransaction?.Id;

        var transactions = await _transactionQueries.GetTransactionsAsync(new TransactionQueryFilter()
        {
            Accounts = SelectedAccount == null ? null : [SelectedAccount.Id],
            SearchTerm = AppliedSearchTerm,
            From = DateOnly.FromDateTime(_filterState.Range.Start),
            To = DateOnly.FromDateTime(_filterState.Range.End)
        });

        Transactions.Clear();

        var transactionViewModels = TransactionViewModel.Parse(transactions.Items, _clock.GetCurrentLocalDate()).ToList();
        foreach (var transactionViewModel in transactionViewModels)
        {
            transactionViewModel.RefreshCurrentAutoSatValue(_liveRateState.UsdPrice, _liveRateState.BitcoinPrice,
                _currencySettings.MainFiatCurrency);
        }

        Transactions.AddRange(transactionViewModels);

        if (currentSelectedTransactionId is not null)
            SelectedTransaction = Transactions.SingleOrDefault(x => x.Id == currentSelectedTransactionId);
    }

    [RelayCommand]
    private async Task ApplySearch()
    {
        AppliedSearchTerm = SearchTerm;

        if (!Design.IsDesignMode)
            await FetchTransactions();
    }

    public void UpdateSelectedItems(IList selectedItems)
    {
        SelectedTransactions = selectedItems.OfType<TransactionViewModel>().ToList();

        if (selectedItems.Count == 0)
        {
            // No selection - clear state
            IsSingleItemSelected = false;
            SelectedTransaction = null;
            AmountHeader = language.Transactions_Columns_Amount;
            return;
        }

        // Keep SelectedTransaction as anchor for context menu (first item in selection)
        SelectedTransaction = SelectedTransactions.FirstOrDefault();

        if (SelectedAccount == null)
        {
            IsSingleItemSelected = selectedItems.Count == 1;
            AmountHeader = language.Transactions_Columns_Amount;
            return;
        }

        if (selectedItems.Count == 1)
        {
            IsSingleItemSelected = true;
            AmountHeader = language.Transactions_Columns_Amount;
            return;
        }

        IsSingleItemSelected = false;

        if (SelectedAccount!.IsBtcAccount)
        {
            long totalSats = 0;
            foreach (var item in SelectedTransactions)
            {
                if (item.TransactionType is TransactionTypes.Credit or TransactionTypes.Debt)
                    totalSats += item.FromAmountSats.GetValueOrDefault();
                else
                {
                    if (item.FromAccountId == SelectedAccount.Id)
                        totalSats += item.FromAmountSats.GetValueOrDefault();
                    else
                        totalSats += item.ToAmountSats.GetValueOrDefault();
                }
            }

            AmountHeader = $"{language.Transactions_Columns_Amount}: {totalSats}";
        }
        else
        {
            decimal totalFiat = 0;
            foreach (var item in SelectedTransactions)
            {
                if (item.TransactionType is TransactionTypes.Credit or TransactionTypes.Debt)
                    totalFiat += item.FromAmountFiat.GetValueOrDefault();
                else
                {
                    if (item.FromAccountId == SelectedAccount.Id)
                        totalFiat += item.FromAmountFiat.GetValueOrDefault();
                    else
                        totalFiat += item.ToAmountFiat.GetValueOrDefault();
                }
            }

            AmountHeader = $"{language.Transactions_Columns_Amount}: {totalFiat}";
        }
    }

    private void LiveRateStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        foreach (var transaction in Transactions)
        {
            transaction.RefreshCurrentAutoSatValue(_liveRateState.UsdPrice, _liveRateState.BitcoinPrice,
                _currencySettings.MainFiatCurrency);
        }
    }

    #region Message recipients

    private void OnAutoSatAmountRefreshed(object recipient, AutoSatAmountRefreshed message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var transactionMessage in message.Transactions)
            {
                var transaction = Transactions.SingleOrDefault(x => x.Id == transactionMessage.TransactionId.Value);

                if (transaction is null) continue;

                transaction.SetupAutoSatAmount(transactionMessage.AutoSatAmount);
                transaction.RefreshCurrentAutoSatValue(_liveRateState.UsdPrice, _liveRateState.BitcoinPrice,
                    _currencySettings.MainFiatCurrency);
            }
        });
    }

    private void OnAccountSelectedChanged(object recipient, AccountSelectedChanged message)
    {
        SelectedAccount = message.Value;
    }
    
    private void OnFixedExpenseChanged(object recipient, FixedExpenseChanged message)
    {
        SelectedFixedExpense = message.Value;
        RefreshFixedExpensesContextProperties();
    }

    private void OnFilterDataRangeChanged(object recipient, FilterDateRangeChanged message)
    {
        OnPropertyChanged(nameof(FilterMainDate));
        OnPropertyChanged(nameof(FilterRange));
        FetchTransactions().SafeFireAndForget(logger: _logger, callerName: nameof(FetchTransactions));
    }

    partial void OnSelectedTransactionChanged(TransactionViewModel? value)
    {
        RefreshFixedExpensesContextProperties();
        RefreshMatchingAvgPriceProfilesAsync().SafeFireAndForget(logger: _logger);
    }

    partial void OnIsSingleItemSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSendToAvgPrice));
    }
    


    #endregion

    public void Dispose()
    {
        _liveRateState.PropertyChanged -= LiveRateStateOnPropertyChanged;
        _localDatabase.PropertyChanged -= LocalDatabaseOnPropertyChanged;

        WeakReferenceMessenger.Default.Unregister<AccountSelectedChanged>(this);
        WeakReferenceMessenger.Default.Unregister<FilterDateRangeChanged>(this);
        WeakReferenceMessenger.Default.Unregister<FixedExpenseChanged>(this);
        WeakReferenceMessenger.Default.Unregister<AutoSatAmountRefreshed>(this);
        WeakReferenceMessenger.Default.Unregister<AddTransactionRequested>(this);
    }
}