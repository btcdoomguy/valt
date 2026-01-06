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
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Modules.Budget.Categories.Queries.DTOs;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.MathExpression;
using Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

namespace Valt.UI.Views.Main.Modals.TransactionEditor;

public partial class TransactionEditorViewModel : ValtModalValidatorViewModel, IValidatableObject
{
    private readonly ITransactionRepository? _transactionRepository;
    private readonly IAccountQueries? _accountQueries;
    private readonly ICategoryQueries? _categoryQueries;
    private readonly IFixedExpenseQueries _fixedExpenseQueries;
    private readonly ITransactionTermService? _transactionTermService;
    private readonly IModalFactory _modalFactory;
    private readonly CurrencySettings _currencySettings;
    private readonly DisplaySettings _displaySettings;

    public AvaloniaList<CategoryDTO> AvailableCategories { get; set; } = new();
    public AvaloniaList<AccountDTO> AvailableAccounts { get; set; } = new();

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

    [CustomValidation(typeof(TransactionEditorViewModel), nameof(ValidateFromAccountBtcValue))] [ObservableProperty]
    private BtcValue? _fromAccountBtcValue = BtcValue.Empty;

    [CustomValidation(typeof(TransactionEditorViewModel), nameof(ValidateFromAccountFiatValue))] [ObservableProperty]
    private FiatValue? _fromAccountFiatValue = FiatValue.Empty;

    [CustomValidation(typeof(TransactionEditorViewModel), nameof(ValidateToAccountBtcValue))] [ObservableProperty]
    private BtcValue? _toAccountBtcValue = BtcValue.Empty;

    [CustomValidation(typeof(TransactionEditorViewModel), nameof(ValidateToAccountFiatValue))] [ObservableProperty]
    private FiatValue? _toAccountFiatValue = FiatValue.Empty;

    [ObservableProperty] private string _transferRate;

    [ObservableProperty] private bool _isFromBtcInputFocused;
    [ObservableProperty] private bool _isFromFiatInputFocused;
    [ObservableProperty] private bool _isToBtcInputFocused;
    [ObservableProperty] private bool _isToFiatInputFocused;

    [NotifyPropertyChangedFor(nameof(IsBoundToFixedExpense), nameof(BoundToFixedExpenseCaption), nameof(HasMetadata))]
    [ObservableProperty]
    private TransactionFixedExpenseReference? _transactionFixedExpenseReference;

