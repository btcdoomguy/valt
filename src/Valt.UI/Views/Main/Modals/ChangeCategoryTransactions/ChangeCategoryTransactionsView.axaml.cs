using System;
using Avalonia.Threading;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.ChangeCategoryTransactions;

public partial class ChangeCategoryTransactionsView : ValtBaseWindow
{
    public ChangeCategoryTransactionsView()
    {
        InitializeComponent();
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        var viewModel = DataContext as ChangeCategoryTransactionsViewModel;

        NameBox.AsyncPopulator = viewModel!.GetSearchTermsAsync;
        
        Dispatcher.UIThread.InvokeAsync(() => NameBox.Focus(), DispatcherPriority.ApplicationIdle);
    }
}