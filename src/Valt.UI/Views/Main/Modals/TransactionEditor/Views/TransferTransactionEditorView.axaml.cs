using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Modals.TransactionEditor.ChildViewModels;

namespace Valt.UI.Views.Main.Modals.TransactionEditor.Views;

public partial class TransferTransactionEditorView : UserControl, ITransactionEditorChildView
{
    private AutoCompleteBox? _autoCompleteTransactionNameBox;
    private BtcInput? _fromBtc;
    private FiatInput? _fromFiat;

    public TransferTransactionEditorView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _autoCompleteTransactionNameBox = this.FindControl<AutoCompleteBox>("AutoCompleteTransactionNameBox");
        _fromBtc = this.FindControl<BtcInput>("FromBtc");
        _fromFiat = this.FindControl<FiatInput>("FromFiat");

        if (_autoCompleteTransactionNameBox is null) return;

        var parentView = this.FindAncestorOfType<TransactionEditorView>();
        if (parentView?.DataContext is TransactionEditorViewModel parentVm)
        {
            _autoCompleteTransactionNameBox.AsyncPopulator = parentVm.GetTransactionTermsAsync;
        }

        _autoCompleteTransactionNameBox.DropDownClosed += AutoCompleteTransactionNameBox_OnDropDownClosed;
    }

    public void FocusNameInput()
    {
        _autoCompleteTransactionNameBox?.Focus();
    }

    public void FocusAmountInput()
    {
        if (DataContext is TransferTransactionEditorViewModel { FromAccountIsBtc: true })
        {
            _fromBtc?.Focus();
        }
        else
        {
            _fromFiat?.Focus();
        }
    }

    private void AutoCompleteTransactionNameBox_OnDropDownClosed(object? sender, EventArgs e)
    {
        var parentView = this.FindAncestorOfType<TransactionEditorView>();
        if (parentView?.DataContext is not TransactionEditorViewModel parentVm) return;
        if (parentVm.TransactionTermResult is null) return;
        if (DataContext is not TransferTransactionEditorViewModel { FromAccount: not null }) return;

        Action focusAction = FocusAmountInput;
        Dispatcher.UIThread.Post(focusAction, DispatcherPriority.ApplicationIdle);
    }
}
