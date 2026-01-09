using Valt.Infra.DataAccess.Migrations.Scripts;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Configuration;

namespace Valt.Infra.DataAccess.Migrations;

internal class MigrationManager
{
    private readonly IEnumerable<IMigrationScript> _migrationScripts;
    private readonly ConfigurationManager _configurationManager;
    private readonly IPriceDatabase _priceDatabase;

    public MigrationManager(IEnumerable<IMigrationScript> migrationScripts,
        ConfigurationManager configurationManager,
        IPriceDatabase priceDatabase)
    {
        _migrationScripts = migrationScripts;
        _configurationManager = configurationManager;
        _priceDatabase = priceDatabase;
    }

    public async Task RunMigrationsAsync()
    {
        var latestVersion = await _configurationManager.GetMigrationVersionAsync();

        var nextMigrations = _migrationScripts.Where(x => x.Version > latestVersion).ToList();

        if (nextMigrations.Count == 0)
        {
            // Even if no migrations to run, check if prices.db needs reset
            // (handles case where user opens a second valt file that's already migrated)
            HandlePricesDatabaseReset();
            return;
        }

        foreach (var migration in nextMigrations)
        {
            await migration.ExecuteAsync();
        }

        await _configurationManager.SetMigrationVersionAsync(nextMigrations.Max(x => x.Version));

        // Update the minimum assembly version requirement based on all migrations
        // (both previously executed and newly executed)
        UpdateMinimumAssemblyVersion();

        // Handle prices.db reset if any migration requires it
        HandlePricesDatabaseReset();
    }

    private void HandlePricesDatabaseReset()
    {
        // Get the maximum ResetPricesToVersion across all migrations
        var requiredVersion = _migrationScripts
            .Select(m => m.ResetPricesToVersion)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (requiredVersion == 0)
            return;

        var pricesDbPath = Path.Combine(ValtEnvironment.AppDataPath, "prices.db");

        // If prices.db doesn't exist, just ensure it gets created with the right version
        if (!_priceDatabase.DatabaseFileExists())
        {
            _priceDatabase.OpenDatabase();
            _priceDatabase.SetVersion(requiredVersion);
            _priceDatabase.CloseDatabase();
            return;
        }

        // Open to check current version
        _priceDatabase.OpenDatabase();
        var currentVersion = _priceDatabase.GetVersion();

        if (currentVersion >= requiredVersion)
        {
            // Already at required version, close and let normal startup reopen
            _priceDatabase.CloseDatabase();
            return;
        }

        // Need to reset - close, delete, recreate
        _priceDatabase.CloseDatabase();
        File.Delete(pricesDbPath);

        _priceDatabase.OpenDatabase();
        _priceDatabase.SetVersion(requiredVersion);
        _priceDatabase.CloseDatabase();
    }

    private void UpdateMinimumAssemblyVersion()
    {
        // Get the maximum MinimumAssemblyVersion from all migrations
        var maxVersion = _migrationScripts
            .Select(m => m.MinimumAssemblyVersion)
            .OrderByDescending(v => v)
            .FirstOrDefault();

        if (maxVersion is not null)
        {
            var currentMinVersion = _configurationManager.GetMinimumAssemblyVersion();

            // Only update if the new version is higher
            if (currentMinVersion is null || maxVersion > currentMinVersion)
            {
                _configurationManager.SetMinimumAssemblyVersion(maxVersion);
            }
        }
    }
}