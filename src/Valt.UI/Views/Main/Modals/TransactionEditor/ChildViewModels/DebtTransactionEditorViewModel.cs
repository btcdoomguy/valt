using CommunityToolkit.Mvvm.ComponentModel;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.TransactionEditor.ChildViewModels;

public partial class DebtTransactionEditorViewModel : TransactionEditorChildViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInstallmentCount), nameof(InstallmentValueText))]
    private bool _useInstallments;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstallmentValueText))]
    private int _installmentCount = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInstallmentsOption))]
    private bool _isEditing;

    public bool ShowInstallmentsOption => !IsEditing;

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

    public DebtTransactionEditorViewModel()
    {
    }
}
