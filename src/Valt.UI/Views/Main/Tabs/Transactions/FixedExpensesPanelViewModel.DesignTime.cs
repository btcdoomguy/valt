using System;
using Avalonia.Controls;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class FixedExpensesPanelViewModel
{
    public FixedExpensesPanelViewModel()
    {
        if (!Design.IsDesignMode) return;

        var currentMockedDay = new DateOnly(2025, 1, 10);

        FixedExpenseEntries =
        [
            new FixedExpensesEntryViewModel(new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test1",
                new CategoryId(),
                new DateOnly(2025, 1, 7), null, 100, null, null, FiatCurrency.Usd.Code, FixedExpenseRecordState.Paid,
                new TransactionId()), currentMockedDay),
            new FixedExpensesEntryViewModel(new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test2",
                new CategoryId(),
                new DateOnly(2025, 1, 10), null, 120, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.ManuallyPaid, null), currentMockedDay),
            new FixedExpensesEntryViewModel(new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test3",
                new CategoryId(),
                new DateOnly(2025, 1, 15), null, 120, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Ignored, null), currentMockedDay),
            new FixedExpensesEntryViewModel(new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test4",
                new CategoryId(),
                new DateOnly(2025, 1, 28), null, 120, null, null, FiatCurrency.Usd.Code), currentMockedDay),
        ];

        RemainingFixedExpensesAmount = "~ R$ 1.234,56";
    }
}
