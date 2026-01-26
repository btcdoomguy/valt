using System.Drawing;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
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

        // Group IDs
        var bankAccountsGroupId = IdGenerator.Generate();
        var bitcoinGroupId = IdGenerator.Generate();

        // Create accounts
        var btcAccount = new AccountViewModel(
            new AccountSummaryDTO(
                Id: IdGenerator.Generate(),
                Type: "BtcAccount",
                Name: "Cold Storage",
                Visible: true,
                IconId: new Icon("MaterialDesign", "wallet-outline", '\uF19F', Color.FromArgb(255, 247, 147, 26))
                    .ToString(),
                Unicode: '\uF19F',
                Color: Color.FromArgb(255, 247, 147, 26),
                Currency: null,
                CurrencyDisplayName: "BTC",
                IsBtcAccount: true,
                FiatTotal: null,
                SatsTotal: 50000000,
                HasFutureTotal: true,
                FutureFiatTotal: null,
                FutureSatsTotal: 52000000,
                GroupId: bitcoinGroupId,
                GroupName: "Bitcoin"));

        var lightningAccount = new AccountViewModel(
            new AccountSummaryDTO(
                Id: IdGenerator.Generate(),
                Type: "BtcAccount",
                Name: "Lightning Wallet",
                Visible: true,
                IconId: new Icon("MaterialDesign", "flash", '\uEA0B', Color.FromArgb(255, 255, 215, 0))
                    .ToString(),
                Unicode: '\uEA0B',
                Color: Color.FromArgb(255, 255, 215, 0),
                Currency: null,
                CurrencyDisplayName: "BTC",
                IsBtcAccount: true,
                FiatTotal: null,
                SatsTotal: 1500000,
                HasFutureTotal: false,
                FutureFiatTotal: null,
                FutureSatsTotal: null,
                GroupId: bitcoinGroupId,
                GroupName: "Bitcoin"));

        var nubankAccount = new AccountViewModel(
            new AccountSummaryDTO(
                Id: IdGenerator.Generate(),
                Type: "FiatAccount",
                Name: "Nubank",
                Visible: true,
                IconId: new Icon("MaterialDesign", "bank", '\uE905', Color.FromArgb(255, 130, 10, 209))
                    .ToString(),
                Unicode: '\uE905',
                Color: Color.FromArgb(255, 130, 10, 209),
                Currency: "BRL",
                CurrencyDisplayName: "BRL",
                IsBtcAccount: false,
                FiatTotal: 5250.75m,
                SatsTotal: null,
                HasFutureTotal: true,
                FutureFiatTotal: 4500.00m,
                FutureSatsTotal: null,
                GroupId: bankAccountsGroupId,
                GroupName: "Bank Accounts"));

        var itauAccount = new AccountViewModel(
            new AccountSummaryDTO(
                Id: IdGenerator.Generate(),
                Type: "FiatAccount",
                Name: "Itaú",
                Visible: true,
                IconId: new Icon("MaterialDesign", "bank", '\uE905', Color.FromArgb(255, 0, 51, 160))
                    .ToString(),
                Unicode: '\uE905',
                Color: Color.FromArgb(255, 0, 51, 160),
                Currency: "BRL",
                CurrencyDisplayName: "BRL",
                IsBtcAccount: false,
                FiatTotal: 12500.00m,
                SatsTotal: null,
                HasFutureTotal: false,
                FutureFiatTotal: null,
                FutureSatsTotal: null,
                GroupId: bankAccountsGroupId,
                GroupName: "Bank Accounts"));

        var cashAccount = new AccountViewModel(
            new AccountSummaryDTO(
                Id: IdGenerator.Generate(),
                Type: "FiatAccount",
                Name: "Cash",
                Visible: true,
                IconId: new Icon("MaterialDesign", "cash", '\uEA0C', Color.FromArgb(255, 76, 175, 80))
                    .ToString(),
                Unicode: '\uEA0C',
                Color: Color.FromArgb(255, 76, 175, 80),
                Currency: "USD",
                CurrencyDisplayName: "USD",
                IsBtcAccount: false,
                FiatTotal: 350.00m,
                SatsTotal: null,
                HasFutureTotal: false,
                FutureFiatTotal: null,
                FutureSatsTotal: null,
                GroupId: null,
                GroupName: null));

        // Populate Accounts list (flat list for compatibility)
        Accounts = [btcAccount, lightningAccount, nubankAccount, itauAccount, cashAccount];

        // Populate AccountListItems with groups and accounts
        AccountListItems = new AvaloniaList<IAccountListItem>
        {
            // Bitcoin group
            new AccountGroupHeaderViewModel(bitcoinGroupId, "Bitcoin"),
            btcAccount,
            lightningAccount,

            // Bank Accounts group
            new AccountGroupHeaderViewModel(bankAccountsGroupId, "Bank Accounts"),
            nubankAccount,
            itauAccount,

            // Ungrouped accounts
            cashAccount
        };

        SelectedAccount = Accounts.FirstOrDefault();

        #endregion
    }
}