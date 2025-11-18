using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Kernel.Exceptions;
using Valt.Infra.Modules.Budget.FixedExpenses;

namespace Valt.Infra.Modules.Budget.Transactions;

public static class Extensions
{
    public static Transaction AsDomainObject(this TransactionEntity entity,
        FixedExpenseRecordEntity? fixedExpenseRecord = null)
    {
        try
        {
            return entity.Type switch
            {
                TransactionEntityType.Fiat => ConvertToFiatTransaction(entity, fixedExpenseRecord),
                TransactionEntityType.Bitcoin => ConvertToBtcTransaction(entity, fixedExpenseRecord),
                TransactionEntityType.BitcoinToBitcoin => ConvertToBtcToBtcTransfer(entity, fixedExpenseRecord),
                TransactionEntityType.FiatToFiat => ConvertToFiatToFiatTransfer(entity, fixedExpenseRecord),
                TransactionEntityType.FiatToBitcoin => ConvertToFiatToBtcTransfer(entity, fixedExpenseRecord),
                TransactionEntityType.BitcoinToFiat => ConvertToBtcToFiatTransfer(entity, fixedExpenseRecord),
                _ => throw new ArgumentException("Transaction type not found")
            };
        }
        catch (Exception ex)
        {
            throw new BrokenConversionFromDbException(nameof(TransactionEntity), entity.Id.ToString(), ex);
        }
    }

    private static Transaction ConvertToFiatTransaction(TransactionEntity entity,
        FixedExpenseRecordEntity? fixedExpenseRecord)
    {
        return Transaction.Create(entity.Id.ToString(),
            DateOnly.FromDateTime(entity.Date),
            entity.Name,
            entity.CategoryId.ToString(),
            new FiatDetails(entity.FromAccountId.ToString(),
                DatabaseValueParser.AdaptToFiatValue(entity.FromFiatAmount!.Value),
                entity.FromFiatAmount! > 0),
            new AutoSatAmountDetails(true,
                (SatAmountState)entity.SatAmountStateId!,
                entity.SatAmount.HasValue ? BtcValue.ParseSats(entity.SatAmount!.Value) : null),
            entity.Notes,
            fixedExpenseRecord is not null
                ? new TransactionFixedExpenseReference(
                    new FixedExpenseId(fixedExpenseRecord.FixedExpense.Id.ToString()),
                    DateOnly.FromDateTime(fixedExpenseRecord.ReferenceDate))
                : null,
            entity.Version);
    }

    private static Transaction ConvertToBtcTransaction(TransactionEntity entity,
        FixedExpenseRecordEntity? fixedExpenseRecord)
    {
        return Transaction.Create(entity.Id.ToString(),
            DateOnly.FromDateTime(entity.Date),
            entity.Name,
            entity.CategoryId.ToString(),
            new BitcoinDetails(
                entity.FromAccountId.ToString(),
                DatabaseValueParser.AdaptToBtcValue(entity.FromSatAmount!.Value),
                entity.FromSatAmount! > 0),
            null,
            entity.Notes,
            fixedExpenseRecord is not null
                ? new TransactionFixedExpenseReference(
                    new FixedExpenseId(fixedExpenseRecord.FixedExpense.Id.ToString()),
                    DateOnly.FromDateTime(fixedExpenseRecord.ReferenceDate))
                : null,
            entity.Version);
    }

    private static Transaction ConvertToBtcToBtcTransfer(TransactionEntity entity,
        FixedExpenseRecordEntity? fixedExpenseRecord)
    {
        return Transaction.Create(entity.Id.ToString(),
            DateOnly.FromDateTime(entity.Date),
            entity.Name,
            entity.CategoryId.ToString(),
            new BitcoinToBitcoinDetails(entity.FromAccountId.ToString(),
                entity.ToAccountId!.ToString(),
                DatabaseValueParser.AdaptToBtcValue(entity.FromSatAmount!.Value)),
            null,
            entity.Notes,
            fixedExpenseRecord is not null
                ? new TransactionFixedExpenseReference(
                    new FixedExpenseId(fixedExpenseRecord.FixedExpense.Id.ToString()),
                    DateOnly.FromDateTime(fixedExpenseRecord.ReferenceDate))
                : null,
            entity.Version);
    }

