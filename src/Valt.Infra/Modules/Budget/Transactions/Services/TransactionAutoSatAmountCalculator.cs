using System.Collections.Frozen;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Transactions.Messages;

namespace Valt.Infra.Modules.Budget.Transactions.Services;

public class TransactionAutoSatAmountCalculator : ITransactionAutoSatAmountCalculator
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILocalDatabase _localDatabase;
    private readonly ILocalHistoricalPriceProvider _localHistoricalPriceProvider;
    private readonly ILogger<TransactionAutoSatAmountCalculator> _logger;

    public TransactionAutoSatAmountCalculator(ITransactionRepository transactionRepository,
        ILocalDatabase localDatabase, ILocalHistoricalPriceProvider localHistoricalPriceProvider, ILogger<TransactionAutoSatAmountCalculator> logger)
    {
        _transactionRepository = transactionRepository;
        _localDatabase = localDatabase;
        _localHistoricalPriceProvider = localHistoricalPriceProvider;
        _logger = logger;
    }

    public Task UpdateAutoSatAmountAsync(TransactionId transactionId)
    {
        return UpdateAutoSatAmountAsync([transactionId]);
    }

    public async Task UpdateAutoSatAmountAsync(TransactionId[] transactionIds)
    {
        var accounts = _localDatabase.GetAccounts().FindAll().ToFrozenSet();
        
        var transactionAutoSatAmountsUpdated = new List<AutoSatAmountRefreshedTransaction>();
        var notify = false;
        foreach (var transactionId in transactionIds)
        {
            var transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId);
            
            if (transaction is null)
                continue;

            //only applies to fiat and fiattofiat
            if (transaction.TransactionDetails.TransferType is not (TransactionTransferTypes.Fiat
                or TransactionTransferTypes.FiatToFiat)) continue;
            
            //grab the origin currency
            var fromAccount = accounts.SingleOrDefault(x =>
                x.Id == transaction.TransactionDetails.FromAccountId.ToObjectId());

            if (fromAccount is null)
            {
                transaction.ChangeAutoSatAmount(new AutoSatAmountDetails(true, SatAmountState.Missing, null));
                _logger.LogInformation("[TransactionAutoSatAmountCalculator] Missing account for transaction: {TransactionId}",
                    transaction.Id);

                await _transactionRepository.SaveTransactionAsync(transaction);
                continue;
            }

            var currency = FiatCurrency.GetFromCode(fromAccount.Currency!);

            var btcUsdRateOnDate = await GetBtcUsdRate(transaction);
            var fiatRateOnDate = await GetFiatRate(transaction, currency);

            if (btcUsdRateOnDate is null || fiatRateOnDate is null)
            {
                _logger.LogInformation("[TransactionAutoSatAmountCalculator] Skipping transaction: {TransactionId}", transaction.Id);
                continue;
            }

            var finalRate = btcUsdRateOnDate * fiatRateOnDate;

            if (finalRate is null or 0)
            {
                transaction.ChangeAutoSatAmount(new AutoSatAmountDetails(true, SatAmountState.Missing, null));

                _logger.LogInformation("[TransactionAutoSatAmountCalculator] Missing rate for transaction: {TransactionId}",
                    transaction.Id);
                await _transactionRepository.SaveTransactionAsync(transaction);
                continue;
            }

            var transactionBtcAmount = (transaction.TransactionDetails.FromAccountFiatValue < 0
                ? -transaction.TransactionDetails.FromAccountFiatValue
                : transaction.TransactionDetails.FromAccountFiatValue) / finalRate;

            transaction.ChangeAutoSatAmount(new AutoSatAmountDetails(true, SatAmountState.Processed,
                BtcValue.ParseBitcoin(transactionBtcAmount.GetValueOrDefault())));
            _logger.LogInformation("[TransactionAutoSatAmountCalculator] Setting transaction {TransactionId} as {SatAmountSats} sats",
                transaction.Id, transaction.AutoSatAmountDetails!.SatAmount!.Sats);

            await _transactionRepository.SaveTransactionAsync(transaction);

            transactionAutoSatAmountsUpdated.Add(new AutoSatAmountRefreshedTransaction(transaction.Id,
                transaction.AutoSatAmountDetails!.SatAmount!.Sats));

            notify = true;
        }
        
        if (notify)
            WeakReferenceMessenger.Default.Send(new AutoSatAmountRefreshed(transactionAutoSatAmountsUpdated));
    }
    
    private async Task<decimal?> GetFiatRate(Transaction transaction, FiatCurrency currency)
    {
        if (currency == FiatCurrency.Usd)
            return 1;
        
        return await _localHistoricalPriceProvider.GetFiatRateAtAsync(transaction.Date, currency);
    }

    private async Task<decimal?> GetBtcUsdRate(Transaction transaction)
    {
        return await _localHistoricalPriceProvider.GetUsdBitcoinRateAtAsync(transaction.Date);
    }
}