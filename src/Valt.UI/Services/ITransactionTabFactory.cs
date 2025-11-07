using Valt.UI.Base;
using Valt.UI.Views.Main.Tabs.Transactions;

namespace Valt.UI.Services;

public interface ITransactionTabFactory
{
    ValtViewModel Create(TransactionsTabNames tabName);
}