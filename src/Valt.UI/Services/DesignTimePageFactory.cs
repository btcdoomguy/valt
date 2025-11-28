using System;
using Valt.UI.Base;
using Valt.UI.Views;
using Valt.UI.Views.Main.Tabs.Reports;
using TransactionsViewModel = Valt.UI.Views.Main.Tabs.Transactions.TransactionsViewModel;

namespace Valt.UI.Services;

public class DesignTimePageFactory : IPageFactory
{
    public ValtTabViewModel Create(MainViewTabNames pageName)
    {
        return pageName switch
        {
            MainViewTabNames.TransactionsPageContent => new TransactionsViewModel(),
            MainViewTabNames.ReportsPageContent => new ReportsViewModel(),
            _ => throw new ArgumentOutOfRangeException(nameof(pageName), pageName, null)
        };
    }
}