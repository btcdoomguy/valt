using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.Base;
using Valt.Infra.Settings;

namespace Valt.UI.Views.Main.Modals.TransactionEditor.ChildViewModels;

public partial class TransferTransactionEditorViewModel : TransactionEditorChildViewModel, IValidatableObject
{
    private const string AccountModeFiat = "Fiat";
    private const string AccountModeBitcoin = "Bitcoin";
    private const string AccountModeFiatToFiat = "FiatToFiat";
    private const string AccountModeFiatToBitcoin = "FiatToBitcoin";
    private const string AccountModeBitcoinToBitcoin = "BitcoinToBitcoin";
    private const string AccountModeBitcoinToFiat = "BitcoinToFiat";

    private readonly CurrencySettings _currencySettings;
    private readonly IFireAndForgetTaskRunner _runner;
    private readonly ILogger<TransferTransactionEditorViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToAccountType), nameof(ToAccountIsBtc), nameof(ShowTransferValueField), nameof(AccountsAreSameTypeAndCurrency), nameof(CurrentAccountMode))]
    [CustomValidation(typeof(TransferTransactionEditorViewModel), nameof(ValidateToAccount))]
    private AccountDTO? _toAccount;

    [CustomValidation(typeof(TransferTransactionEditorViewModel), nameof(ValidateToAccountBtcValue))]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTransferValueField), nameof(CurrentAccountMode))]
    private BtcValue? _toAccountBtcValue = BtcValue.Empty;

    [CustomValidation(typeof(TransferTransactionEditorViewModel), nameof(ValidateToAccountFiatValue))]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTransferValueField), nameof(CurrentAccountMode))]
    private FiatValue? _toAccountFiatValue = FiatValue.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTransferValueField))]
    private string _transferRate = string.Empty;

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

    public bool ToAccountIsBtc => ToAccountType == AccountTypes.Bitcoin;

    public bool AccountsAreSameTypeAndCurrency
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

    public bool ShowTransferValueField =>
        !AccountsAreSameTypeAndCurrency;

    public override bool IsTransferValueFieldVisible => ShowTransferValueField;

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

    public static ValidationResult ValidateToAccountBtcValue(BtcValue? btcValue, ValidationContext context)
    {
        var instance = (TransferTransactionEditorViewModel)context.ObjectInstance;

        var shouldValidate = instance is
        {
            FromAccountType: AccountTypes.Fiat,
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
        var instance = (TransferTransactionEditorViewModel)context.ObjectInstance;

        var shouldValidate = instance is {
                FromAccountType: AccountTypes.Fiat,
                ToAccountType: AccountTypes.Fiat,
                AccountsAreSameTypeAndCurrency: false
            } or
            {
                FromAccountType: AccountTypes.Bitcoin,
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
        if (account is null)
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

        return results;
    }

    public TransferTransactionEditorViewModel()
    {
        _currencySettings = null!;
        _runner = null!;
        _logger = null!;
    }

    public TransferTransactionEditorViewModel(
        CurrencySettings currencySettings,
        IFireAndForgetTaskRunner runner,
        ILogger<TransferTransactionEditorViewModel> logger)
    {
        _currencySettings = currencySettings;
        _runner = runner;
        _logger = logger;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(ToAccount) or nameof(ToAccountBtcValue)
            or nameof(ToAccountFiatValue) or nameof(FromAccount) or nameof(FromAccountBtcValue)
            or nameof(FromAccountFiatValue) or nameof(FromAccountType) or nameof(ToAccountType))
        {
            UpdateTransferRateAsync().FireAndForgetSafeAsync(_runner, _logger);
        }
    }

    public async Task UpdateTransferRateAsync()
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
}
