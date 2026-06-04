using System;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Budget;
using Valt.Infra.Modules.Configuration;

namespace Valt.UI.Services;

public class DatabaseLifecycleService : IDatabaseLifecycleService
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly BackgroundJobManager _backgroundJobManager;
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly IDatabaseVersionChecker _databaseVersionChecker;
    private readonly ILogger<DatabaseLifecycleService> _logger;

    public DatabaseLifecycleService(
        ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        BackgroundJobManager backgroundJobManager,
        IDatabaseInitializer databaseInitializer,
        IDatabaseVersionChecker databaseVersionChecker,
        ILogger<DatabaseLifecycleService> logger)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _backgroundJobManager = backgroundJobManager;
        _databaseInitializer = databaseInitializer;
        _databaseVersionChecker = databaseVersionChecker;
        _logger = logger;
    }

    public Task<(bool Success, string? ErrorMessage)> OpenLocalDatabaseAsync(string file, string password)
    {
        try
        {
            _localDatabase.OpenDatabase(file, password);
            return Task.FromResult((true, (string?)null));
        }
        catch (LiteException ex)
        {
            return Task.FromResult((false, (string?)ex.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, (string?)ex.Message));
        }
    }

    public void CloseLocalDatabase()
    {
        _localDatabase.CloseDatabase();
    }

    public (bool IsCompatible, string? RequiredVersion, string? CurrentVersion) CheckCompatibility()
    {
        var result = _databaseVersionChecker.CheckCompatibility();
        return (result.IsCompatible, result.RequiredVersion?.ToString(), result.CurrentVersion?.ToString());
    }

    public Task InitializeNewDatabaseAsync(string language, string[] currencies)
    {
        return _databaseInitializer.InitializeAsync(language, currencies);
    }

    public Task MigrateAsync()
    {
        return _databaseInitializer.MigrateAsync();
    }

    public async Task<PriceDatabaseInitResult> InitializePriceDatabaseAsync()
    {
        try
        {
            var jobsAlreadyStarted = false;

            if (!_priceDatabase.DatabaseFileExists() || IsPriceDatabaseEmpty())
            {
                var installResult = await InstallPriceDatabaseAsync();

                if (!installResult)
                    return new PriceDatabaseInitResult(false, null);

                jobsAlreadyStarted = true;
            }

            if (!_priceDatabase.HasDatabaseOpen)
                _priceDatabase.OpenDatabase();

            var duplicatesRemoved = _priceDatabase.RemoveDuplicateEntries();
            if (duplicatesRemoved > 0)
                _logger.LogInformation("Removed {Count} duplicate entries from price database", duplicatesRemoved);

            if (!jobsAlreadyStarted)
                await _backgroundJobManager.StartAllJobsAsync(jobType: BackgroundJobTypes.PriceDatabase, triggerInitialRun: false);

            await _backgroundJobManager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.LivePricesUpdater);

            _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.BitcoinHistoryUpdater);
            _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.FiatHistoryUpdater);
            _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.IndicatorsUpdater);

            return new PriceDatabaseInitResult(true, null);
        }
        catch (LiteException ex)
        {
            _logger.LogError(ex, "Error initializing price database");
            return new PriceDatabaseInitResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing price database");
            return new PriceDatabaseInitResult(false, ex.Message);
        }
    }

    private bool IsPriceDatabaseEmpty()
    {
        try
        {
            _priceDatabase.OpenDatabase();
            return !_priceDatabase.HasPriceData();
        }
        finally
        {
            _priceDatabase.CloseDatabase();
        }
    }

    private async Task<bool> InstallPriceDatabaseAsync()
    {
        _priceDatabase.OpenDatabase();

        try
        {
            await _backgroundJobManager.StartAllJobsAsync(
                jobType: BackgroundJobTypes.PriceDatabase,
                triggerInitialRun: false);

            await _backgroundJobManager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.BitcoinHistoryUpdater);
            await _backgroundJobManager.TriggerJobAndWaitAsync(BackgroundJobSystemNames.FiatHistoryUpdater);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during price database installation");
            return false;
        }
    }

    public async Task CloseDatabasesAsync()
    {
        await _backgroundJobManager.StopAll();
        _localDatabase.CloseDatabase();
        _priceDatabase.CloseDatabase();
    }

    public Task StartValtDatabaseJobsAsync()
    {
        return _backgroundJobManager.StartAllJobsAsync(jobType: BackgroundJobTypes.ValtDatabase);
    }

    public void TriggerJobManually(BackgroundJobSystemNames systemName)
    {
        _backgroundJobManager.TriggerJobManually(systemName);
    }
}
