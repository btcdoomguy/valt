using Valt.App.Kernel;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;

namespace Valt.App.Modules.Budget.Transactions.Commands;

internal static class TransactionCommandHelper
{
    public static async Task<Result<TResult>?> ValidateAccountsAsync<TResult>(
        IAccountRepository accountRepository,
        TransactionDetailsDto details)
    {
        var fromAccountId = new AccountId(details.FromAccountId);

        string? toAccountIdString = details switch
        {
            FiatToFiatTransferDto f => f.ToAccountId,
            BitcoinToBitcoinTransferDto b => b.ToAccountId,
            FiatToBitcoinTransferDto fb => fb.ToAccountId,
            BitcoinToFiatTransferDto bf => bf.ToAccountId,
            _ => null
        };

        var fromAccountTask = accountRepository.GetAccountByIdAsync(fromAccountId);
        Task<Account?>? toAccountTask = toAccountIdString is not null
            ? accountRepository.GetAccountByIdAsync(new AccountId(toAccountIdString))
            : null;

        var tasks = new List<Task> { fromAccountTask };
        if (toAccountTask is not null)
            tasks.Add(toAccountTask);

        await Task.WhenAll(tasks);

        if (await fromAccountTask is null)
        {
            return Result<TResult>.NotFound("Account", details.FromAccountId);
        }

        if (toAccountIdString is not null && toAccountTask is not null && await toAccountTask is null)
        {
            return Result<TResult>.NotFound("Account", toAccountIdString);
        }

        return null;
    }

    public static TransactionDetails BuildTransactionDetails(TransactionDetailsDto dto)
    {
        return dto switch
        {
            FiatTransactionDto fiat => new FiatDetails(
                new AccountId(fiat.FromAccountId),
                FiatValue.New(fiat.Amount),
                fiat.IsCredit),

            BitcoinTransactionDto btc => new BitcoinDetails(
                new AccountId(btc.FromAccountId),
                (BtcValue)btc.AmountSats,
                btc.IsCredit),

            FiatToFiatTransferDto fiatToFiat => new FiatToFiatDetails(
                new AccountId(fiatToFiat.FromAccountId),
                new AccountId(fiatToFiat.ToAccountId),
                FiatValue.New(fiatToFiat.FromAmount),
                FiatValue.New(fiatToFiat.ToAmount)),

            BitcoinToBitcoinTransferDto btcToBtc => new BitcoinToBitcoinDetails(
                new AccountId(btcToBtc.FromAccountId),
                new AccountId(btcToBtc.ToAccountId),
                (BtcValue)btcToBtc.AmountSats),

            FiatToBitcoinTransferDto fiatToBtc => new FiatToBitcoinDetails(
                new AccountId(fiatToBtc.FromAccountId),
                new AccountId(fiatToBtc.ToAccountId),
                FiatValue.New(fiatToBtc.FromFiatAmount),
                (BtcValue)fiatToBtc.ToSatsAmount),

            BitcoinToFiatTransferDto btcToFiat => new BitcoinToFiatDetails(
                new AccountId(btcToFiat.FromAccountId),
                new AccountId(btcToFiat.ToAccountId),
                (BtcValue)btcToFiat.FromSatsAmount,
                FiatValue.New(btcToFiat.ToFiatAmount)),

            _ => throw new ArgumentException($"Unknown transaction details type: {dto.GetType().Name}")
        };
    }
}
