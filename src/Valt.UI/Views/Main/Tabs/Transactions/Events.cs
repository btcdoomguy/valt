using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public record TransactionListChanged();
public record FilterDateRangeChanged();

public record AccountSelectedChanged(AccountViewModel? Value);
public record FixedExpenseChanged(FixedExpensesEntryViewModel? Value);