    private static Transaction ConvertToFiatToFiatTransfer(TransactionEntity entity,
        FixedExpenseRecordEntity? fixedExpenseRecord)
    {
        return Transaction.Create(entity.Id.ToString(),
            DateOnly.FromDateTime(entity.Date),
            entity.Name,
            entity.CategoryId.ToString(),
            new FiatToFiatDetails(
                entity.FromAccountId.ToString(),
                entity.ToAccountId!.ToString(),
                DatabaseValueParser.AdaptToFiatValue(entity.FromFiatAmount!.Value),
                DatabaseValueParser.AdaptToFiatValue(entity.ToFiatAmount!.Value)),
            new AutoSatAmountDetails(true,
                (SatAmountState)entity.SatAmountStateId!,
                entity.SatAmount.HasValue ? BtcValue.ParseSats(entity.SatAmount!.Value) : null),
            entity.Notes,
            fixedExpenseRecord is not null
                ? new TransactionFixedExpenseReference(
                    new FixedExpenseId(fixedExpenseRecord.FixedExpense.Id.ToString()),
                    DateOnly.FromDateTime(fixedExpenseRecord.ReferenceDate))
                : null,
            entity.Version);
    }

    private static Transaction ConvertToFiatToBtcTransfer(TransactionEntity entity,
        FixedExpenseRecordEntity? fixedExpenseRecord)
    {
        return Transaction.Create(entity.Id.ToString(),
            DateOnly.FromDateTime(entity.Date),
            entity.Name,
            entity.CategoryId.ToString(),
            new FiatToBitcoinDetails(
                entity.FromAccountId.ToString(),
                entity.ToAccountId!.ToString(),
                DatabaseValueParser.AdaptToFiatValue(entity.FromFiatAmount!.Value),
                DatabaseValueParser.AdaptToBtcValue(entity.ToSatAmount!.Value)),
            null,
            entity.Notes,
            fixedExpenseRecord is not null
                ? new TransactionFixedExpenseReference(
                    new FixedExpenseId(fixedExpenseRecord.FixedExpense.Id.ToString()),
                    DateOnly.FromDateTime(fixedExpenseRecord.ReferenceDate))
                : null,
            entity.Version);
    }

    private static Transaction ConvertToBtcToFiatTransfer(TransactionEntity entity,
        FixedExpenseRecordEntity? fixedExpenseRecord)
    {
        return Transaction.Create(entity.Id.ToString(),
            DateOnly.FromDateTime(entity.Date),
            entity.Name,
            entity.CategoryId.ToString(),
            new BitcoinToFiatDetails(
                entity.FromAccountId.ToString(),
                entity.ToAccountId!.ToString(),
                DatabaseValueParser.AdaptToBtcValue(entity.FromSatAmount!.Value),
                entity.ToFiatAmount!.Value),
            null,
            entity.Notes,
            fixedExpenseRecord is not null
                ? new TransactionFixedExpenseReference(
                    new FixedExpenseId(fixedExpenseRecord.FixedExpense.Id.ToString()),
                    DateOnly.FromDateTime(fixedExpenseRecord.ReferenceDate))
                : null,
            entity.Version);
    }

    public static TransactionEntity AsEntity(this Transaction transaction)
    {
        try
        {
            return transaction.TransactionDetails switch
            {
                FiatDetails fiatTransaction => ConvertToFiatTransactionEntity(transaction),
                BitcoinDetails btcTransaction => ConvertToBtcTransactionEntity(transaction),
                BitcoinToBitcoinDetails btcToBtcTransfer => ConvertToBtcToBtcTransferEntity(transaction),
                FiatToFiatDetails fiatToFiatTransfer => ConvertToFiatToFiatTransferEntity(transaction),
                FiatToBitcoinDetails fiatToBtcTransfer => ConvertToFiatToBtcTransferEntity(transaction),
                BitcoinToFiatDetails btcToFiatTransfer => ConvertToBtcToFiatTransferEntity(transaction),
                _ => throw new ArgumentException("Transaction type not found")
            };
        }
        catch (Exception ex)
        {
            throw new BrokenConversionToDbException(nameof(Transaction), transaction.Id, ex);
        }
    }

