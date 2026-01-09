using Microsoft.Extensions.Logging;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Modules.Budget.Accounts.Services;

internal class AccountTotalsJob : IBackgroundJob
{
    private readonly IClock _clock;
    private readonly IAccountCacheService _accountCacheService;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalDatabase _localDatabase;
    private readonly ILogger<AccountTotalsJob> _logger;
    private DateOnly? _currentDay;

    public string Name => "Account totals calculator";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.AccountTotalsCalculator;
    public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
    public TimeSpan Interval => new(0, 0, 5);

    public AccountTotalsJob(IClock clock, IAccountCacheService accountCacheService, IPriceDatabase priceDatabase,
        ILocalDatabase localDatabase, ILogger<AccountTotalsJob> logger)
    {
        _clock = clock;
        _accountCacheService = accountCacheService;
        _priceDatabase = priceDatabase;
        _localDatabase = localDatabase;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[AccountTotalsCalculator] Started");
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_priceDatabase.HasDatabaseOpen)
            {
                _logger.LogInformation("[AccountTotalsCalculator] Price database not open, skipping");
                return;
            }

            if (!_localDatabase.HasDatabaseOpen)
            {
                _logger.LogInformation("[AccountTotalsCalculator] Local database not open, skipping");
                return;
            }

            var today = _clock.GetCurrentLocalDate();

            if (today != _currentDay)
            {
                _logger.LogInformation(
                    "[AccountTotalsCalculator] Day changed from {OldDate} to {NewDate}, refreshing totals",
                    _currentDay?.ToString("yyyy-MM-dd") ?? "null", today.ToString("yyyy-MM-dd"));

                await _accountCacheService.RefreshCurrentTotalsAsync(today);

                _currentDay = today;
                _logger.LogInformation("[AccountTotalsCalculator] Account totals refreshed successfully for {Date}",
                    today.ToString("yyyy-MM-dd"));
            }
            else
            {
                _logger.LogDebug("[AccountTotalsCalculator] No date change, skipping refresh (current: {Date})",
                    today.ToString("yyyy-MM-dd"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AccountTotalsCalculator] Error during execution");
            throw;
        }
    }
}