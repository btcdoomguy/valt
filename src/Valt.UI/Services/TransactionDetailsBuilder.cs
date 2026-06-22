using System;
using System.Collections.Generic;
using System.Linq;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

namespace Valt.UI.Services;

public class TransactionDetailsBuilder : ITransactionDetailsBuilder
{
    public TransactionDetailsDto BuildDto(TransactionFormSnapshot snapshot)
    {
        var fromAccountType = ParseAccountType(snapshot.FromAccount.Type);
        var toAccountType = snapshot.ToAccount is not null
            ? ParseAccountType(snapshot.ToAccount.Type)
            : (AccountTypes?)null;

        if (snapshot.SelectedMode == TransactionTypes.Transfer)
        {
            if (snapshot.ToAccount is null)
                throw new TransactionDetailsBuildException();

            return (fromAccountType, toAccountType) switch
            {
                (AccountTypes.Bitcoin, AccountTypes.Bitcoin) => new BitcoinToBitcoinTransferDto
                {
                    FromAccountId = snapshot.FromAccount.Id,
                    ToAccountId = snapshot.ToAccount.Id,
                    AmountSats = snapshot.FromAccountBtcValue!.Sats
                },
                (AccountTypes.Bitcoin, AccountTypes.Fiat) => new BitcoinToFiatTransferDto
                {
                    FromAccountId = snapshot.FromAccount.Id,
                    ToAccountId = snapshot.ToAccount.Id,
                    FromSatsAmount = snapshot.FromAccountBtcValue!.Sats,
                    ToFiatAmount = snapshot.ToAccountFiatValue!.Value
                },
                (AccountTypes.Fiat, AccountTypes.Bitcoin) => new FiatToBitcoinTransferDto
                {
                    FromAccountId = snapshot.FromAccount.Id,
                    ToAccountId = snapshot.ToAccount.Id,
                    FromFiatAmount = snapshot.FromAccountFiatValue!.Value,
                    ToSatsAmount = snapshot.ToAccountBtcValue!.Sats
                },
                (AccountTypes.Fiat, AccountTypes.Fiat) => new FiatToFiatTransferDto
                {
                    FromAccountId = snapshot.FromAccount.Id,
                    ToAccountId = snapshot.ToAccount.Id,
                    FromAmount = snapshot.FromAccountFiatValue!.Value,
                    ToAmount = snapshot.AccountsAreSameTypeAndCurrency
                        ? snapshot.FromAccountFiatValue.Value
                        : snapshot.ToAccountFiatValue!.Value
                },
                _ => throw new TransactionDetailsBuildException()
            };
        }

        return fromAccountType switch
        {
            AccountTypes.Bitcoin => new BitcoinTransactionDto
            {
                FromAccountId = snapshot.FromAccount.Id,
                AmountSats = snapshot.FromAccountBtcValue!.Sats,
                IsCredit = snapshot.SelectedMode == TransactionTypes.Credit
            },
            AccountTypes.Fiat => new FiatTransactionDto
            {
                FromAccountId = snapshot.FromAccount.Id,
                Amount = snapshot.FromAccountFiatValue!.Value,
                IsCredit = snapshot.SelectedMode == TransactionTypes.Credit
            },
            _ => throw new TransactionDetailsBuildException()
        };
    }

    public TransactionFormValues LoadFromDto(
        TransactionDetailsDto dto,
        IReadOnlyList<AccountDTO> availableAccounts)
    {
        switch (dto)
        {
            case FiatTransactionDto fiat:
                return new TransactionFormValues(
                    SelectedMode: fiat.IsCredit ? TransactionTypes.Credit : TransactionTypes.Debt,
                    ToAccount: null,
                    FromAccountBtcValue: null,
                    FromAccountFiatValue: FiatValue.New(fiat.Amount),
                    ToAccountBtcValue: null,
                    ToAccountFiatValue: null);

            case BitcoinTransactionDto btc:
                return new TransactionFormValues(
                    SelectedMode: btc.IsCredit ? TransactionTypes.Credit : TransactionTypes.Debt,
                    ToAccount: null,
                    FromAccountBtcValue: BtcValue.New(btc.AmountSats),
                    FromAccountFiatValue: null,
                    ToAccountBtcValue: null,
                    ToAccountFiatValue: null);

            case FiatToFiatTransferDto fiatToFiat:
                var fromFiatAccount = availableAccounts.FirstOrDefault(a => a.Id == fiatToFiat.FromAccountId);
                var toFiatAccount = availableAccounts.FirstOrDefault(a => a.Id == fiatToFiat.ToAccountId);
                var sameTypeAndCurrency = AccountsShareTypeAndCurrency(fromFiatAccount, toFiatAccount);

                return new TransactionFormValues(
                    SelectedMode: TransactionTypes.Transfer,
                    ToAccount: toFiatAccount,
                    FromAccountBtcValue: null,
                    FromAccountFiatValue: FiatValue.New(fiatToFiat.FromAmount),
                    ToAccountBtcValue: null,
                    ToAccountFiatValue: sameTypeAndCurrency
                        ? FiatValue.New(fiatToFiat.FromAmount)
                        : FiatValue.New(fiatToFiat.ToAmount));

            case BitcoinToBitcoinTransferDto btcToBtc:
                return new TransactionFormValues(
                    SelectedMode: TransactionTypes.Transfer,
                    ToAccount: availableAccounts.FirstOrDefault(a => a.Id == btcToBtc.ToAccountId),
                    FromAccountBtcValue: BtcValue.New(btcToBtc.AmountSats),
                    FromAccountFiatValue: null,
                    ToAccountBtcValue: BtcValue.New(btcToBtc.AmountSats),
                    ToAccountFiatValue: null);

            case FiatToBitcoinTransferDto fiatToBtc:
                return new TransactionFormValues(
                    SelectedMode: TransactionTypes.Transfer,
                    ToAccount: availableAccounts.FirstOrDefault(a => a.Id == fiatToBtc.ToAccountId),
                    FromAccountBtcValue: null,
                    FromAccountFiatValue: FiatValue.New(fiatToBtc.FromFiatAmount),
                    ToAccountBtcValue: BtcValue.New(fiatToBtc.ToSatsAmount),
                    ToAccountFiatValue: null);

            case BitcoinToFiatTransferDto btcToFiat:
                return new TransactionFormValues(
                    SelectedMode: TransactionTypes.Transfer,
                    ToAccount: availableAccounts.FirstOrDefault(a => a.Id == btcToFiat.ToAccountId),
                    FromAccountBtcValue: BtcValue.New(btcToFiat.FromSatsAmount),
                    FromAccountFiatValue: null,
                    ToAccountBtcValue: null,
                    ToAccountFiatValue: FiatValue.New(btcToFiat.ToFiatAmount));

            default:
                throw new TransactionDetailsBuildException();
        }
    }

    private static AccountTypes ParseAccountType(string type) =>
        Enum.Parse<AccountTypes>(type);

    private static bool AccountsShareTypeAndCurrency(AccountDTO? fromAccount, AccountDTO? toAccount)
    {
        if (fromAccount is null || toAccount is null)
            return false;

        if (fromAccount.Type != toAccount.Type)
            return false;

        if (fromAccount.IsBtcAccount && toAccount.IsBtcAccount)
            return true;

        return fromAccount.Currency == toAccount.Currency;
    }
}
