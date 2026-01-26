using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;

namespace Valt.App.Modules.Budget.Transactions.Queries.GetTransactionById;

internal sealed class GetTransactionByIdHandler : IQueryHandler<GetTransactionByIdQuery, TransactionForEditDTO?>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionByIdHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionForEditDTO?> HandleAsync(GetTransactionByIdQuery query, CancellationToken ct = default)
    {
        var transaction = await _transactionRepository.GetTransactionByIdAsync(new TransactionId(query.TransactionId));

        if (transaction is null)
            return null;

        return MapToDto(transaction);
    }

    private static TransactionForEditDTO MapToDto(Transaction transaction)
    {
        return new TransactionForEditDTO
        {
            Id = transaction.Id.Value,
            Date = transaction.Date,
            Name = transaction.Name.Value,
            CategoryId = transaction.CategoryId.Value,
            Notes = transaction.Notes,
            GroupId = transaction.GroupId?.Value,
            FixedExpenseReference = transaction.FixedExpenseReference is not null
                ? new FixedExpenseReferenceDTO
                {
                    FixedExpenseId = transaction.FixedExpenseReference.FixedExpenseId.Value,
                    ReferenceDate = transaction.FixedExpenseReference.ReferenceDate
                }
                : null,
            Details = MapDetailsToDto(transaction.TransactionDetails),
            AutoSatAmountDetails = transaction.HasAutoSatAmount
                ? new AutoSatAmountDTO
                {
                    IsAutoSatAmount = transaction.AutoSatAmountDetails!.IsAutoSatAmount,
                    SatAmountState = transaction.AutoSatAmountDetails.SatAmountState.ToString(),
                    SatAmountSats = transaction.AutoSatAmountDetails.SatAmount?.Sats
                }
                : null
        };
    }

    private static TransactionDetailsDto MapDetailsToDto(TransactionDetails details)
    {
        return details switch
        {
            FiatDetails fiat => new FiatTransactionDto
            {
                FromAccountId = fiat.FromAccountId.Value,
                Amount = fiat.Amount.Value,
                IsCredit = fiat.Credit
            },
            BitcoinDetails btc => new BitcoinTransactionDto
            {
                FromAccountId = btc.FromAccountId.Value,
                AmountSats = btc.Amount.Sats,
                IsCredit = btc.Credit
            },
            FiatToFiatDetails fiatToFiat => new FiatToFiatTransferDto
            {
                FromAccountId = fiatToFiat.FromAccountId.Value,
                ToAccountId = fiatToFiat.ToAccountId!.Value,
                FromAmount = fiatToFiat.FromAmount.Value,
                ToAmount = fiatToFiat.ToAmount.Value
            },
            BitcoinToBitcoinDetails btcToBtc => new BitcoinToBitcoinTransferDto
            {
                FromAccountId = btcToBtc.FromAccountId.Value,
                ToAccountId = btcToBtc.ToAccountId!.Value,
                AmountSats = btcToBtc.Amount.Sats
            },
            FiatToBitcoinDetails fiatToBtc => new FiatToBitcoinTransferDto
            {
                FromAccountId = fiatToBtc.FromAccountId.Value,
                ToAccountId = fiatToBtc.ToAccountId!.Value,
                FromFiatAmount = fiatToBtc.FromAmount.Value,
                ToSatsAmount = fiatToBtc.ToAmount.Sats
            },
            BitcoinToFiatDetails btcToFiat => new BitcoinToFiatTransferDto
            {
                FromAccountId = btcToFiat.FromAccountId.Value,
                ToAccountId = btcToFiat.ToAccountId!.Value,
                FromSatsAmount = btcToFiat.FromAmount.Sats,
                ToFiatAmount = btcToFiat.ToAmount.Value
            },
            _ => throw new InvalidOperationException($"Unknown transaction details type: {details.GetType().Name}")
        };
    }
}