    private static TransactionEntity ConvertToFiatTransactionEntity(Transaction transaction)
    {
        var fiatDetails = (FiatDetails)transaction.TransactionDetails;

        return new TransactionEntity()
        {
            Id = new ObjectId(transaction.Id),
            Type = TransactionEntityType.Fiat,
            CategoryId = new ObjectId(transaction.CategoryId),
            Name = transaction.Name,
            Date = transaction.Date.ToValtDateTime(),
            Notes = transaction.Notes,
            SatAmount = transaction.AutoSatAmountDetails?.SatAmount?.Sats,
            BtcPrice = null,
            FromAccountId = new ObjectId(fiatDetails.FiatAccountId),
            ToAccountId = null,
            IsAutoSatAmount = transaction.AutoSatAmountDetails?.IsAutoSatAmount,
            FromFiatAmount = ConvertFiatValueToStorageFormat(fiatDetails.Amount, !fiatDetails.Credit),
            SatAmountStateId = (int?)transaction.AutoSatAmountDetails?.SatAmountState,
            ToFiatAmount = null,
            FromSatAmount = null,
            ToSatAmount = null,
            Version = transaction.Version
        };
    }

    private static TransactionEntity ConvertToBtcTransactionEntity(Transaction transaction)
    {
        var bitcoinDetails = (BitcoinDetails)transaction.TransactionDetails;

        return new TransactionEntity()
        {
            Id = new ObjectId(transaction.Id),
            Type = TransactionEntityType.Bitcoin,
            CategoryId = new ObjectId(transaction.CategoryId),
            Name = transaction.Name,
            Date = transaction.Date.ToValtDateTime(),
            Notes = transaction.Notes,
            SatAmount = null,
            BtcPrice = null,
            FromAccountId = new ObjectId(bitcoinDetails.BtcAccountId),
            ToAccountId = null,
            IsAutoSatAmount = null,
            FromFiatAmount = null,
            FromSatAmount = ConvertBtcValueToStorageFormat(bitcoinDetails.Amount, !bitcoinDetails.Credit),
            ToFiatAmount = null,
            ToSatAmount = null,
            SatAmountStateId = null,
            Version = transaction.Version
        };
    }

    private static TransactionEntity ConvertToBtcToBtcTransferEntity(Transaction transaction)
    {
        var bitcoinToBitcoinDetails = (BitcoinToBitcoinDetails)transaction.TransactionDetails;

        return new TransactionEntity()
        {
            Id = new ObjectId(transaction.Id),
            Type = TransactionEntityType.BitcoinToBitcoin,
            CategoryId = new ObjectId(transaction.CategoryId),
            Name = transaction.Name,
            Date = transaction.Date.ToValtDateTime(),
            Notes = transaction.Notes,
            SatAmount = null,
            BtcPrice = null,
            FromAccountId = new ObjectId(bitcoinToBitcoinDetails.FromBtcAccountId),
            ToAccountId = new ObjectId(bitcoinToBitcoinDetails.ToBtcAccountId),
            IsAutoSatAmount = null,
            FromFiatAmount = null,
            FromSatAmount = ConvertBtcValueToStorageFormat(bitcoinToBitcoinDetails.Amount, true),
            ToSatAmount = ConvertBtcValueToStorageFormat(bitcoinToBitcoinDetails.Amount, false),
            ToFiatAmount = null,
            SatAmountStateId = null,
            Version = transaction.Version
        };
    }

