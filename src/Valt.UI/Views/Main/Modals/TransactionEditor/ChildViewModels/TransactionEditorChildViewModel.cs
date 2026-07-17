using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Input;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.TransactionEditor.ChildViewModels;

public abstract partial class TransactionEditorChildViewModel : ValtValidatorViewModel
{
    public AvaloniaList<AccountDTO> AvailableAccounts { get; set; } = [];
    public AvaloniaList<CategoryDTO> AvailableCategories { get; set; } = [];

    [Required(ErrorMessage = "Origin account is required")]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FromAccountType), nameof(FromAccountIsBtc))]
    private AccountDTO? _fromAccount;

    [CustomValidation(typeof(TransactionEditorChildViewModel), nameof(ValidateFromAccountBtcValue))]
    [ObservableProperty]
    private BtcValue? _fromAccountBtcValue = BtcValue.Empty;

    [CustomValidation(typeof(TransactionEditorChildViewModel), nameof(ValidateFromAccountFiatValue))]
    [ObservableProperty]
    private FiatValue? _fromAccountFiatValue = FiatValue.Empty;

    [ObservableProperty] private bool _isFromBtcInputFocused;
    [ObservableProperty] private bool _isFromFiatInputFocused;
    [ObservableProperty] private bool _isToBtcInputFocused;
    [ObservableProperty] private bool _isToFiatInputFocused;

    [ObservableProperty] private bool _fromBtcIsBitcoinMode;
    [ObservableProperty] private bool _toBtcIsBitcoinMode;

    public ICommand? OpenCalculatorCommand { get; set; }

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

    public bool IsFromAmountFocused => IsFromBtcInputFocused || IsFromFiatInputFocused;

    public virtual bool IsTransferValueFieldVisible => false;

    public bool ShouldProcessEnter => IsFromBtcInputFocused ||
                                       (IsFromFiatInputFocused && !IsTransferValueFieldVisible) ||
                                       IsToBtcInputFocused ||
                                       IsToFiatInputFocused;

    public new void ValidateAllProperties() => base.ValidateAllProperties();

    public static ValidationResult ValidateFromAccountBtcValue(BtcValue? btcValue, ValidationContext context)
    {
        var instance = (TransactionEditorChildViewModel)context.ObjectInstance;

        var shouldValidate = instance.FromAccountType == AccountTypes.Bitcoin;

        if (shouldValidate && (btcValue is null || btcValue.Btc == 0))
        {
            return new ValidationResult("Value is required");
        }

        return ValidationResult.Success!;
    }

    public static ValidationResult ValidateFromAccountFiatValue(FiatValue? fiatValue, ValidationContext context)
    {
        var instance = (TransactionEditorChildViewModel)context.ObjectInstance;

        var shouldValidate = instance.FromAccountType == AccountTypes.Fiat;

        if (shouldValidate && (fiatValue is null || fiatValue.Value == 0))
        {
            return new ValidationResult("Value is required");
        }

        return ValidationResult.Success!;
    }

    protected TransactionEditorChildViewModel()
    {
    }
}
