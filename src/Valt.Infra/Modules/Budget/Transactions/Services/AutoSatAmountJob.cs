using Microsoft.Extensions.Logging;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Modules.Budget.Transactions.Services;

internal class AutoSatAmountJob : IBackgroundJob
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ITransactionAutoSatAmountCalculator _transactionAutoSatAmountCalculator;
    private readonly ILogger<AutoSatAmountJob> _logger;
    public string Name => "Auto sat amount job";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.AutoSatAmountUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
    public TimeSpan Interval => TimeSpan.FromSeconds(120);
    
    public AutoSatAmountJob(ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        ITransactionAutoSatAmountCalculator transactionAutoSatAmountCalculator,
        ILogger<AutoSatAmountJob> logger)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _transactionAutoSatAmountCalculator = transactionAutoSatAmountCalculator;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[AutoSatAmountJob] Started");
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[AutoSatAmountJob] Starting auto sat amount calculation cycle");

        if (!_priceDatabase.HasDatabaseOpen)
        {
            _logger.LogInformation("[AutoSatAmountJob] Price database not open, skipping");
            return;
        }

        if (!_localDatabase.HasDatabaseOpen)
        {
            _logger.LogInformation("[AutoSatAmountJob] Local database not open, skipping");
            return;
        }

        try
        {
            var btcRecordCount = _priceDatabase.GetBitcoinData().Query().Count();
            if (btcRecordCount == 0)
            {
                _logger.LogInformation("[AutoSatAmountJob] No BTC price data available, skipping");
                return;
            }

            var lastDateParsed = _priceDatabase.GetBitcoinData().Max(x => x.Date).Date;
            _logger.LogInformation("[AutoSatAmountJob] Last BTC price date: {Date}", lastDateParsed.ToString("yyyy-MM-dd"));

            var maxDateRange = lastDateParsed.AddDays(1);

            var pendingTransactionIds = _localDatabase.GetTransactions()
                .Find(x => x.SatAmountStateId == 2 && x.Date < maxDateRange && x.IsAutoSatAmount != null)
                .Select(x => new TransactionId(x.Id.ToString()))
                .ToArray();

            if (pendingTransactionIds.Length == 0)
            {
                _logger.LogInformation("[AutoSatAmountJob] No pending transactions to update");
                return;
            }

            _logger.LogInformation("[AutoSatAmountJob] Found {Count} transactions pending sat amount calculation",
                pendingTransactionIds.Length);

            await _transactionAutoSatAmountCalculator.UpdateAutoSatAmountAsync(pendingTransactionIds);

            _logger.LogInformation("[AutoSatAmountJob] Successfully updated {Count} transactions", pendingTransactionIds.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AutoSatAmountJob] Error during execution");
            throw;
        }
    }
}