    private static TransactionEntity ConvertToFiatToFiatTransferEntity(Transaction transaction)
    {
        var fiatToFiatDetails = (FiatToFiatDetails)transaction.TransactionDetails;

        return new TransactionEntity()
        {
            Id = new ObjectId(transaction.Id),
            Type = TransactionEntityType.FiatToFiat,
            CategoryId = new ObjectId(transaction.CategoryId),
            Name = transaction.Name,
            Date = transaction.Date.ToValtDateTime(),
            Notes = transaction.Notes,
            SatAmount = transaction.AutoSatAmountDetails?.SatAmount?.Sats,
            BtcPrice = null,
            FromAccountId = new ObjectId(fiatToFiatDetails.FromFiatAccountId),
            ToAccountId = new ObjectId(fiatToFiatDetails.ToFiatAccountId),
            IsAutoSatAmount = transaction.AutoSatAmountDetails?.IsAutoSatAmount,
            SatAmountStateId = (int?)transaction.AutoSatAmountDetails?.SatAmountState,
            FromFiatAmount = ConvertFiatValueToStorageFormat(fiatToFiatDetails.FromAmount, true),
            FromSatAmount = null,
            ToFiatAmount = ConvertFiatValueToStorageFormat(fiatToFiatDetails.ToAmount, false),
            ToSatAmount = null,
            Version = transaction.Version
        };
    }

    private static TransactionEntity ConvertToFiatToBtcTransferEntity(Transaction transaction)
    {
        var fiatToBitcoinDetails = (FiatToBitcoinDetails)transaction.TransactionDetails;

        return new TransactionEntity()
        {
            Id = new ObjectId(transaction.Id),
            Type = TransactionEntityType.FiatToBitcoin,
            CategoryId = new ObjectId(transaction.CategoryId),
            Name = transaction.Name,
            Date = transaction.Date.ToValtDateTime(),
            Notes = transaction.Notes,
            SatAmount = null,
            BtcPrice = ConvertFiatValueToStorageFormat(fiatToBitcoinDetails.BtcPrice, false),
            FromAccountId = new ObjectId(fiatToBitcoinDetails.FromFiatAccountId),
            ToAccountId = new ObjectId(fiatToBitcoinDetails.ToBtcAccountId),
            IsAutoSatAmount = null,
            SatAmountStateId = null,
            FromFiatAmount = ConvertFiatValueToStorageFormat(fiatToBitcoinDetails.FromAmount, true),
            FromSatAmount = null,
            ToFiatAmount = null,
            ToSatAmount = ConvertBtcValueToStorageFormat(fiatToBitcoinDetails.ToAmount, false),
            Version = transaction.Version
        };
    }

    private static TransactionEntity ConvertToBtcToFiatTransferEntity(Transaction transaction)
    {
        var bitcoinToFiatDetails = (BitcoinToFiatDetails)transaction.TransactionDetails;

        return new TransactionEntity()
        {
            Id = new ObjectId(transaction.Id),
            Type = TransactionEntityType.BitcoinToFiat,
            CategoryId = new ObjectId(transaction.CategoryId),
            Name = transaction.Name,
            Date = transaction.Date.ToValtDateTime(),
            Notes = transaction.Notes,
            SatAmount = null,
            BtcPrice = ConvertFiatValueToStorageFormat(bitcoinToFiatDetails.BtcPrice, false),
            FromAccountId = new ObjectId(bitcoinToFiatDetails.FromBtcAccountId),
            ToAccountId = new ObjectId(bitcoinToFiatDetails.ToFiatAccountId),
            IsAutoSatAmount = null,
            SatAmountStateId = null,
            FromFiatAmount = null,
            FromSatAmount = ConvertBtcValueToStorageFormat(bitcoinToFiatDetails.FromAmount.Sats, true),
            ToFiatAmount = ConvertFiatValueToStorageFormat(bitcoinToFiatDetails.ToAmount, false),
            ToSatAmount = null,
            Version = transaction.Version
        };
    }

    private static decimal ConvertFiatValueToStorageFormat(FiatValue fiatValue, bool storeAsDebt)
    {
        return storeAsDebt ? fiatValue.Value * -1 : fiatValue.Value;
    }

    private static long ConvertBtcValueToStorageFormat(BtcValue btcValue, bool storeAsDebt)
    {
        var result = btcValue.Sats;

        if (storeAsDebt)
            result = result * -1;

        return result;
    }
}