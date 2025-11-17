using System;
using Avalonia.Threading;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.FixedExpenseEditor;

public partial class FixedExpenseEditorView : ValtBaseWindow
{
    public FixedExpenseEditorView()
    {
        InitializeComponent();
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var viewModel = DataContext as FixedExpenseEditorViewModel;

        AutoCompleteTransactionNameBox.AsyncPopulator = viewModel!.GetTransactionTermsAsync;

        Dispatcher.UIThread.InvokeAsync(() => AutoCompleteTransactionNameBox.Focus(), DispatcherPriority.ApplicationIdle);
    }
}