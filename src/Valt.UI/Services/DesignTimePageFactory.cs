using System;
using Valt.UI.Base;
using Valt.UI.Views;
using TransactionsViewModel = Valt.UI.Views.Main.Tabs.Transactions.TransactionsViewModel;

namespace Valt.UI.Services;

public class DesignTimePageFactory : IPageFactory
{
    public ValtViewModel Create(MainViewTabNames pageName)
    {
        return pageName switch
        {
            MainViewTabNames.TransactionsPageContent => new TransactionsViewModel(),
            _ => throw new ArgumentOutOfRangeException(nameof(pageName), pageName, null)
        };
    }
}