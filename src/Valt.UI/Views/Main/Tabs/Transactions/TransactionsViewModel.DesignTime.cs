using System;
using System.Drawing;
using System.Linq;
using Avalonia.Controls;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionsViewModel
{
    public TransactionsViewModel()
    {
        if (!Design.IsDesignMode) return;

        _transactionListViewModel = new TransactionListViewModel();

        SubContent = _transactionListViewModel;

        #region Design-time data

        Accounts =
        [
            new AccountViewModel(
                new AccountSummaryDTO(
                    Id: IdGenerator.Generate(),
                    Type: "BtcAccount",
                    Name: "BTC Account",
                    Visible: true,
                    Icon: new Icon("MaterialDesign", "account-box-line", '\uEA07', Color.FromArgb(255, 255, 0, 0))
                        .ToString(),
                    Unicode: '\uEA07',
                    Color: Color.FromArgb(255, 255, 0, 0),
                    Currency: null,
                    IsBtcAccount: true,
                    FiatTotal: null,
                    SatsTotal: 100000,
                    HasFutureTotal: true,
                    FutureFiatTotal: null,
                    FutureSatsTotal: 120000)),
            new AccountViewModel(new AccountSummaryDTO(Id: IdGenerator.Generate(),
                Type: "FiatAccount",
                Name: "Nubank",
                Visible: true,
                Unicode: '\uEA07',
                Color: Color.FromArgb(255, 255, 0, 255),
                Icon: new Icon("MaterialDesign", "account-box-line", '\uEA07', Color.FromArgb(255, 255, 0, 255))
                    .ToString(),
                Currency: "BRL", IsBtcAccount: false, FiatTotal: 1000, SatsTotal: null,
                HasFutureTotal: false,
                FutureFiatTotal: 1000, FutureSatsTotal: null)),
            new AccountViewModel(new AccountSummaryDTO(Id: IdGenerator.Generate(),
                Type: "FiatAccount",
                Name: "Itaú",
                Visible: true,
                Unicode: '\uEA07',
                Color: Color.FromArgb(255, 255, 255, 0),
                Icon: new Icon("MaterialDesign", "account-box-line", '\uEA07', Color.FromArgb(255, 255, 255, 0))
                    .ToString(),
                Currency: "BRL",
                IsBtcAccount: false,
                FiatTotal: 1000, SatsTotal: null,
                HasFutureTotal: false,
                FutureFiatTotal: 1000, FutureSatsTotal: null))
        ];

        SelectedAccount = Accounts.FirstOrDefault();

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

        #endregion
    }
}