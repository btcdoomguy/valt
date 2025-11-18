using System;
using Valt.UI.Base;
using Valt.UI.Views.Main.Tabs.Transactions;

namespace Valt.UI.Services;

public class TransactionTabFactory : ITransactionTabFactory
{
    private readonly Func<TransactionsTabNames, ValtViewModel> _factoryMethod;

    public TransactionTabFactory(Func<TransactionsTabNames, ValtViewModel> factoryMethod)
    {
        _factoryMethod = factoryMethod;
    }

    public ValtViewModel Create(TransactionsTabNames pageName) => _factoryMethod.Invoke(pageName);
}