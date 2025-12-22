using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.TransactionEditor;

public partial class TransactionEditorView : ValtBaseWindow
{
    public TransactionEditorView()
    {
        InitializeComponent();
        
        this.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var viewModel = DataContext as TransactionEditorViewModel;

        InputElement controlToFocus = AutoCompleteTransactionNameBox;
        
        AutoCompleteTransactionNameBox.AsyncPopulator = viewModel!.GetTransactionTermsAsync;

        if (viewModel.TransactionFixedExpenseReference is not null)
        {
            if (viewModel.FromAccountIsBtc)
                controlToFocus = FromBtc;
            else
                controlToFocus = FromFiat;
        }
        

        Dispatcher.UIThread.InvokeAsync(() => controlToFocus.Focus(), DispatcherPriority.ApplicationIdle);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not TransactionEditorViewModel viewModel) return;
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.D1:
                    viewModel.SwitchToDebtCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D2:
                    viewModel.SwitchToCreditCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D3:
                    viewModel.SwitchToTransferCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Left:
                    viewModel.PreviousDayCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Right:
                    viewModel.NextDayCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.R:
                    viewModel.SelectTodayCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    viewModel.ProcessEnterCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
        else if (e.Key == Key.Enter)
        {
            viewModel.ProcessEnterCommand.Execute(null);
        }
    }

    private void AutoCompleteTransactionNameBox_OnDropDownClosed(object? sender, EventArgs e)
    {
        var viewModel = DataContext as TransactionEditorViewModel;

        if (viewModel?.TransactionTermResult is null) return;

        if (viewModel.FromAccount is null) return;

        Action focusAction;
        if (viewModel.FromAccountIsBtc)
            focusAction = () =>
            {
                FromBtc.Focus();
            };
        else
            focusAction = () =>
            {
                FromFiat.Focus();
            };
        //call post instead of Invoke to put it on a queue and process after the UI thread is done
        Dispatcher.UIThread.Post(focusAction, DispatcherPriority.ApplicationIdle);
    }
}