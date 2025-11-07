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
        _logger.LogInformation("[AutoSatAmountJob] Updating auto sat amount history");
        
        try
        {
            var lastDateParsed = _priceDatabase.GetBitcoinData().Max(x => x.Date).Date;
            
            var maxDateRange = lastDateParsed.AddDays(1);
            
            var pendingTransactionIds = _localDatabase.GetTransactions()
                .Find(x => x.SatAmountStateId == 2 && x.Date < maxDateRange && x.IsAutoSatAmount != null)
                .Select(x => new TransactionId(x.Id.ToString()))
                .ToArray();

            await _transactionAutoSatAmountCalculator.UpdateAutoSatAmountAsync(pendingTransactionIds);
            
            _logger.LogInformation("[AutoSatAmountJob] Update finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AutoSatAmountJob] Error during execution");
            throw;
        }
    }
}