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
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Accounts.Queries.GetAccounts;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries.GetCategories;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpense;
using Valt.App.Modules.Budget.Transactions.Commands.AddTransaction;
using Valt.App.Modules.Budget.Transactions.Commands.EditTransaction;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.App.Modules.Budget.Transactions.Queries.GetTransactionById;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Services;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State;
using Valt.UI.Views.Main.Modals.ConversionCalculator;
using Valt.UI.Views.Main.Modals.TransactionEditor.ChildViewModels;
using Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

namespace Valt.UI.Views.Main.Modals.TransactionEditor;

public partial class TransactionEditorViewModel : ValtModalValidatorViewModel
{
    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly ITransactionTermService? _transactionTermService;
    private readonly IModalFactory _modalFactory = null!;
    private readonly CurrencySettings _currencySettings = null!;
    private readonly DisplaySettings _displaySettings = null!;
    private readonly LastTransactionDateState _lastTransactionDateState;
    private readonly IFireAndForgetTaskRunner _runner = null!;
    private readonly ILogger<TransactionEditorViewModel> _logger = null!;
    private readonly ILoggerFactory _loggerFactory = null!;
    private readonly ITransactionDetailsBuilder? _transactionDetailsBuilder;

    public AvaloniaList<CategoryDTO> AvailableCategories { get; set; } = [];
    public AvaloniaList<AccountDTO> AvailableAccounts { get; set; } = [];

    #region Form Data

    private TransactionId? _transactionId;

    [Required(ErrorMessage = "Date is required")] [ObservableProperty]
    private DateTime? _date;

    [Required(ErrorMessage = "Name is required")] [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty] private TransactionTermResult? _transactionTermResult;

    [Required(ErrorMessage = "Category is required")] [ObservableProperty]
    private CategoryDTO? _category;

    [ObservableProperty] private string _notes = string.Empty;

    [NotifyPropertyChangedFor(nameof(IsBoundToFixedExpense), nameof(BoundToFixedExpenseCaption), nameof(HasMetadata))]
    [ObservableProperty]
    private TransactionFixedExpenseReference? _transactionFixedExpenseReference;

