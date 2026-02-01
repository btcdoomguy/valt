using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Core.Modules.Budget.Transactions.Services;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.UI.Base;
using static Valt.UI.Base.TaskExtensions;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.ConversionCalculator;
using Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

namespace Valt.UI.Views.Main.Modals.TransactionEditor;

public partial class TransactionEditorViewModel : ValtModalValidatorViewModel, IValidatableObject
{
    // Account mode constants - used for UI binding to show/hide fields based on transfer type
    private const string AccountModeFiat = "Fiat";
    private const string AccountModeBitcoin = "Bitcoin";
    private const string AccountModeFiatToFiat = "FiatToFiat";
    private const string AccountModeFiatToBitcoin = "FiatToBitcoin";
    private const string AccountModeBitcoinToBitcoin = "BitcoinToBitcoin";
    private const string AccountModeBitcoinToFiat = "BitcoinToFiat";

    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly ITransactionTermService? _transactionTermService;
    private readonly IModalFactory _modalFactory = null!;
    private readonly CurrencySettings _currencySettings = null!;
    private readonly DisplaySettings _displaySettings = null!;

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

    [Required(ErrorMessage = "Origin account is required")]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FromAccountType), nameof(FromAccountIsBtc), nameof(ShowTransferValueField))]
    private AccountDTO? _fromAccount;

    [ObservableProperty]
    [CustomValidation(typeof(TransactionEditorViewModel), nameof(ValidateToAccount))]
    [NotifyPropertyChangedFor(nameof(ToAccountType), nameof(ToAccountIsBtc),
        nameof(ShowTransferValueField))]
    private AccountDTO? _toAccount;

    [CustomValidation(typeof(TransactionEditorViewModel), nameof(ValidateFromAccountBtcValue))]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstallmentValueText))]
    private BtcValue? _fromAccountBtcValue = BtcValue.Empty;

    [CustomValidation(typeof(TransactionEditorViewModel), nameof(ValidateFromAccountFiatValue))]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstallmentValueText))]
    private FiatValue? _fromAccountFiatValue = FiatValue.Empty;

    [CustomValidation(typeof(TransactionEditorViewModel), nameof(ValidateToAccountBtcValue))] [ObservableProperty]
    private BtcValue? _toAccountBtcValue = BtcValue.Empty;

    [CustomValidation(typeof(TransactionEditorViewModel), nameof(ValidateToAccountFiatValue))] [ObservableProperty]
    private FiatValue? _toAccountFiatValue = FiatValue.Empty;

    [ObservableProperty] private string _transferRate = string.Empty;

    [ObservableProperty] private bool _isFromBtcInputFocused;
    [ObservableProperty] private bool _isFromFiatInputFocused;
    [ObservableProperty] private bool _isToBtcInputFocused;
    [ObservableProperty] private bool _isToFiatInputFocused;

    [ObservableProperty] private bool _fromBtcIsBitcoinMode;
    [ObservableProperty] private bool _toBtcIsBitcoinMode;

    [NotifyPropertyChangedFor(nameof(IsBoundToFixedExpense), nameof(BoundToFixedExpenseCaption), nameof(HasMetadata))]
    [ObservableProperty]
    private TransactionFixedExpenseReference? _transactionFixedExpenseReference;

    [NotifyPropertyChangedFor(nameof(IsBoundToFixedExpense), nameof(BoundToFixedExpenseCaption), nameof(HasMetadata))]
    [ObservableProperty]
    private FixedExpenseDTO? _fixedExpense;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInstallmentCount), nameof(InstallmentValueText))]
    private bool _useInstallments;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstallmentValueText))]
    private int _installmentCount = 2;

    #endregion

    #region Custom validations

    public static ValidationResult ValidateFromAccountBtcValue(BtcValue? btcValue, ValidationContext context)
    {
        var instance = (TransactionEditorViewModel)context.ObjectInstance;

        var shouldValidate = instance.FromAccountType == AccountTypes.Bitcoin;

        if (shouldValidate && (btcValue is null || btcValue.Btc == 0))
        {
            return new ValidationResult("Value is required");
        }

        return ValidationResult.Success!;
    }

    public static ValidationResult ValidateFromAccountFiatValue(FiatValue? fiatValue, ValidationContext context)
    {
        var instance = (TransactionEditorViewModel)context.ObjectInstance;

        var shouldValidate = instance.FromAccountType == AccountTypes.Fiat;

        if (shouldValidate && (fiatValue is null || fiatValue.Value == 0))
        {
            return new ValidationResult("Value is required");
        }

        return ValidationResult.Success!;
    }

    public static ValidationResult ValidateToAccountBtcValue(BtcValue? btcValue, ValidationContext context)
    {
        var instance = (TransactionEditorViewModel)context.ObjectInstance;

        var shouldValidate = instance is
        {
            SelectedMode: TransactionTypes.Transfer, FromAccountType: AccountTypes.Fiat,
            ToAccountType: AccountTypes.Bitcoin
        };

        if (shouldValidate && (btcValue is null || btcValue.Btc == 0))
        {
            return new ValidationResult("Value is required");
        }

        return ValidationResult.Success!;
    }

    public static ValidationResult ValidateToAccountFiatValue(FiatValue? fiatValue, ValidationContext context)
    {
        var instance = (TransactionEditorViewModel)context.ObjectInstance;

        var shouldValidate = instance is {
                SelectedMode: TransactionTypes.Transfer, FromAccountType: AccountTypes.Fiat,
                ToAccountType: AccountTypes.Fiat, AccountsAreSameTypeAndCurrency: false
            } or
            {
                SelectedMode: TransactionTypes.Transfer, FromAccountType: AccountTypes.Bitcoin,
                ToAccountType: AccountTypes.Fiat
            };

        if (shouldValidate && (fiatValue is null || fiatValue.Value == 0))
        {
            return new ValidationResult("Value is required");
        }

        return ValidationResult.Success!;
    }

    public static ValidationResult ValidateToAccount(AccountDTO? account, ValidationContext context)
    {
        var instance = (TransactionEditorViewModel)context.ObjectInstance;

        var shouldValidate = instance is { SelectedMode: TransactionTypes.Transfer };

        if (shouldValidate && account is null)
        {
            return new ValidationResult("Destination account is required");
        }

        return ValidationResult.Success!;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        var fromBitcoinValue = FromAccountType == AccountTypes.Bitcoin;

        if (fromBitcoinValue && (FromAccountBtcValue is null || FromAccountBtcValue.Btc == 0))
        {
            results.Add(new ValidationResult(
                "Origin value is required",
                new[] { nameof(FromAccountBtcValue) }));
        }

        if (!fromBitcoinValue && (FromAccountFiatValue is null || FromAccountFiatValue.Value == 0))
        {
            results.Add(new ValidationResult(
                "Origin value is required",
                new[] { nameof(FromAccountFiatValue) }));
        }

        if (SelectedMode == TransactionTypes.Transfer)
        {
            if (ToAccount is null)
            {
                results.Add(new ValidationResult(
                    "Destination account is required",
                    new[] { nameof(ToAccount) }));
            }

            var toBitcoinValue = ToAccountType == AccountTypes.Bitcoin;

            if (toBitcoinValue && (ToAccountBtcValue is null || ToAccountBtcValue.Btc == 0))
            {
                results.Add(new ValidationResult(
                    "Destination value is required",
                    new[] { nameof(ToAccountBtcValue) }));
            }

            if (!toBitcoinValue && (ToAccountFiatValue is null || ToAccountFiatValue.Value == 0))
            {
                results.Add(new ValidationResult(
                    "Destination value is required",
                    new[] { nameof(ToAccountFiatValue) }));
            }
        }

        return results;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebtSelected), nameof(CreditSelected), nameof(TransferSelected),
        nameof(ShowTransferValueField), nameof(ShowInstallmentsOption), nameof(ShowInstallmentCount))]
    private TransactionTypes _selectedMode;

    public bool DebtSelected => SelectedMode == TransactionTypes.Debt;
    public bool CreditSelected => SelectedMode == TransactionTypes.Credit;
    public bool TransferSelected => SelectedMode == TransactionTypes.Transfer;

    public bool ShowTransferValueField =>
        SelectedMode == TransactionTypes.Transfer && !AccountsAreSameTypeAndCurrency;

    public bool ShowInstallmentsOption => DebtSelected && _transactionId is null;
    public bool ShowInstallmentCount => UseInstallments && ShowInstallmentsOption;

    public string InstallmentValueText
    {
        get
        {
            if (!UseInstallments || InstallmentCount < 2)
                return string.Empty;

            var value = FromAccountIsBtc
                ? FromAccountBtcValue?.Sats.ToString() ?? "0"
                : FromAccountFiatValue?.Value.ToString("N2") ?? "0";

            return $"{InstallmentCount}x of {value}";
        }
    }

    private bool AccountsAreSameTypeAndCurrency
    {
        get
        {
            if (FromAccount is null || ToAccount is null)
                return false;

            if (FromAccountType != ToAccountType)
                return false;

            if (FromAccountIsBtc && ToAccountIsBtc) return true;

            return FromAccount.Currency == ToAccount.Currency;
        }
    }

    public AccountTypes FromAccountType
    {
        get
        {
            if (FromAccount is null)
                return AccountTypes.Fiat;

            var account = AvailableAccounts.FirstOrDefault(a => a.Id == FromAccount.Id);

            if (account is null)
                return AccountTypes.Fiat;

            return Enum.Parse<AccountTypes>(account.Type);
        }
    }

    public bool FromAccountIsBtc => FromAccountType == AccountTypes.Bitcoin;
    public bool ToAccountIsBtc => ToAccountType == AccountTypes.Bitcoin;

    public AccountTypes? ToAccountType
    {
        get
        {
            if (ToAccount is null)
                return null;

            var account = AvailableAccounts.FirstOrDefault(a => a.Id == ToAccount.Id);

            if (account is null)
                return null;

            return Enum.Parse<AccountTypes>(account.Type);
        }
    }

    public string CurrentAccountMode
    {
        get
        {
            return FromAccountType switch
            {
                AccountTypes.Fiat when ToAccountType == AccountTypes.Fiat => AccountModeFiatToFiat,
                AccountTypes.Fiat when ToAccountType == AccountTypes.Bitcoin => AccountModeFiatToBitcoin,
                AccountTypes.Fiat => AccountModeFiat,
                AccountTypes.Bitcoin when ToAccountType == AccountTypes.Bitcoin => AccountModeBitcoinToBitcoin,
                AccountTypes.Bitcoin when ToAccountType == AccountTypes.Fiat => AccountModeBitcoinToFiat,
                AccountTypes.Bitcoin => AccountModeBitcoin,
                _ => ""
            };
        }
    }

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
        if (Design.IsDesignMode)
        {
            Date = DateTime.Now.Date;
        }
    }

    public TransactionEditorViewModel(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        ITransactionTermService transactionTermService,
        IModalFactory modalFactory,
        CurrencySettings currencySettings,
        DisplaySettings displaySettings)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _transactionTermService = transactionTermService;
        _modalFactory = modalFactory;
        _currencySettings = currencySettings;
        _displaySettings = displaySettings;

        InitializeAsync().SafeFireAndForget(callerName: nameof(InitializeAsync));
    }

    private async Task InitializeAsync()
    {
        Date = DateTime.Now.Date;
        await FetchCategoriesAsync();
        await FetchAccountsAsync();
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not Request request)
            return;

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

        if (request.AccountId is not null)
        {
            FromAccount = AvailableAccounts.FirstOrDefault(a => a.Id == request.AccountId.Value);
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
            FromAccountFiatValue = request.DefaultFromFiatValue;
        }

        if (request.FixedExpenseReference is not null)
        {
            TransactionFixedExpenseReference = request.FixedExpenseReference;
            FixedExpense = await _queryDispatcher!.DispatchAsync(new GetFixedExpenseQuery
            {
                FixedExpenseId = TransactionFixedExpenseReference.FixedExpenseId.Value
            });
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

        Date = transaction.Date.ToDateTime(TimeOnly.MinValue);
        if (!request.CopyTransaction)
        {
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
        else
        {
            WindowTitle = language.ManageTransactions_CopyTitle;
        }

        Name = transaction.Name;
        Notes = transaction.Notes ?? string.Empty;
        Category = AvailableCategories.FirstOrDefault(c => c.Id == transaction.CategoryId);
        FromAccount = AvailableAccounts.FirstOrDefault(a => a.Id == transaction.Details.FromAccountId);

        LoadTransactionDetailsFromDto(transaction.Details);

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

    private void LoadTransactionDetailsFromDto(TransactionDetailsDto details)
    {
        switch (details)
        {
            case FiatTransactionDto fiat:
                SelectedMode = fiat.IsCredit ? TransactionTypes.Credit : TransactionTypes.Debt;
                FromAccountFiatValue = FiatValue.New(fiat.Amount);
                break;
            case BitcoinTransactionDto btc:
                SelectedMode = btc.IsCredit ? TransactionTypes.Credit : TransactionTypes.Debt;
                FromAccountBtcValue = BtcValue.New(btc.AmountSats);
                break;
            case FiatToFiatTransferDto fiatToFiat:
                SelectedMode = TransactionTypes.Transfer;
                ToAccount = AvailableAccounts.FirstOrDefault(a => a.Id == fiatToFiat.ToAccountId);
                FromAccountFiatValue = FiatValue.New(fiatToFiat.FromAmount);
                ToAccountFiatValue = AccountsAreSameTypeAndCurrency
                    ? FromAccountFiatValue
                    : FiatValue.New(fiatToFiat.ToAmount);
                break;
            case BitcoinToBitcoinTransferDto btcToBtc:
                SelectedMode = TransactionTypes.Transfer;
                ToAccount = AvailableAccounts.FirstOrDefault(a => a.Id == btcToBtc.ToAccountId);
                FromAccountBtcValue = BtcValue.New(btcToBtc.AmountSats);
                ToAccountBtcValue = BtcValue.New(btcToBtc.AmountSats);
                break;
            case FiatToBitcoinTransferDto fiatToBtc:
                SelectedMode = TransactionTypes.Transfer;
                ToAccount = AvailableAccounts.FirstOrDefault(a => a.Id == fiatToBtc.ToAccountId);
                FromAccountFiatValue = FiatValue.New(fiatToBtc.FromFiatAmount);
                ToAccountBtcValue = BtcValue.New(fiatToBtc.ToSatsAmount);
                break;
            case BitcoinToFiatTransferDto btcToFiat:
                SelectedMode = TransactionTypes.Transfer;
                ToAccount = AvailableAccounts.FirstOrDefault(a => a.Id == btcToFiat.ToAccountId);
                FromAccountBtcValue = BtcValue.New(btcToFiat.FromSatsAmount);
                ToAccountFiatValue = FiatValue.New(btcToFiat.ToFiatAmount);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(details), details.GetType().Name, "Unknown transaction details type");
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

        if ((FromAccountBtcValue is null || FromAccountBtcValue == BtcValue.Empty) && value.SatAmount is not null)
        {
            if (!TransferSelected)
            {
                SelectedMode = value.SatAmount.Value < 0 ? TransactionTypes.Debt : TransactionTypes.Credit;
            }

            FromAccountBtcValue =
                BtcValue.ParseSats(value.SatAmount.Value < 0 ? -value.SatAmount.Value : value.SatAmount.Value);
        }

        if ((FromAccountFiatValue is null || FromAccountFiatValue == FiatValue.Empty) && value.FiatAmount is not null)
        {
            if (!TransferSelected)
            {
                SelectedMode = value.FiatAmount.Value < 0 ? TransactionTypes.Debt : TransactionTypes.Credit;
            }

            FromAccountFiatValue =
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
        SelectedMode = TransactionTypes.Debt;
        ToAccount = null;
    }

    [RelayCommand]
    private void SwitchToCredit()
    {
        SelectedMode = TransactionTypes.Credit;
        ToAccount = null;
        UseInstallments = false;
    }

    [RelayCommand]
    private void SwitchToTransfer()
    {
        SelectedMode = TransactionTypes.Transfer;
        UseInstallments = false;
    }

    #endregion

    [RelayCommand]
    private void ProcessEnter()
    {
        if (IsFromBtcInputFocused || IsFromFiatInputFocused && !ShowTransferValueField)
        {
            OkCommand.Execute(null);
        }
        else if (IsToBtcInputFocused || IsToFiatInputFocused)
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

        if (!HasErrors)
        {
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
                if (UseInstallments && InstallmentCount >= 2 && DebtSelected)
                {
                    var dates = InstallmentDateCalculator.CalculateInstallmentDates(
                        DateOnly.FromDateTime(Date!.Value), InstallmentCount).ToList();

                    var groupId = new GroupId();

                    for (var i = 0; i < dates.Count; i++)
                    {
                        var installmentName = $"{Name} ({i + 1}/{InstallmentCount})";
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

            CloseDialog?.Invoke(new Response(true, newTransactionDate));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }


    [RelayCommand]
    private async Task OpenCalculator(object parameter)
    {
        // Determine the default currency based on the field and current BTC/Sats mode
        string? defaultCurrencyCode = parameter switch
        {
            "FromBtc" => FromBtcIsBitcoinMode ? "BTC" : "SATS",
            "ToBtc" => ToBtcIsBitcoinMode ? "BTC" : "SATS",
            "FromFiat" => FromAccount?.Currency,
            "ToFiat" => ToAccount?.Currency,
            _ => null
        };

        var request = new ConversionCalculatorViewModel.Request(
            ResponseMode: true,
            DefaultCurrencyCode: defaultCurrencyCode);

        var window =
            (ConversionCalculatorView)await _modalFactory.CreateAsync(ApplicationModalNames.ConversionCalculator, GetWindow!(), request)!;

        var result = await window.ShowDialog<ConversionCalculatorViewModel.Response?>(GetWindow!());

        if (result is null)
            return;

        switch (parameter)
        {
            case "FromFiat":
                FromAccountFiatValue = FiatValue.New(result.Result.GetValueOrDefault());
                break;
            case "ToFiat":
                ToAccountFiatValue = FiatValue.New(result.Result.GetValueOrDefault());
                break;
            case "FromBtc":
                if (result.SelectedCurrencyCode == "SATS")
                    FromAccountBtcValue = BtcValue.New((long)result.Result.GetValueOrDefault());
                else
                    FromAccountBtcValue = BtcValue.ParseBitcoin(result.Result.GetValueOrDefault());
                break;
            case "ToBtc":
                if (result.SelectedCurrencyCode == "SATS")
                    ToAccountBtcValue = BtcValue.New((long)result.Result.GetValueOrDefault());
                else
                    ToAccountBtcValue = BtcValue.ParseBitcoin(result.Result.GetValueOrDefault());
                break;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(SelectedMode) or nameof(ToAccount) or nameof(ToAccountBtcValue)
            or nameof(ToAccountFiatValue) or nameof(FromAccount) or nameof(FromAccountBtcValue)
            or nameof(FromAccountFiatValue))
        {
            UpdateTransferRateAsync().SafeFireAndForget(callerName: nameof(UpdateTransferRateAsync));
        }
    }

    private async Task UpdateTransferRateAsync()
    {
        TransferRate = "Calculating...";

        var result = await Task.Run(() => DoUpdateTransferRate());

        TransferRate = result;
    }

    private string DoUpdateTransferRate()
    {
        if (!ShowTransferValueField || ToAccount is null)
            return string.Empty;

        //if one of the accounts is BTC, so show the price of 1 BTC in the fiat currency
        if (FromAccountType == AccountTypes.Bitcoin || ToAccountType == AccountTypes.Bitcoin)
        {
            var sats = FromAccountType == AccountTypes.Bitcoin ? FromAccountBtcValue : ToAccountBtcValue;
            var fiat = FromAccountType == AccountTypes.Bitcoin ? ToAccountFiatValue : FromAccountFiatValue;

            if (sats is null || fiat is null || sats.Sats == 0 || fiat.Value == 0)
                return string.Empty;

            var rate = fiat.Value / sats.Btc;

            var fiatRate = FiatValue.New((decimal)rate);

            return fiatRate.ToCurrencyString(FromAccountType == AccountTypes.Bitcoin
                ? FiatCurrency.GetFromCode(ToAccount!.Currency!)
                : FiatCurrency.GetFromCode(FromAccount!.Currency!));
        }

        if (FromAccountType == AccountTypes.Fiat && ToAccountType == AccountTypes.Fiat)
        {
            var fiatRate1 = FromAccountFiatValue!.Value;
            var fiatRate2 = ToAccountFiatValue!.Value;

            if (fiatRate1 == 0 || fiatRate2 == 0)
                return string.Empty;

            var mainCurrency = FiatCurrency.GetFromCode(FromAccount!.Currency!);

            decimal rate;
            //use the main currency preferable as the main currency for the rate
            if (ToAccount!.Currency == _currencySettings.MainFiatCurrency)
            {
                rate = fiatRate2 / fiatRate1;
                mainCurrency = FiatCurrency.GetFromCode(ToAccount!.Currency!);
            }
            else
                rate = fiatRate1 / fiatRate2;

            var fiatRate = FiatValue.New(rate);

            return fiatRate.ToCurrencyString(mainCurrency);
        }

        return string.Empty;
    }

    private TransactionDetailsDto BuildTransactionDetailsDtoFromForm()
    {
        if (SelectedMode == TransactionTypes.Transfer)
        {
            return CurrentAccountMode switch
            {
                "BitcoinToBitcoin" => new BitcoinToBitcoinTransferDto
                {
                    FromAccountId = FromAccount!.Id,
                    ToAccountId = ToAccount!.Id,
                    AmountSats = FromAccountBtcValue!.Sats
                },
                "BitcoinToFiat" => new BitcoinToFiatTransferDto
                {
                    FromAccountId = FromAccount!.Id,
                    ToAccountId = ToAccount!.Id,
                    FromSatsAmount = FromAccountBtcValue!.Sats,
                    ToFiatAmount = ToAccountFiatValue!.Value
                },
                "FiatToBitcoin" => new FiatToBitcoinTransferDto
                {
                    FromAccountId = FromAccount!.Id,
                    ToAccountId = ToAccount!.Id,
                    FromFiatAmount = FromAccountFiatValue!.Value,
                    ToSatsAmount = ToAccountBtcValue!.Sats
                },
                "FiatToFiat" => new FiatToFiatTransferDto
                {
                    FromAccountId = FromAccount!.Id,
                    ToAccountId = ToAccount!.Id,
                    FromAmount = FromAccountFiatValue!.Value,
                    ToAmount = AccountsAreSameTypeAndCurrency ? FromAccountFiatValue!.Value : ToAccountFiatValue!.Value
                },
                _ => throw new TransactionDetailsBuildException()
            };
        }

        return CurrentAccountMode switch
        {
            "Bitcoin" => new BitcoinTransactionDto
            {
                FromAccountId = FromAccount!.Id,
                AmountSats = FromAccountBtcValue!.Sats,
                IsCredit = SelectedMode == TransactionTypes.Credit
            },
            "Fiat" => new FiatTransactionDto
            {
                FromAccountId = FromAccount!.Id,
                Amount = FromAccountFiatValue!.Value,
                IsCredit = SelectedMode == TransactionTypes.Credit
            },
            _ => throw new TransactionDetailsBuildException()
        };
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
    }

    public record Response(bool Ok, DateTime? TransactionDate);
}