    [NotifyPropertyChangedFor(nameof(IsBoundToFixedExpense), nameof(BoundToFixedExpenseCaption), nameof(HasMetadata))]
    [ObservableProperty]
    private FixedExpenseDto? _fixedExpense;

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
        nameof(ShowTransferValueField))]
    private TransactionTypes _selectedMode;

    public bool DebtSelected => SelectedMode == TransactionTypes.Debt;
    public bool CreditSelected => SelectedMode == TransactionTypes.Credit;
    public bool TransferSelected => SelectedMode == TransactionTypes.Transfer;

    public bool ShowTransferValueField =>
        SelectedMode == TransactionTypes.Transfer && !AccountsAreSameTypeAndCurrency;

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
                AccountTypes.Fiat when ToAccountType == AccountTypes.Fiat => "FiatToFiat",
                AccountTypes.Fiat when ToAccountType == AccountTypes.Bitcoin => "FiatToBitcoin",
                AccountTypes.Fiat => "Fiat",
                AccountTypes.Bitcoin when ToAccountType == AccountTypes.Bitcoin => "BitcoinToBitcoin",
                AccountTypes.Bitcoin when ToAccountType == AccountTypes.Fiat => "BitcoinToFiat",
                AccountTypes.Bitcoin => "Bitcoin",
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
        ITransactionRepository transactionRepository,
        IAccountQueries accountQueries,
        ICategoryQueries categoryQueries,
        IFixedExpenseQueries fixedExpenseQueries,
        ITransactionTermService transactionTermService,
        IModalFactory modalFactory,
        CurrencySettings currencySettings,
        DisplaySettings displaySettings)
    {
        _transactionRepository = transactionRepository;
        _accountQueries = accountQueries;
        _categoryQueries = categoryQueries;
        _fixedExpenseQueries = fixedExpenseQueries;
        _transactionTermService = transactionTermService;
        _modalFactory = modalFactory;
        _currencySettings = currencySettings;
        _displaySettings = displaySettings;

        _ = InitializeAsync();
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
            FixedExpense =
                await _fixedExpenseQueries.GetFixedExpenseAsync(TransactionFixedExpenseReference
                    .FixedExpenseId);
        }
    }

    private async Task OnBindParameterForEditingAsync(Request request)
    {
        var transaction =
            await _transactionRepository!.GetTransactionByIdAsync(request.TransactionId);

        if (transaction is null)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_TransactionNotFound, GetWindow!());
            Close();
            return;
        }

        Date = transaction.Date.ToValtDateTime();
        if (!request.CopyTransaction)
        {
            _transactionId = transaction.Id;

            WindowTitle = language.ManageTransactions_EditTitle;
            if (transaction.FixedExpenseReference is not null)
            {
                TransactionFixedExpenseReference = transaction.FixedExpenseReference;
                FixedExpense =
                    await _fixedExpenseQueries.GetFixedExpenseAsync(TransactionFixedExpenseReference
                        .FixedExpenseId);
            }
        }
        else
        {
            WindowTitle = language.ManageTransactions_CopyTitle;
        }

        Name = transaction.Name;
        Notes = transaction.Notes ?? string.Empty;
        Category = AvailableCategories.FirstOrDefault(c => c.Id == transaction.CategoryId.Value);
        FromAccount =
            AvailableAccounts.FirstOrDefault(a => a.Id == transaction.TransactionDetails.FromAccountId);
        if (transaction.TransactionDetails.TransactionType == TransactionTypes.Transfer &&
            transaction.TransactionDetails.ToAccountId is not null)
        {
            ToAccount =
                AvailableAccounts.FirstOrDefault(a => a.Id == transaction.TransactionDetails.ToAccountId);
        }

        SelectedMode = transaction.TransactionDetails.TransactionType;

        switch (transaction.TransactionDetails.TransferType)
        {
            case TransactionTransferTypes.Fiat:
                FromAccountFiatValue = ((FiatDetails)transaction.TransactionDetails).Amount;
                break;
            case TransactionTransferTypes.Bitcoin:
                FromAccountBtcValue = ((BitcoinDetails)transaction.TransactionDetails).Amount;
                break;
            case TransactionTransferTypes.FiatToFiat:
                FromAccountFiatValue = ((FiatToFiatDetails)transaction.TransactionDetails).FromAmount;
                ToAccountFiatValue = AccountsAreSameTypeAndCurrency
                    ? FromAccountFiatValue
                    : ((FiatToFiatDetails)transaction.TransactionDetails).ToAmount;
                break;
            case TransactionTransferTypes.BitcoinToBitcoin:
                FromAccountBtcValue = ((BitcoinToBitcoinDetails)transaction.TransactionDetails).Amount;
                ToAccountBtcValue = ((BitcoinToBitcoinDetails)transaction.TransactionDetails).Amount;
                break;
            case TransactionTransferTypes.FiatToBitcoin:
                FromAccountFiatValue = ((FiatToBitcoinDetails)transaction.TransactionDetails).FromAmount;
                ToAccountBtcValue = ((FiatToBitcoinDetails)transaction.TransactionDetails).ToAmount;
                break;
            case TransactionTransferTypes.BitcoinToFiat:
                FromAccountBtcValue = ((BitcoinToFiatDetails)transaction.TransactionDetails).FromAmount;
                ToAccountFiatValue = ((BitcoinToFiatDetails)transaction.TransactionDetails).ToAmount;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (transaction.HasAutoSatAmount)
        {
            var autoSatAmountDetails = transaction.AutoSatAmountDetails!;
            IsAutoSatAmount = autoSatAmountDetails.IsAutoSatAmount;

            SatAmountStateDescription = autoSatAmountDetails.SatAmountState == SatAmountState.Processed
                ? $"{autoSatAmountDetails.SatAmount!.Sats} sats"
                : autoSatAmountDetails.SatAmountState.ToString();
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
    }

    [RelayCommand]
    private void SwitchToTransfer()
    {
        SelectedMode = TransactionTypes.Transfer;
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
            Transaction? transaction;
            DateTime? newTransactionDate = null;
            if (_transactionId != null)
            {
                transaction = await _transactionRepository!.GetTransactionByIdAsync(_transactionId);

                await EditAsync(transaction!);
            }
            else
            {
                transaction = await CreateAsync();
                newTransactionDate = transaction.Date.ToValtDateTime();
            }

            await _transactionRepository!.SaveTransactionAsync(transaction!);
            CloseDialog?.Invoke(new Response(true, newTransactionDate));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    private Task<Transaction> CreateAsync()
    {
        var name = TransactionName.New(Name);
        var date = DateOnly.FromDateTime(Date!.Value);

        var transactionDetails = BuildTransactionDetailsFromForm();
        var notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes;

        return Task.FromResult(Transaction.New(date, name, Category!.Id, transactionDetails, notes,
            TransactionFixedExpenseReference));
    }

    private Task EditAsync(Transaction transaction)
    {
        var name = TransactionName.New(Name);
        var date = DateOnly.FromDateTime(Date!.Value);
        var transactionDetails = BuildTransactionDetailsFromForm();
        var notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes;

        transaction.ChangeDate(date);
        transaction.ChangeNameAndCategory(name, Category!.Id);
        transaction.SetFixedExpense(TransactionFixedExpenseReference);
        transaction.ChangeTransactionDetails(transactionDetails);
        transaction.ChangeNotes(notes);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenCalculator(object parameter)
    {
        var window =
            (MathExpressionView)await _modalFactory.CreateAsync(ApplicationModalNames.MathExpression, GetWindow!())!;

        var result = await window.ShowDialog<MathExpressionViewModel.Response?>(GetWindow!());

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
                FromAccountBtcValue = BtcValue.ParseBitcoin(result.Result.GetValueOrDefault());
                break;
            case "ToBtc":
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
            _ = UpdateTransferRateAsync();
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

    private TransactionDetails BuildTransactionDetailsFromForm()
    {
        if (SelectedMode == TransactionTypes.Transfer)
        {
            switch (CurrentAccountMode)
            {
                case "BitcoinToBitcoin":
                    return new BitcoinToBitcoinDetails(FromAccount!.Id, ToAccount!.Id, FromAccountBtcValue!);
                case "BitcoinToFiat":
                    return new BitcoinToFiatDetails(FromAccount!.Id, ToAccount!.Id, FromAccountBtcValue!,
                        ToAccountFiatValue!);
                case "FiatToBitcoin":
                    return new FiatToBitcoinDetails(FromAccount!.Id, ToAccount!.Id, FromAccountFiatValue!,
                        ToAccountBtcValue!);
                case "FiatToFiat":
                    return new FiatToFiatDetails(FromAccount!.Id, ToAccount!.Id, FromAccountFiatValue!,
                        AccountsAreSameTypeAndCurrency ? FromAccountFiatValue! : ToAccountFiatValue!);
            }
        }

        return CurrentAccountMode switch
        {
            "Bitcoin" => new BitcoinDetails(FromAccount!.Id, FromAccountBtcValue!,
                SelectedMode == TransactionTypes.Credit),
            "Fiat" => new FiatDetails(FromAccount!.Id, FromAccountFiatValue!, SelectedMode == TransactionTypes.Credit),
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