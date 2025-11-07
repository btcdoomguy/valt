using Microsoft.Extensions.Logging;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Budget.Accounts.Services;

namespace Valt.Infra.BackgroundJobs.AccountTotalsCalculator;

internal class AccountTotalsJob : IBackgroundJob
{
    private readonly IClock _clock;
    private readonly IAccountCacheService _accountCacheService;
    private readonly ILogger<AccountTotalsJob> _logger;
    private DateOnly? _currentDay;
    
    public string Name => "Account totals calculator";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.AccountTotalsCalculator;
    public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
    public TimeSpan Interval => new(0, 0, 5);

    public AccountTotalsJob(IClock clock, IAccountCacheService accountCacheService, ILogger<AccountTotalsJob> logger)
    {
        _clock = clock;
        _accountCacheService = accountCacheService;
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
            var today = _clock.GetCurrentLocalDate();

            if (today != _currentDay)
            {
                await _accountCacheService.RefreshCurrentTotalsAsync(today);

                _currentDay = today;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AccountTotalsCalculator] Error during execution");
            throw;
        }
    }
}