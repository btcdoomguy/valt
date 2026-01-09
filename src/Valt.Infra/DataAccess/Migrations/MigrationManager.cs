using Valt.Infra.DataAccess.Migrations.Scripts;
using Valt.Infra.Modules.Configuration;

namespace Valt.Infra.DataAccess.Migrations;

internal class MigrationManager
{
    private readonly IEnumerable<IMigrationScript> _migrationScripts;
    private readonly ConfigurationManager _configurationManager;

    public MigrationManager(IEnumerable<IMigrationScript> migrationScripts,
        ConfigurationManager configurationManager)
    {
        _migrationScripts = migrationScripts;
        _configurationManager = configurationManager;
    }

    public async Task RunMigrationsAsync()
    {
        var latestVersion = await _configurationManager.GetMigrationVersionAsync();

        var nextMigrations = _migrationScripts.Where(x => x.Version > latestVersion).ToList();

        if (nextMigrations.Count == 0)
            return;

        foreach (var migration in nextMigrations)
        {
            await migration.ExecuteAsync();
        }

        await _configurationManager.SetMigrationVersionAsync(nextMigrations.Max(x => x.Version));

        // Update the minimum assembly version requirement based on all migrations
        // (both previously executed and newly executed)
        UpdateMinimumAssemblyVersion();
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