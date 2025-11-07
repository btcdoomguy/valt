using System;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    
    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CustomTitleBar_OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}