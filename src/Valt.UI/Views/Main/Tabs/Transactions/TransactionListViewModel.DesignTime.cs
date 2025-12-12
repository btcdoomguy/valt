using System;
using System.Drawing;
using Avalonia.Collections;
using Avalonia.Controls;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionListViewModel
{
    public TransactionListViewModel()
    {
        if (!Design.IsDesignMode)
            return;
        
        Transactions = new AvaloniaList<TransactionViewModel>()
        {
            new(id: IdGenerator.Generate(),
                date: new DateOnly(2025, 2, 12),
                name: "Test Credit",
                categoryId: IdGenerator.Generate(),
                categoryName: "Test",
                categoryIcon: new Icon("MaterialDesign", "account-box-line", '\uEA07', Color.FromArgb(255, 0, 255, 0)),
                fromAccountId: IdGenerator.Generate(),
                fromAccountName: "My Account",
                fromAccountIcon: new Icon("MaterialDesign", "account-box-line", '\uEA07',
                    Color.FromArgb(255, 0, 255, 0)),
                toAccountId: null,
                toAccountName: null,
                toAccountIcon: null,
                formattedFromAmount: "R$ 123.00",
                fromAmountFiat: 123,
                fromAmountSats: null,
                formattedToAmount: null,
                toAmountFiat: null,
                toAmountSats: null,
                fromCurrency: "BRL",
                toCurrency: null,
                transferType: TransactionTransferTypes.Fiat,
                transactionType: TransactionTypes.Credit,
                autoSatAmount: 688012,
                fixedExpenseRecordId: null,
                fixedExpenseId: null,
                fixedExpenseName: null,
                fixedExpenseReferenceDate: null,
                futureTransaction: true),
            new(id: IdGenerator.Generate(),
                date: new DateOnly(2025, 2, 13),
                name: "Test Debt",
                categoryId: IdGenerator.Generate(),
                categoryName: "Test",
                categoryIcon: new Icon("MaterialDesign", "account-box-line", '\uEA07', Color.FromArgb(255, 0, 255, 0)),
                fromAccountId: IdGenerator.Generate(),
                fromAccountName: "My Account",
                fromAccountIcon: new Icon("MaterialDesign", "account-box-line", '\uEA07',
                    Color.FromArgb(255, 0, 255, 0)),
                toAccountId: null,
                toAccountName: null,
                toAccountIcon: null,
                formattedFromAmount: "R$ -123.00",
                fromAmountFiat: -123,
                fromAmountSats: null,
                formattedToAmount: null,
                toAmountFiat: null,
                toAmountSats: null,
                fromCurrency: "BRL",
                toCurrency: null,
                transferType: TransactionTransferTypes.Fiat,
                transactionType: TransactionTypes.Debt,
                autoSatAmount: 688012,
                fixedExpenseRecordId: null,
                fixedExpenseId: null,
                fixedExpenseName: null,
                fixedExpenseReferenceDate: null,
                futureTransaction: false),
            new(id: IdGenerator.Generate(),
                date: new DateOnly(2025, 2, 12),
                name: "Test Transfer",
                categoryId: IdGenerator.Generate(),
                categoryName: "Test",
                categoryIcon: new Icon("MaterialDesign", "account-box-line", '\uEA07', Color.FromArgb(255, 0, 255, 0)),
                fromAccountId: IdGenerator.Generate(),
                fromAccountName: "Nubank",
                fromAccountIcon: new Icon("MaterialDesign", "account-box-line", '\uEA07',
                    Color.FromArgb(255, 0, 255, 0)),
                toAccountId: IdGenerator.Generate(),
                toAccountName: "Cold Wallet",
                toAccountIcon: new Icon("MaterialDesign", "account-box-line", '\uEA07', Color.FromArgb(255, 0, 255, 0)),
                formattedFromAmount: "R$ -1000.00",
                fromAmountFiat: -1000,
                fromAmountSats: null,
                formattedToAmount: "0.00010000",
                toAmountFiat: null,
                toAmountSats: 10000,
                fromCurrency: "BRL",
                toCurrency: "BTC",
                transferType: TransactionTransferTypes.FiatToBitcoin,
                transactionType: TransactionTypes.Transfer,
                autoSatAmount: 688012,
                fixedExpenseRecordId: null,
                fixedExpenseId: null,
                fixedExpenseName: null,
                fixedExpenseReferenceDate: null,
                futureTransaction: false),
        };

        _filterState = new FilterState
        {
            MainDate = DateTime.Now
        };
    }
}