    [NotifyPropertyChangedFor(nameof(IsBoundToFixedExpense), nameof(BoundToFixedExpenseCaption), nameof(HasMetadata))]
    [ObservableProperty]
    private FixedExpenseDTO? _fixedExpense;

    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebtSelected), nameof(CreditSelected), nameof(TransferSelected))]
    private TransactionTypes _selectedMode;

    public bool DebtSelected => SelectedMode == TransactionTypes.Debt;
    public bool CreditSelected => SelectedMode == TransactionTypes.Credit;
    public bool TransferSelected => SelectedMode == TransactionTypes.Transfer;

    [ObservableProperty]
    private TransactionEditorChildViewModel? _activeChildViewModel;

    public string OkButtonLabel => _transactionId is null
        ? language.TransactionEditor_Ok
        : language.TransactionEditor_Save;

    [ObservableProperty] private string _windowTitle = language.ManageTransactions_AddTitle;

    public bool IsBoundToFixedExpense => TransactionFixedExpenseReference is not null;

    public string BoundToFixedExpenseCaption => TransactionFixedExpenseReference is not null && FixedExpense is not null
        ? $"{FixedExpense.Name} ({TransactionFixedExpenseReference.ReferenceDate.ToShortDateString()})"
        : language.Empty;

    public bool HasMetadata => IsBoundToFixedExpense || IsAutoSatAmount;

    #endregion Properties

    #region Auto Sat Area

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMetadata))]
    private bool _isAutoSatAmount;

    [ObservableProperty] private string? _satAmountStateDescription;

    [ObservableProperty] private long? _satAmountDescription;

    #endregion

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public TransactionEditorViewModel()
    {
        _lastTransactionDateState = null!;
        if (Design.IsDesignMode)
        {
            Date = DateTime.Now.Date;
            SelectedMode = TransactionTypes.Debt;
            ActiveChildViewModel = new DebtTransactionEditorViewModel();
        }
    }

    public TransactionEditorViewModel(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        ITransactionTermService transactionTermService,
        IModalFactory modalFactory,
        CurrencySettings currencySettings,
        DisplaySettings displaySettings,
        LastTransactionDateState lastTransactionDateState,
        IFireAndForgetTaskRunner runner,
        ILogger<TransactionEditorViewModel> logger,
        ILoggerFactory loggerFactory,
        ITransactionDetailsBuilder transactionDetailsBuilder)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _transactionTermService = transactionTermService;
        _modalFactory = modalFactory;
        _currencySettings = currencySettings;
        _displaySettings = displaySettings;
        _lastTransactionDateState = lastTransactionDateState;
        _runner = runner;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _transactionDetailsBuilder = transactionDetailsBuilder;
    }

    public override async Task OnBindParameterAsync()
    {
        Date = DateTime.Now.Date;
        await FetchCategoriesAsync();
        await FetchAccountsAsync();

        if (Parameter is not Request request)
        {
            SetActiveChild(TransactionTypes.Debt);
            return;
        }

        if (request.TransactionId is not null)
            await OnBindParameterForEditingAsync(request);
        else
            await OnBindParameterForAddingNewAsync(request);
    }

    private async Task OnBindParameterForAddingNewAsync(Request request)
    {
        if (request.TransactionId is not null)
        {
            return;
        }

        var mode = request.DefaultMode ?? TransactionTypes.Debt;
        SetActiveChild(mode);

        if (request.AccountId is not null)
        {
            ActiveChildViewModel!.FromAccount = AvailableAccounts.FirstOrDefault(a => a.Id == request.AccountId.Value);
        }

        if (request.Name is not null)
        {
            Name = request.Name;
        }

        if (request.CategoryId is not null)
        {
            Category = AvailableCategories.FirstOrDefault(c => c.Id == request.CategoryId.Value);
        }

        if (request.Date is not null)
        {
            Date = request.Date.Value;
        }

        if (request.DefaultFromFiatValue is not null)
        {
            ActiveChildViewModel!.FromAccountFiatValue = request.DefaultFromFiatValue;
        }

        if (request.FixedExpenseReference is not null)
        {
            TransactionFixedExpenseReference = request.FixedExpenseReference;
            FixedExpense = await _queryDispatcher!.DispatchAsync(new GetFixedExpenseQuery
            {
                FixedExpenseId = TransactionFixedExpenseReference.FixedExpenseId.Value
            });
        }

        if (request.Notes is not null)
        {
            Notes = request.Notes;
        }
    }

    private async Task OnBindParameterForEditingAsync(Request request)
    {
        var transaction = await _queryDispatcher!.DispatchAsync(new GetTransactionByIdQuery
        {
            TransactionId = request.TransactionId!.Value
        });

        if (transaction is null)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_TransactionNotFound, GetWindow!());
            Close();
            return;
        }

        if (request.CopyTransaction)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var lastDate = _lastTransactionDateState.LastDate.HasValue
                ? DateOnly.FromDateTime(_lastTransactionDateState.LastDate.Value)
                : (DateOnly?)null;
            var copyDate = lastDate.HasValue && lastDate.Value < today
                ? lastDate.Value
                : today;
            Date = copyDate.ToDateTime(TimeOnly.MinValue);
            WindowTitle = language.ManageTransactions_CopyTitle;
        }
        else
        {
            Date = transaction.Date.ToDateTime(TimeOnly.MinValue);
            _transactionId = new TransactionId(transaction.Id);

            WindowTitle = language.ManageTransactions_EditTitle;
            if (transaction.FixedExpenseReference is not null)
            {
                TransactionFixedExpenseReference = new TransactionFixedExpenseReference(
                    new FixedExpenseId(transaction.FixedExpenseReference.FixedExpenseId),
                    transaction.FixedExpenseReference.ReferenceDate);
                FixedExpense = await _queryDispatcher!.DispatchAsync(new GetFixedExpenseQuery
                {
                    FixedExpenseId = TransactionFixedExpenseReference.FixedExpenseId.Value
                });
            }
        }

        Name = transaction.Name;
        Notes = transaction.Notes ?? string.Empty;
        Category = AvailableCategories.FirstOrDefault(c => c.Id == transaction.CategoryId);

        var values = _transactionDetailsBuilder!.LoadFromDto(transaction.Details, AvailableAccounts);
        SetActiveChild(values.SelectedMode);

        ActiveChildViewModel!.FromAccount = AvailableAccounts.FirstOrDefault(a => a.Id == transaction.Details.FromAccountId);
        ActiveChildViewModel!.FromAccountBtcValue = values.FromAccountBtcValue;
        ActiveChildViewModel!.FromAccountFiatValue = values.FromAccountFiatValue;

        if (ActiveChildViewModel is TransferTransactionEditorViewModel transfer)
        {
            transfer.ToAccount = values.ToAccount;
            transfer.ToAccountBtcValue = values.ToAccountBtcValue;
            transfer.ToAccountFiatValue = values.ToAccountFiatValue;
        }
        else if (ActiveChildViewModel is DebtTransactionEditorViewModel debt)
        {
            debt.IsEditing = !request.CopyTransaction;
        }

        if (!request.CopyTransaction && transaction.AutoSatAmountDetails is not null)
        {
            IsAutoSatAmount = transaction.AutoSatAmountDetails.IsAutoSatAmount;

            SatAmountStateDescription = transaction.AutoSatAmountDetails.SatAmountState switch
            {
                nameof(SatAmountState.Processed) => $"{transaction.AutoSatAmountDetails.SatAmountSats} sats",
                nameof(SatAmountState.Manual) => language.SatAmountState_Manual,
                nameof(SatAmountState.Pending) => language.SatAmountState_Pending,
                nameof(SatAmountState.Missing) => language.SatAmountState_Missing,
                _ => transaction.AutoSatAmountDetails.SatAmountState
            };
        }
    }

    private void SetActiveChild(TransactionTypes mode)
    {
        var previous = ActiveChildViewModel;

        TransactionEditorChildViewModel child = mode switch
        {
            TransactionTypes.Debt => new DebtTransactionEditorViewModel(),
            TransactionTypes.Credit => new CreditTransactionEditorViewModel(),
            TransactionTypes.Transfer => new TransferTransactionEditorViewModel(
                _currencySettings,
                _runner,
                _loggerFactory.CreateLogger<TransferTransactionEditorViewModel>()),
            _ => throw new NotSupportedException($"Transaction mode {mode} is not supported")
        };

        child.AvailableAccounts = AvailableAccounts;
        child.AvailableCategories = AvailableCategories;
        child.OpenCalculatorCommand = OpenCalculatorCommand;

        CopyFromPreviousChild(previous, child);

        ActiveChildViewModel = child;
        SelectedMode = mode;
    }

    private static void CopyFromPreviousChild(TransactionEditorChildViewModel? previous, TransactionEditorChildViewModel child)
    {
        if (previous is null)
            return;

        child.FromAccount = previous.FromAccount;
        child.FromAccountBtcValue = previous.FromAccountBtcValue;
        child.FromAccountFiatValue = previous.FromAccountFiatValue;
        child.FromBtcIsBitcoinMode = previous.FromBtcIsBitcoinMode;

        if (child is TransferTransactionEditorViewModel transfer && previous is TransferTransactionEditorViewModel previousTransfer)
        {
            transfer.ToAccount = previousTransfer.ToAccount;
            transfer.ToAccountBtcValue = previousTransfer.ToAccountBtcValue;
            transfer.ToAccountFiatValue = previousTransfer.ToAccountFiatValue;
            transfer.ToBtcIsBitcoinMode = previousTransfer.ToBtcIsBitcoinMode;
        }
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

        var child = ActiveChildViewModel;
        if (child is null)
            return;

        if ((child.FromAccountBtcValue is null || child.FromAccountBtcValue == BtcValue.Empty) && value.SatAmount is not null)
        {
            if (!TransferSelected)
            {
                var newMode = value.SatAmount.Value < 0 ? TransactionTypes.Debt : TransactionTypes.Credit;
                SetActiveChild(newMode);
                child = ActiveChildViewModel;
            }

            child!.FromAccountBtcValue =
                BtcValue.ParseSats(value.SatAmount.Value < 0 ? -value.SatAmount.Value : value.SatAmount.Value);
        }

        if ((child.FromAccountFiatValue is null || child.FromAccountFiatValue == FiatValue.Empty) && value.FiatAmount is not null)
        {
            if (!TransferSelected)
            {
                var newMode = value.FiatAmount.Value < 0 ? TransactionTypes.Debt : TransactionTypes.Credit;
                SetActiveChild(newMode);
                child = ActiveChildViewModel;
            }

            child!.FromAccountFiatValue =
                FiatValue.New(value.FiatAmount.Value < 0 ? -value.FiatAmount.Value : value.FiatAmount.Value);
        }
    }

    private async Task FetchCategoriesAsync()
    {
        var result = await _queryDispatcher!.DispatchAsync(new GetCategoriesQuery());
        var categories = result.Items.OrderBy(x => x.Name);

        AvailableCategories.Clear();
        foreach (var category in categories)
            AvailableCategories.Add(category);
    }

    private async Task FetchAccountsAsync()
    {
        var accounts = await _queryDispatcher!.DispatchAsync(new GetAccountsQuery(_displaySettings.ShowHiddenAccounts));

        AvailableAccounts.Clear();
        foreach (var account in accounts)
            AvailableAccounts.Add(account);
    }

    #region Change main mode

    [RelayCommand]
    private void SwitchToDebt()
    {
        SetActiveChild(TransactionTypes.Debt);
    }

    [RelayCommand]
    private void SwitchToCredit()
    {
        SetActiveChild(TransactionTypes.Credit);
    }

    [RelayCommand]
    private void SwitchToTransfer()
    {
        SetActiveChild(TransactionTypes.Transfer);
    }

    #endregion

    [RelayCommand]
    private void ProcessEnter()
    {
        if (ActiveChildViewModel?.ShouldProcessEnter == true)
        {
            OkCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void NextDay()
    {
        Date = Date?.AddDays(1);
    }

    [RelayCommand]
    private void PreviousDay()
    {
        Date = Date?.AddDays(-1);
    }

    [RelayCommand]
    private void SelectToday()
    {
        Date = DateTime.Now.Date;
    }

    [RelayCommand]
    private void Close()
    {
        CloseWindow?.Invoke();
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();
        ActiveChildViewModel?.ValidateAllProperties();

        if (HasErrors || (ActiveChildViewModel?.HasErrors ?? false))
        {
            return;
        }

        DateTime? newTransactionDate = null;

        if (_transactionId != null)
        {
            // Edit existing transaction
            var detailsDto = BuildTransactionDetailsDtoFromForm();
            var result = await _commandDispatcher!.DispatchAsync(new EditTransactionCommand
            {
                TransactionId = _transactionId.Value,
                Date = DateOnly.FromDateTime(Date!.Value),
                Name = Name,
                CategoryId = Category!.Id,
                Details = detailsDto,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                FixedExpenseId = TransactionFixedExpenseReference?.FixedExpenseId.Value,
                FixedExpenseReferenceDate = TransactionFixedExpenseReference?.ReferenceDate
            });

            if (result.IsFailure)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                return;
            }
        }
        else
        {
            // Create new transaction(s)
            if (ActiveChildViewModel is DebtTransactionEditorViewModel debtChild &&
                debtChild.UseInstallments && debtChild.InstallmentCount >= 2 && DebtSelected)
            {
                var dates = InstallmentDateCalculator.CalculateInstallmentDates(
                    DateOnly.FromDateTime(Date!.Value), debtChild.InstallmentCount).ToList();

                var groupId = new GroupId();

                for (var i = 0; i < dates.Count; i++)
                {
                    var installmentName = $"{Name} ({i + 1}/{debtChild.InstallmentCount})";
                    var detailsDto = BuildTransactionDetailsDtoFromForm();

                    var result = await _commandDispatcher!.DispatchAsync(new AddTransactionCommand
                    {
                        Date = dates[i],
                        Name = installmentName,
                        CategoryId = Category!.Id,
                        Details = detailsDto,
                        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                        FixedExpenseId = TransactionFixedExpenseReference?.FixedExpenseId.Value,
                        FixedExpenseReferenceDate = TransactionFixedExpenseReference?.ReferenceDate,
                        GroupId = groupId.Value
                    });

                    if (result.IsFailure)
                    {
                        await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                        return;
                    }

                    if (i == 0)
                    {
                        newTransactionDate = dates[i].ToDateTime(TimeOnly.MinValue);
                    }
                }
            }
            else
            {
                var detailsDto = BuildTransactionDetailsDtoFromForm();
                var result = await _commandDispatcher!.DispatchAsync(new AddTransactionCommand
                {
                    Date = DateOnly.FromDateTime(Date!.Value),
                    Name = Name,
                    CategoryId = Category!.Id,
                    Details = detailsDto,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                    FixedExpenseId = TransactionFixedExpenseReference?.FixedExpenseId.Value,
                    FixedExpenseReferenceDate = TransactionFixedExpenseReference?.ReferenceDate
                });

                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                    return;
                }

                newTransactionDate = result.Value!.Date.ToDateTime(TimeOnly.MinValue);
            }
        }

        _lastTransactionDateState.LastDate = Date!.Value;
        CloseDialog?.Invoke(new Response(true, newTransactionDate));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }


    [RelayCommand]
    private async Task OpenCalculator(object parameter)
    {
        var child = ActiveChildViewModel;
        if (child is null)
            return;

        var transferChild = child as TransferTransactionEditorViewModel;

        // Determine the default currency based on the field and current BTC/Sats mode
        string? defaultCurrencyCode = parameter switch
        {
            "FromBtc" => child.FromBtcIsBitcoinMode ? "BTC" : "SATS",
            "ToBtc" => transferChild?.ToBtcIsBitcoinMode == true ? "BTC" : "SATS",
            "FromFiat" => child.FromAccount?.Currency,
            "ToFiat" => transferChild?.ToAccount?.Currency,
            _ => null
        };

        var request = new ConversionCalculatorViewModel.Request(
            ResponseMode: true,
            DefaultCurrencyCode: defaultCurrencyCode);

        var window =
            (ConversionCalculatorView)await _modalFactory.CreateAsync(ApplicationModalNames.ConversionCalculator, GetWindow!(), request)!;

        var result = await window.ShowDialogSafeAsync<ConversionCalculatorViewModel.Response?>(GetWindow!());

        if (result is null)
            return;

        switch (parameter)
        {
            case "FromFiat":
                child.FromAccountFiatValue = FiatValue.New(result.Result.GetValueOrDefault());
                break;
            case "ToFiat":
                transferChild!.ToAccountFiatValue = FiatValue.New(result.Result.GetValueOrDefault());
                break;
            case "FromBtc":
                if (result.SelectedCurrencyCode == "SATS")
                    child.FromAccountBtcValue = BtcValue.New((long)result.Result.GetValueOrDefault());
                else
                    child.FromAccountBtcValue = BtcValue.ParseBitcoin(result.Result.GetValueOrDefault());
                break;
            case "ToBtc":
                if (result.SelectedCurrencyCode == "SATS")
                    transferChild!.ToAccountBtcValue = BtcValue.New((long)result.Result.GetValueOrDefault());
                else
                    transferChild!.ToAccountBtcValue = BtcValue.ParseBitcoin(result.Result.GetValueOrDefault());
                break;
        }
    }

    private TransactionDetailsDto BuildTransactionDetailsDtoFromForm()
    {
        var child = ActiveChildViewModel;
        var transfer = child as TransferTransactionEditorViewModel;

        return _transactionDetailsBuilder!.BuildDto(new TransactionFormSnapshot(
            SelectedMode,
            child!.FromAccount!,
            transfer?.ToAccount,
            child.FromAccountBtcValue,
            child.FromAccountFiatValue,
            transfer?.ToAccountBtcValue,
            transfer?.ToAccountFiatValue,
            transfer?.AccountsAreSameTypeAndCurrency ?? false));
    }

    public Task<IEnumerable<object>> GetTransactionTermsAsync(string? term, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(term)
            ? Task.FromResult(Enumerable.Empty<object>())
            : Task.FromResult<IEnumerable<object>>(_transactionTermService!.Search(term, 5));
    }

    public record Request
    {
        public TransactionId? TransactionId { get; init; }

        public DateTime? Date { get; init; }
        public string? Name { get; set; }
        public CategoryId? CategoryId { get; set; }
        public AccountId? AccountId { get; init; }
        public FiatValue? DefaultFromFiatValue { get; init; }
        public TransactionFixedExpenseReference? FixedExpenseReference { get; set; }

        public bool CopyTransaction { get; init; }

        public string? Notes { get; init; }
        public TransactionTypes? DefaultMode { get; init; }
    }

    public record Response(bool Ok, DateTime? TransactionDate);
}
